namespace SolidCS.Avalonia;

/// <summary>
/// Dynamic list that maps each item to a Component. Children are added and
/// removed when the list Signal read inside items changes. C# cannot infer
/// generic type arguments on constructors, so the `For.Each(...)` static
/// factory is the entry point.
/// Updates follow the immutable convention of assigning a new list
/// (`todos.Value = [.. todos.Value, item]`) — an in-place Add slips past the
/// setter's reference comparison and no notification fires.
/// v2 (keyed diff): children whose key matches (the item itself when key is
/// omitted) reuse their Component; only additions/removals/reorders are
/// applied as a diff — UI state inside an item (in-progress input, focus,
/// etc.) survives list changes. A reused child is NOT re-rendered, so changes
/// within an item must be expressed as Signals inside the item (if you key by
/// Id and mutate the contents, the display stays stale).
/// Duplicate keys fail fast with InvalidOperationException.
/// </summary>
public static class For
{
    public static ForComponent<T> Each<T>(
        Func<IReadOnlyList<T>> items,
        Func<T, Component> render,
        Func<T, object>? key = null,
        Orientation orientation = Orientation.Vertical)
        => new(items, render, key, orientation);
}

public sealed class ForComponent<T> : Component
{
    private readonly global::Avalonia.Controls.StackPanel _panel;
    private readonly Func<T, Component> _render;
    private readonly Func<T, object> _keyFn;
    private readonly IDisposable _effect;
    private List<(object Key, Component Comp)> _entries = new();
    // Re-entrancy detection during rebuild (for the case where render or a
    // child constructor writes the list Signal)
    private bool _rebuilding;

    internal ForComponent(
        Func<IReadOnlyList<T>> items,
        Func<T, Component> render,
        Func<T, object>? key,
        Orientation orientation)
    {
        _render = render;
        _keyFn = key ?? (x => (object)x!);
        _panel = new global::Avalonia.Controls.StackPanel
        {
            Orientation = orientation.ToAvalonia(),
        };
        _effect = Context.Instance.RegisterEffect(() =>
        {
            var list = items(); // track only the list Signal read here
            // Detach child construction with Untrack. Otherwise Signals inside
            // the items would also be subscribed to For's effect, and a change
            // in a single item would rebuild the entire list.
            Context.Instance.Untrack(() => Rebuild(list));
        });
    }

    private void Rebuild(IReadOnlyList<T> list)
    {
        // If render writes the list Signal, this effect re-enters in the middle
        // of the rebuild loop and old and new state get mixed together.
        // Fail fast instead of breaking silently.
        if (_rebuilding)
        {
            throw new InvalidOperationException(
                "The list Signal was written while For was building its children (re-entrancy). Do not write the list Signal inside render.");
        }

        _rebuilding = true;
        try
        {
            // Children with matching keys are reused; only new items are
            // rendered and only removed children are Disposed. If render
            // throws partway through, only the half-built new children are
            // released — existing children and the display remain untouched
            // (guaranteed inside Reconcile).
            _entries = KeyedDiff.Reconcile(_entries, list, _keyFn, _render, c => c.Dispose());

            var desired = _entries.Select(e => e.Comp.GetControl()).ToList();
            var children = _panel.Children;
            if (children.Count == 0)
            {
                // First round: add in bulk (Adding one at a time triggers a
                // layout invalidation per child)
                children.AddRange(desired);
                return;
            }
            // Remove departed children first, then fix up the order. Existing
            // children are reordered with Move — they are never
            // detached/attached, so state such as focus survives.
            var keep = new HashSet<global::Avalonia.Controls.Control>(desired);
            for (var i = children.Count - 1; i >= 0; i--)
            {
                if (!keep.Contains(children[i])) children.RemoveAt(i);
            }
            for (var i = 0; i < desired.Count; i++)
            {
                var idx = children.IndexOf(desired[i]);
                if (idx == i) continue;
                if (idx >= 0) children.Move(idx, i);
                else children.Insert(i, desired[i]);
            }
        }
        finally
        {
            _rebuilding = false;
        }
    }

    public override global::Avalonia.Controls.Control GetControl() => _panel;

    protected override void OnDispose()
    {
        _effect.Dispose();
        foreach (var (_, c) in _entries) c.Dispose();
    }
}
