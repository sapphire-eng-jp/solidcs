namespace SolidCS.Avalonia;

/// <summary>
/// For / TreeView 共通の keyed reconcile。key 一致の既存 state を再利用し、
/// 新規 key だけ create、消えた key だけ dispose する。key の重複は fail-fast。
/// create が途中で throw した場合はそのラウンドで作った state だけ dispose して
/// 再 throw する — 呼び元の current は無傷のまま残る (旧世代を壊さない保証)。
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
                        $"keyed diff の key が重複している ({k})。key: で一意なキーを指定すること。");
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
