namespace SolidCS;

public sealed class Signal<T>(T initValue)
{
    // Signal の読み書きは単一の論理フロー (UI スレッドや一つの async chain) 内で完結させる前提。
    // 別スレッドから書く場合は呼び元で Dispatcher 経由などにマーシャリングすること。
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

            // 通知中にeffectが新しい購読を足してもコレクション変更例外にならないようスナップショットで反復する。
            foreach (var effect in _subscribers.ToArray())
            {
                effect.Run();
            }
        }
    }
}
