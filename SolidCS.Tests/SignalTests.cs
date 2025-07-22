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

        var func = () =>
        {
            result = s.Value;
            Console.WriteLine(result);
        };
        ctx.Subscribers.Push(func);
        func();
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
}
