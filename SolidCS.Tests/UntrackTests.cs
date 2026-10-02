// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;

namespace SolidCS.Tests;

public class UntrackTests
{
    [Fact]
    public void UntrackedReadDoesNotSubscribe()
    {
        var s = new Signal<int>(0);
        var runs = 0;

        Context.Instance.RegisterEffect(() =>
        {
            Context.Instance.Untrack(() => { _ = s.Value; });
            runs++;
        });

        Assert.Equal(1, runs);
        s.Value = 1;
        Assert.Equal(1, runs);
    }

    [Fact]
    public void TrackingResumesAfterUntrack()
    {
        var tracked = new Signal<int>(0);
        var untracked = new Signal<int>(0);
        var runs = 0;

        Context.Instance.RegisterEffect(() =>
        {
            Context.Instance.Untrack(() => { _ = untracked.Value; });
            // Reads after leaving Untrack are tracked as usual.
            _ = tracked.Value;
            runs++;
        });

        Assert.Equal(1, runs);
        untracked.Value = 1;
        Assert.Equal(1, runs);
        tracked.Value = 1;
        Assert.Equal(2, runs);
    }

    [Fact]
    public void UntrackFuncReturnsValue()
    {
        var s = new Signal<int>(42);
        var result = Context.Instance.Untrack(() => s.Value);
        Assert.Equal(42, result);
    }

    [Fact]
    public void NestedUntrackRestoresOuterState()
    {
        var s = new Signal<int>(0);
        var runs = 0;

        Context.Instance.RegisterEffect(() =>
        {
            Context.Instance.Untrack(() =>
            {
                Context.Instance.Untrack(() => { });
                // After returning from the nested Untrack, we are still inside Untrack.
                _ = s.Value;
            });
            runs++;
        });

        Assert.Equal(1, runs);
        s.Value = 1;
        Assert.Equal(1, runs);
    }

    [Fact]
    public void RegisterEffectInsideUntrackTracksItsOwnDependencies()
    {
        var s = new Signal<int>(0);
        var outerRuns = 0;
        var innerRuns = 0;

        Context.Instance.RegisterEffect(() =>
        {
            outerRuns++;
            Context.Instance.Untrack(() =>
            {
                // Even inside Untrack, a newly registered effect tracks its own
                // dependencies normally (child construction in For / Show is exactly this pattern).
                Context.Instance.RegisterEffect(() =>
                {
                    _ = s.Value;
                    innerRuns++;
                });
            });
        });

        Assert.Equal(1, outerRuns);
        Assert.Equal(1, innerRuns);

        // s is subscribed only by the inner effect, so changing it re-runs only the inner one.
        s.Value = 1;
        Assert.Equal(1, outerRuns);
        Assert.Equal(2, innerRuns);
    }

    // Simulates the equivalent of For using only the core. Pins down that the pattern of
    // disposing child effects and rebuilding them under Untrack, inside the list Signal's
    // effect, works without over-tracking and survives Dispose within the notification
    // path (reentrancy).
    [Fact]
    public void ForLikeRebuildScenario()
    {
        var ctx = Context.Instance;
        var items = new Signal<IReadOnlyList<Signal<int>>>([new Signal<int>(0)]);
        var rebuilds = 0;
        var childRuns = 0;
        var childHandles = new List<IDisposable>();

        ctx.RegisterEffect(() =>
        {
            var list = items.Value;
            rebuilds++;
            ctx.Untrack(() =>
            {
                foreach (var h in childHandles) h.Dispose();
                childHandles.Clear();
                foreach (var item in list)
                {
                    childHandles.Add(ctx.RegisterEffect(() =>
                    {
                        _ = item.Value;
                        childRuns++;
                    }));
                }
            });
        });

        Assert.Equal(1, rebuilds);
        Assert.Equal(1, childRuns);

        // Changing a Signal inside an item re-runs only the child effect; the whole list is
        // not rebuilt (without Untrack, rebuilds would also increase = over-tracking).
        items.Value[0].Value = 1;
        Assert.Equal(1, rebuilds);
        Assert.Equal(2, childRuns);

        // Replacing the list triggers a rebuild. Changing an old item's Signal does not wake the disposed old children.
        var old = items.Value[0];
        items.Value = [new Signal<int>(10), new Signal<int>(20)];
        Assert.Equal(2, rebuilds);
        Assert.Equal(4, childRuns);
        old.Value = 99;
        Assert.Equal(4, childRuns);
    }

    // Issue 7: the case where a child effect subscribes to the same Signal as its parent,
    // and the parent's rebuild disposes the old-generation child while it still sits in the
    // notification snapshot. Uses per-generation counters to pin down that, thanks to the
    // _disposed guard, an old generation never runs again after Dispose.
    [Fact]
    public void ChildSubscribedToSameSignalDisposedDuringRebuildStaysDead()
    {
        var ctx = Context.Instance;
        var s = new Signal<int>(0);
        var runsByGen = new Dictionary<int, int>();
        var gen = 0;
        IDisposable? child = null;

        ctx.RegisterEffect(() =>
        {
            _ = s.Value;
            ctx.Untrack(() =>
            {
                child?.Dispose();
                var myGen = gen++;
                runsByGen[myGen] = 0;
                child = ctx.RegisterEffect(() =>
                {
                    _ = s.Value;
                    runsByGen[myGen]++;
                });
            });
        });

        Assert.Equal(1, runsByGen[0]);

        s.Value = 1; // generation 0 is disposed and generation 1 is born (must complete without throwing)
        var gen0After = runsByGen[0];
        s.Value = 2; // generation 0 cannot be in the snapshot, and even if it were, _disposed makes it a no-op
        Assert.Equal(gen0After, runsByGen[0]);
        Assert.True(runsByGen[2] >= 1); // the latest generation is alive
    }
}
