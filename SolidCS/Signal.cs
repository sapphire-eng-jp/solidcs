namespace SolidCS;

public sealed class Signal<T>(T initValue)
{
    // Signal reads and writes are assumed to stay within a single logical flow (the UI thread
    // or one async chain). To write from another thread, the caller must marshal the write,
    // e.g. via a Dispatcher.
    private readonly HashSet<Effect> _subscribers = new();

    private T _value = initValue;
    public T Value
    {
        get
        {
            if (Context.Instance.Subscribers.TryPeek(out var effect))
            {
                if (_subscribers.Add(effect))
                {
                    effect.OnCleanup(() => _subscribers.Remove(effect));
                }
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

            if (_subscribers.Count == 0) return;

            // Iterate over a snapshot so that effects adding new subscriptions during notification don't cause a collection-modified exception.
            foreach (var effect in _subscribers.ToArray())
            {
                effect.Run();
            }
        }
    }
}
