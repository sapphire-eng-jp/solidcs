namespace SolidCS;

public sealed class Effect(Action run) : IDisposable
{
    private readonly List<Action> _cleanups = new();
    private bool _disposed;

    public void Run() => run();

    internal void OnCleanup(Action cleanup) => _cleanups.Add(cleanup);

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        foreach (var c in _cleanups) c();
        _cleanups.Clear();
    }
}
