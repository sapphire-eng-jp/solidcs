namespace SolidCS.Avalonia;

/// <summary>
/// Keyed reconcile shared by For and TreeView. Existing state with a matching
/// key is reused; only new keys are created and only departed keys are
/// disposed. Duplicate keys fail fast.
/// If create throws partway through, only the state created in that round is
/// disposed and the exception is re-thrown — the caller's current stays
/// untouched (guaranteeing the old generation is never broken).
/// </summary>
internal static class KeyedDiff
{
    internal static List<(object Key, TState State)> Reconcile<TItem, TState>(
        List<(object Key, TState State)> current,
        IReadOnlyList<TItem> items,
        Func<TItem, object> keyFn,
        Func<TItem, TState> create,
        Action<TState> dispose)
    {
        var oldByKey = new Dictionary<object, TState>(current.Count);
        foreach (var (k, s) in current) oldByKey[k] = s;

        var next = new List<(object Key, TState State)>(items.Count);
        var used = new HashSet<object>();
        var created = new List<TState>();
        try
        {
            foreach (var item in items)
            {
                var k = keyFn(item);
                if (!used.Add(k))
                {
                    throw new InvalidOperationException(
                        $"Duplicate key in keyed diff ({k}). Specify a unique key via key:.");
                }
                if (oldByKey.TryGetValue(k, out var s))
                {
                    next.Add((k, s));
                }
                else
                {
                    var ns = create(item);
                    created.Add(ns);
                    next.Add((k, ns));
                }
            }
        }
        catch
        {
            foreach (var s in created) dispose(s);
            throw;
        }

        foreach (var (k, s) in current)
        {
            if (!used.Contains(k)) dispose(s);
        }
        return next;
    }
}
