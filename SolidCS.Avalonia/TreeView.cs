// SPDX-License-Identifier: MIT

namespace SolidCS.Avalonia;

/// <summary>
/// Dynamic tree. Each node owns its own children effect, and only the subtree
/// of the node whose Signal (read inside childrenFunc) changed is updated via
/// keyed diff (the same Reconcile as For v2) — nodes with matching keys reuse
/// their TreeViewItem, so expansion state survives. Labels are reactive via a
/// per-node effect. Passing selected enables two-way selection binding
/// ("no selection" is represented by the selected value not being present in
/// the tree; null / default is never written back to the Signal).
/// The entry point is the `TreeView.Of(...)` static factory (constructors
/// can't infer generic type arguments). When key is omitted, the item itself
/// is the key (duplicates fail fast).
/// </summary>
public static class TreeView
{
    public static TreeViewComponent<T> Of<T>(
        Func<IReadOnlyList<T>> roots,
        Func<T, string> labelFunc,
        Func<T, IReadOnlyList<T>> childrenFunc,
        Func<T, object>? key = null,
        Signal<T>? selected = null)
        => new(roots, labelFunc, childrenFunc, key, selected);
}

public sealed class TreeViewComponent<T> : Component
{
    private readonly global::Avalonia.Controls.TreeView _tree;
    private readonly Func<T, string> _labelFunc;
    private readonly Func<T, IReadOnlyList<T>> _childrenFunc;
    private readonly Func<T, object> _keyFn;
    // For converting between T and TreeViewItem for selection. Keys are only
    // unique among siblings, so tree-wide reverse lookup goes through this map.
    private readonly Dictionary<global::Avalonia.Controls.TreeViewItem, T> _itemMap = new();
    private readonly IDisposable _rootsEffect;
    private readonly IDisposable? _selectionEffect;
    private List<(object Key, Node State)> _roots = new();
    private bool _rebuilding;

    internal TreeViewComponent(
        Func<IReadOnlyList<T>> roots,
        Func<T, string> labelFunc,
        Func<T, IReadOnlyList<T>> childrenFunc,
        Func<T, object>? key,
        Signal<T>? selected)
    {
        _labelFunc = labelFunc;
        _childrenFunc = childrenFunc;
        _keyFn = key ?? (x => (object)x!);
        _tree = new global::Avalonia.Controls.TreeView();

        _rootsEffect = Context.Instance.RegisterEffect(() =>
        {
            var list = roots(); // track only the root-list Signal read here
            Context.Instance.Untrack(() =>
            {
                if (_rebuilding)
                {
                    throw new InvalidOperationException(
                        "The roots Signal was written while TreeView was being built (re-entrancy).");
                }
                _rebuilding = true;
                try
                {
                    _roots = KeyedDiff.Reconcile(
                        _roots, list, _keyFn,
                        item => new Node(this, item),
                        n => n.Dispose());
                    PatchItems(_tree.Items, _roots.Select(r => (object)r.State.Item).ToList());
                }
                finally
                {
                    _rebuilding = false;
                }
            });
        });

        if (selected is not null)
        {
            // Signal → UI: reverse-look up the TreeViewItem for the value and select it
            _selectionEffect = Context.Instance.RegisterEffect(() =>
            {
                var v = selected.Value;
                global::Avalonia.Controls.TreeViewItem? found = null;
                foreach (var (item, val) in _itemMap)
                {
                    if (EqualityComparer<T>.Default.Equals(val, v))
                    {
                        found = item;
                        break;
                    }
                }
                _tree.SelectedItem = found;
            });
            // UI → Signal: write back only real selections. Echoes are stopped
            // by the equality check.
            _tree.SelectionChanged += (_, _) =>
            {
                if (_tree.SelectedItem is global::Avalonia.Controls.TreeViewItem tvi
                    && _itemMap.TryGetValue(tvi, out var v))
                {
                    selected.Value = v;
                }
            };
        }
    }

    // Bring an ItemCollection (roots and per-node alike) in line with the
    // desired order. Remove departed items first, then fix up positions.
    // ItemCollection has no Move, so reordering is RemoveAt + Insert
    // (TreeViewItem's IsExpanded is a property, so it survives across
    // detach/attach).
    internal static void PatchItems(
        global::Avalonia.Controls.ItemCollection items, List<object> desired)
    {
        var keep = new HashSet<object>(desired);
        for (var i = items.Count - 1; i >= 0; i--)
        {
            if (!keep.Contains(items[i]!)) items.RemoveAt(i);
        }
        for (var i = 0; i < desired.Count; i++)
        {
            var idx = items.IndexOf(desired[i]);
            if (idx == i) continue;
            if (idx >= 0) items.RemoveAt(idx);
            items.Insert(i, desired[i]);
        }
    }

    public override global::Avalonia.Controls.Control GetControl() => _tree;

    protected override void OnDispose()
    {
        _rootsEffect.Dispose();
        _selectionEffect?.Dispose();
        foreach (var (_, n) in _roots) n.Dispose();
    }

    // A single tree node: TreeViewItem + label effect + children effect (keyed diff).
    private sealed class Node : IDisposable
    {
        internal readonly global::Avalonia.Controls.TreeViewItem Item = new();
        private readonly TreeViewComponent<T> _owner;
        private readonly IDisposable _labelEffect;
        private readonly IDisposable _childrenEffect;
        private List<(object Key, Node State)> _children = new();
        private bool _rebuilding;

        internal Node(TreeViewComponent<T> owner, T value)
        {
            _owner = owner;
            owner._itemMap[Item] = value;
            _labelEffect = Context.Instance.RegisterEffect(
                () => Item.Header = owner._labelFunc(value)
            );
            _childrenEffect = Context.Instance.RegisterEffect(() =>
            {
                var list = owner._childrenFunc(value); // track only this node's children Signal
                Context.Instance.Untrack(() =>
                {
                    if (_rebuilding)
                    {
                        throw new InvalidOperationException(
                            "The children Signal was written while a TreeView node was being built (re-entrancy).");
                    }
                    _rebuilding = true;
                    try
                    {
                        _children = KeyedDiff.Reconcile(
                            _children, list, owner._keyFn,
                            item => new Node(owner, item),
                            n => n.Dispose());
                        PatchItems(Item.Items, _children.Select(c => (object)c.State.Item).ToList());
                    }
                    finally
                    {
                        _rebuilding = false;
                    }
                });
            });
        }

        public void Dispose()
        {
            _labelEffect.Dispose();
            _childrenEffect.Dispose();
            foreach (var (_, c) in _children) c.Dispose();
            _owner._itemMap.Remove(Item);
        }
    }
}
