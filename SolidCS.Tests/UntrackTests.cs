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
            // Untrack を抜けた後の読み出しは通常どおり追跡される。
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
                // ネストした Untrack から戻っても、まだ Untrack 中のまま。
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
                // Untrack 中でも新しく登録した effect 自身は普通に依存追跡できる
                // (For / Show の子構築がまさにこのパターン)。
                Context.Instance.RegisterEffect(() =>
                {
                    _ = s.Value;
                    innerRuns++;
                });
            });
        });

        Assert.Equal(1, outerRuns);
        Assert.Equal(1, innerRuns);

        // s は inner にだけ購読されているので、変更で inner だけが再実行される。
        s.Value = 1;
        Assert.Equal(1, outerRuns);
        Assert.Equal(2, innerRuns);
    }

    // For 相当をコアだけでシミュレートするテスト。リスト Signal の effect 内で
    // 子 effect 群を Dispose → Untrack 下で再構築するパターンが、過追跡なしで動き、
    // 通知パス内の Dispose (再入) でも壊れないことを固定する。
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

        // アイテム内 Signal の変化では子 effect だけが再実行され、リスト全体は再構築されない
        // (Untrack がなければ rebuilds も増えてしまう = 過追跡)。
        items.Value[0].Value = 1;
        Assert.Equal(1, rebuilds);
        Assert.Equal(2, childRuns);

        // リスト差し替えで再構築。旧アイテムの Signal を変えても Dispose 済みの旧子は反応しない。
        var old = items.Value[0];
        items.Value = [new Signal<int>(10), new Signal<int>(20)];
        Assert.Equal(2, rebuilds);
        Assert.Equal(4, childRuns);
        old.Value = 99;
        Assert.Equal(4, childRuns);
    }

    // 論点 7: 子 effect が親と同じ Signal を購読しているとき、通知スナップショットに
    // 旧世代の子が残ったまま親の再構築で Dispose されるケース。_disposed ガードにより
    // 旧世代は Dispose 以降一切実行されないことを世代別カウンタで固定する。
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

        s.Value = 1; // 世代 0 が Dispose され、世代 1 が生まれる (例外なく完走すること)
        var gen0After = runsByGen[0];
        s.Value = 2; // 世代 0 は snapshot に入り得ないし、入っていても _disposed でノーオペ
        Assert.Equal(gen0After, runsByGen[0]);
        Assert.True(runsByGen[2] >= 1); // 最新世代は生きている
    }
}
