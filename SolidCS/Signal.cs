namespace SolidCS;

public sealed class Signal<T>(T initValue)
{
    private List<Action> Subscribers {get; init;} = new();

    private T _value = initValue;
    public T Value
    {
        get
        {
            try
            {
                var subscriber = Context.Instance.Subscribers.Peek();
                Subscribers.Add(subscriber);
                //Console.WriteLine($"count = {Subscribers.Count}");
            }
            catch
            {
                // pass
            }
            return _value;
        }
        set
        {
            if (EqualityComparer<T>.Default.Equals(_value, value))
            {
                return;
            }

            _value = value;

            Subscribers.ForEach((x) => x());
        }
    }
}
