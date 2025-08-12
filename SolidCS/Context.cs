using System.Collections.Generic;

namespace SolidCS;


public sealed class Context
{
    public static Context Instance {get;} = new();

    public Stack<Action> Subscribers {get;} = new();

    public System.Threading.Lock Lock {get;} = new();

    public void RegisterEffect(Action subscriber)
    {
        Instance.Subscribers.Push(subscriber);
        subscriber();
        Instance.Subscribers.Pop();
    }

    private Context()
    {
    }
}
