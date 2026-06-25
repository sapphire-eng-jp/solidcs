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
        lock(ctx.Lock)
        {
            ctx.Subscribers.Push(effect);
            effect.Run();
            ctx.Subscribers.Pop();
        }

        s.Value = 10;

        Assert.Equal(10, result);
    }

    [Fact]
    public void RegisterEffectTest()
    {
        var s = new Signal<int>(1);
        var result = 0;

        lock(Context.Instance.Lock)
        {
            Context.Instance.RegisterEffect(
                    () => result = s.Value
                    );
        }
        Assert.Equal(1, result);
        s.Value = 10;
        Assert.Equal(10, result);
    }

    [Fact]
    public void DedupeTest()
    {
        var s = new Signal<int>(1);
        var runs = 0;

        lock(Context.Instance.Lock)
        {
            Context.Instance.RegisterEffect(() =>
            {
                _ = s.Value;
                _ = s.Value;
                _ = s.Value;
                runs++;
            });
        }

        Assert.Equal(1, runs);
        s.Value = 10;
        Assert.Equal(2, runs);
    }

    [Fact]
    public void DisposeStopsNotificationTest()
    {
        var s = new Signal<int>(1);
        var runs = 0;

        IDisposable handle;
        lock(Context.Instance.Lock)
        {
            handle = Context.Instance.RegisterEffect(() =>
            {
                _ = s.Value;
                runs++;
            });
        }

        Assert.Equal(1, runs);
        s.Value = 10;
        Assert.Equal(2, runs);

        handle.Dispose();
        s.Value = 20;
        Assert.Equal(2, runs);
    }

    [Fact]
    public void NewEffectRegisteredDuringNotificationTest()
    {
        var s = new Signal<int>(0);
        var innerRan = false;

        lock(Context.Instance.Lock)
        {
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
        }

        s.Value = 1;
        Assert.True(innerRan);
    }
}
