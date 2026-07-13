namespace SolidCS.Avalonia;

/// <summary>
/// リストの各要素を Component に写像する動的リスト。items 内で読まれたリスト
/// Signal の変化で子が増減する。C# はコンストラクタでジェネリック型推論が
/// 効かないため、`For.Each(...)` の static ファクトリを入口にする。
/// 更新は「新しいリストを代入」のイミュータブル規約
/// (`todos.Value = [.. todos.Value, item]`) — in-place の Add は setter の
/// 参照比較を素通りして通知されない。
/// v2 (keyed diff): key (省略時はアイテム自身) が一致する子は Component を
/// 再利用し、追加/削除/並べ替えだけを差分適用する — アイテム内の UI 状態
/// (入力途中・フォーカス等) がリスト増減で消えない。再利用された子は
/// 再 render されないので、アイテム内の変化はアイテム内の Signal で表現する
/// こと (key を Id 等にして中身だけ変えると表示が古いまま残る)。
/// key の重複は InvalidOperationException で fail-fast。
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
    // 構築中の再入検出用 (render や子コンストラクタがリスト Signal を書くケース)
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
            var list = items(); // ここで読まれたリスト Signal だけを追跡する
            // 子の構築を Untrack で切り離す。さもないとアイテム内部の Signal まで
            // For の effect に購読され、アイテム 1 個の変化でリスト全体が再構築される。
            Context.Instance.Untrack(() => Rebuild(list));
        });
    }

    private void Rebuild(IReadOnlyList<T> list)
    {
        // render がリスト Signal を書くと構築ループの最中にこの effect が再入して
        // 新旧が混在する。黙って壊れる前に fail-fast する。
        if (_rebuilding)
        {
            throw new InvalidOperationException(
                "For の子構築中にリスト Signal が書かれた (再入)。render 内でリスト Signal を書かないこと。");
        }

        _rebuilding = true;
        try
        {
            // key 一致の子は再利用、新規だけ render、消えた子だけ Dispose。
            // render が途中で throw した場合は作りかけの新規子だけ解放され、
            // 既存の子と表示は無傷のまま残る (Reconcile 内で保証)。
            _entries = KeyedDiff.Reconcile(_entries, list, _keyFn, _render, c => c.Dispose());

            var desired = _entries.Select(e => e.Comp.GetControl()).ToList();
            var children = _panel.Children;
            if (children.Count == 0)
            {
                // 初回は一括投入 (1 個ずつ Add すると子の数だけレイアウト無効化が走る)
                children.AddRange(desired);
                return;
            }
            // 消えた子を外してから並び順を合わせる。既存子の並べ替えは Move —
            // detach/attach されないのでフォーカス等の状態が残る。
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
