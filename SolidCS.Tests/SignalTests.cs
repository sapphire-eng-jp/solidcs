// SPDX-License-Identifier: MIT

using System;

namespace SolidCS.Tests;

public class SignalTests
{
    [Fact]
    public void NewSignalTest()
    {
        var s = new Signal<int>(1);
        Assert.Equal(1, s.Value);
    }

    [Fact]
    public void SubscribeTest()
    {
        var ctx = Context.Instance;
        var s = new Signal<int>(1);
        var result = 0;

        var effect = new Effect(() =>
        {
            result = s.Value;
            Console.WriteLine(result);
        });
        ctx.Subscribers.Push(effect);
        effect.Run();
        ctx.Subscribers.Pop();

        s.Value = 10;

        Assert.Equal(10, result);
    }

    [Fact]
    public void RegisterEffectTest()
    {
        var s = new Signal<int>(1);
        var result = 0;

        Context.Instance.RegisterEffect(
                () => result = s.Value
                );

        Assert.Equal(1, result);
        s.Value = 10;
        Assert.Equal(10, result);
    }

    [Fact]
    public void DedupeTest()
    {
        var s = new Signal<int>(1);
        var runs = 0;

        Context.Instance.RegisterEffect(() =>
        {
            _ = s.Value;
            _ = s.Value;
            _ = s.Value;
            runs++;
        });

        Assert.Equal(1, runs);
        s.Value = 10;
        Assert.Equal(2, runs);
    }

    [Fact]
    public void DisposeStopsNotificationTest()
    {
        var s = new Signal<int>(1);
        var runs = 0;

        var handle = Context.Instance.RegisterEffect(() =>
        {
            _ = s.Value;
            runs++;
        });

        Assert.Equal(1, runs);
        s.Value = 10;
        Assert.Equal(2, runs);

        handle.Dispose();
        s.Value = 20;
        Assert.Equal(2, runs);
    }

    [Fact]
    public void DynamicDependencyTrackingTest()
    {
        var flag = new Signal<bool>(true);
        var a = new Signal<int>(0);
        var b = new Signal<int>(0);
        var runs = 0;

        Context.Instance.RegisterEffect(() =>
        {
            runs++;
            _ = flag.Value ? a.Value : b.Value;
        });

        Assert.Equal(1, runs);

        // While flag=true, changing a re-runs the effect, but changing b does not.
        a.Value = 1;
        Assert.Equal(2, runs);
        b.Value = 1;
        Assert.Equal(2, runs);

        // After flipping flag, the effect depends on b and no longer on a.
        flag.Value = false;
        Assert.Equal(3, runs);
        a.Value = 99;
        Assert.Equal(3, runs);
        b.Value = 2;
        Assert.Equal(4, runs);
    }

    [Fact]
    public void DisposedEffectDoesNotRunViaStaleSnapshotTest()
    {
        var s = new Signal<int>(0);
        var aRuns = 0;
        var bRuns = 0;

        IDisposable? handleB = null;
        Context.Instance.RegisterEffect(() =>
        {
            _ = s.Value;
            aRuns++;
            // Dispose b during the first re-run of the notification. The snapshot is still [A, B],
            // but B.Run() should early-return because _disposed=true, so bRuns must not increase.
            if (aRuns == 2) handleB!.Dispose();
        });
        handleB = Context.Instance.RegisterEffect(() =>
        {
            _ = s.Value;
            bRuns++;
        });

        Assert.Equal(1, aRuns);
        Assert.Equal(1, bRuns);

        s.Value = 1;
        Assert.Equal(2, aRuns);
        Assert.Equal(1, bRuns);
    }

    [Fact]
    public void NewEffectRegisteredDuringNotificationTest()
    {
        var s = new Signal<int>(0);
        var innerRan = false;

        Context.Instance.RegisterEffect(() =>
        {
            _ = s.Value;
            if (s.Value > 0 && !innerRan)
            {
                Context.Instance.RegisterEffect(() =>
                {
                    _ = s.Value;
                    innerRan = true;
                });
            }
        });

        s.Value = 1;
        Assert.True(innerRan);
    }
}
