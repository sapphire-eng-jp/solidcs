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

    /*[Fact]
    public void PushPopTest()
    {
        var ctx = Context.Instance;
        ctx.Subscribers.Push(() => Console.WriteLine("HW"));

        var action = Context.Instance.Subscribers.Pop();
        action();

        Assert.Empty(ctx.Subscribers);
    }*/

    /*[Fact]
    public void ClosureTest()
    {
        var a = 2;
        var b = 0;

        var ctx = Context.Instance;
        ctx.Subscribers.Push(() => 
                { b = a * 2; }
        );
        ctx.Subscribers.Pop()();
        Assert.Equal(4, b);
    }*/

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
        //Assert.Empty(ctx.Subscribers);

    }
}

