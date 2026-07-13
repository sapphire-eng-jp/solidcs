namespace SolidCS.Avalonia;

/// <summary>
/// 動的ツリー。各ノードが自分の children effect を持ち、childrenFunc 内で読まれた
/// Signal が変わったノードのサブツリーだけが keyed diff (For v2 と同じ Reconcile)
/// で更新される — key 一致のノードは TreeViewItem ごと再利用されるので展開状態が
/// 生き残る。label はノード単位の effect でリアクティブ。selected を渡すと
/// 選択の双方向バインド (「未選択」は selected の値がツリー内に無い状態で表現し、
/// Signal に null / default を書き戻すことはない)。
/// 入口は `TreeView.Of(...)` static ファクトリ (コンストラクタはジェネリック推論が
/// 効かない)。key 省略時はアイテム自身がキー (重複は fail-fast)。
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
    // 選択の T ⇔ TreeViewItem 変換用。key は兄弟間でしか一意でないため
    // ツリー全体の逆引きはこの map で行う。
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
            var list = roots(); // ここで読まれたルートリストの Signal だけを追跡
            Context.Instance.Untrack(() =>
            {
                if (_rebuilding)
                {
                    throw new InvalidOperationException(
                        "TreeView の構築中にルートの Signal が書かれた (再入)。");
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
            // Signal → UI: 値に対応する TreeViewItem を逆引きして選択
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
            // UI → Signal: 実選択だけ書き戻す。エコーは equality check で止まる。
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

    // ItemCollection (ルート / 各ノード共通) を desired の並びに合わせる。
    // 消えたものを外してから位置を合わせる。ItemCollection に Move は無いので
    // 並べ替えは RemoveAt + Insert (TreeViewItem の IsExpanded はプロパティなので
    // detach/attach をまたいで保持される)。
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

    // ツリーの 1 ノード。TreeViewItem + label effect + children effect (keyed diff)。
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
                var list = owner._childrenFunc(value); // このノードの children Signal だけを追跡
                Context.Instance.Untrack(() =>
                {
                    if (_rebuilding)
                    {
                        throw new InvalidOperationException(
                            "TreeView のノード構築中に children の Signal が書かれた (再入)。");
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
