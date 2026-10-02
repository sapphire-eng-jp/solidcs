// SPDX-License-Identifier: MIT

namespace SolidCS;

public sealed class Effect(Action run) : IDisposable
{
    private readonly List<Action> _cleanups = new();
    private bool _disposed;

    public void Run()
    {
        if (_disposed) return;

        // Release all previous subscriptions and re-track, to handle cases where conditional reads change the dependencies.
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
            // If one cleanup fails, keep running the rest.
            try { c(); } catch { }
        }
        _cleanups.Clear();
    }
}
