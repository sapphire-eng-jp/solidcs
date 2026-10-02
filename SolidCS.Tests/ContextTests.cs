// SPDX-License-Identifier: MIT

using System;

namespace SolidCS.Tests;

public class ContextTests
{
    [Fact]
    public void SingletonTest()
    {
        var ctx1 = Context.Instance;
        var ctx2 = Context.Instance;

        Assert.Equal(ctx1, ctx2);
    }

    [Fact]
    public void PushPopTest()
    {
        var ctx = Context.Instance;
        ctx.Subscribers.Push(new Effect(() => Console.WriteLine("HW")));

        var effect = Context.Instance.Subscribers.Pop();
        effect.Run();

        Assert.Empty(ctx.Subscribers);
    }

    [Fact]
    public void ClosureTest()
    {
        var a = 2;
        var b = 0;

        var ctx = Context.Instance;
        ctx.Subscribers.Push(new Effect(() => { b = a * 2; }));
        ctx.Subscribers.Pop().Run();

        Assert.Equal(4, b);
    }

    [Fact]
    void RegisterEffectTest()
    {
        var a = 2;
        var b = 0;

        var ctx = Context.Instance;
        ctx.RegisterEffect(() =>
        {
            b = a;
        });

        Assert.Equal(2, b);
        Assert.Empty(ctx.Subscribers);
    }

    [Fact]
    public void RegisterEffectFirstRunThrowCleansUpSubscriptionsTest()
    {
        var s = new Signal<int>(0);
        var runs = 0;

        // When the initial Run throws, the caller never receives the handle, so
        // RegisterEffect itself releases the partially set-up subscriptions before rethrowing.
        Assert.Throws<InvalidOperationException>(() => Context.Instance.RegisterEffect(() =>
        {
            _ = s.Value;
            runs++;
            throw new InvalidOperationException();
        }));

        Assert.Equal(1, runs);
        s.Value = 1;
        Assert.Equal(1, runs);
    }
}

