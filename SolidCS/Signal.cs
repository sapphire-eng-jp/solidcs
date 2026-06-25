namespace SolidCS;

public sealed class Signal<T>(T initValue)
{
    // UI構築処理全体をlock(Context.Instance.Lock)で囲う前提で、Subscribersへのアクセスは
    // UIスレッドのみという想定。動的にUIを追加する場合も同じlock規約で守る。
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

            // 通知中にeffectが新しい購読を足してもコレクション変更例外にならないようスナップショットで反復する。
            foreach (var effect in _subscribers.ToArray())
            {
                effect.Run();
            }
        }
    }
}
