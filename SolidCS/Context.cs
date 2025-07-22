using System.Collections.Generic;

namespace SolidCS;


public sealed class Context
{
    public static Context Instance {get;} = new();

    public Stack<Action> Subscribers {get;} = new();

    private readonly System.Threading.Lock _lock = new();

    public void RegisterEffect(Action subscriber)
    {
        lock(_lock) 
        {
            Instance.Subscribers.Push(subscriber);
            subscriber();
            Instance.Subscribers.Pop();
        }
    }

    private Context()
    {
    }
}
