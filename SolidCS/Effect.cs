namespace SolidCS;

public sealed class Effect(Action run) : IDisposable
{
    private readonly List<Action> _cleanups = new();
    private bool _disposed;

    public void Run()
    {
        if (_disposed) return;

        // 前回までの購読を解除して再追跡する。条件付き読み出しで依存が変わるケースに対応するため。
        Untrack();

        Context.Instance.Subscribers.Push(this);
        try
        {
            run();
        }
        finally
        {
            Context.Instance.Subscribers.Pop();
        }
    }

    internal void OnCleanup(Action cleanup) => _cleanups.Add(cleanup);

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Untrack();
    }

    private void Untrack()
    {
        foreach (var c in _cleanups)
        {
            // 1つの解除が失敗しても残りの解除を続行する。
            try { c(); } catch { }
        }
        _cleanups.Clear();
    }
}
