using System.Collections.Generic;

namespace SolidCS;


public sealed class Context
{
    public static Context Instance {get;} = new();

    public Stack<Effect> Subscribers {get;} = new();

    public System.Threading.Lock Lock {get;} = new();

    public IDisposable RegisterEffect(Action callback)
    {
        var effect = new Effect(callback);
        Subscribers.Push(effect);
        try
        {
            effect.Run();
        }
        finally
        {
            Subscribers.Pop();
        }
        return effect;
    }

    private Context()
    {
    }
}
