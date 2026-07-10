namespace SolidCS.Avalonia;

/// <summary>
/// リストの各要素を Component に写像する動的リスト。items 内で読まれたリスト
/// Signal の変化で子が増減する。C# はコンストラクタでジェネリック型推論が
/// 効かないため、`For.Each(...)` の static ファクトリを入口にする。
/// 更新は「新しいリストを代入」のイミュータブル規約
/// (`todos.Value = [.. todos.Value, item]`) — in-place の Add は setter の
/// 参照比較を素通りして通知されない。
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
    private readonly List<Component> _children = new();
    private readonly IDisposable _effect;
    // 構築中の再入検出用 (render や子コンストラクタがリスト Signal を書くケース)
    private bool _rebuilding;

    internal ForComponent(
        Func<IReadOnlyList<T>> items,
        Func<T, Component> render,
        Func<T, object>? key,
        Orientation orientation)
    {
        // key は v1 では未使用。v2 の keyed diff (key 一致のアイテムは Component を
        // 再利用) にシグネチャ変更なしで差し替えるための口だけ確保してある。
        _ = key;
        _render = render;
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

    // v1 は全破棄 + 作り直し。アイテム内の状態 (TextBox の入力途中・フォーカス・
    // アイテム内 Signal) はリスト変化のたびに消える点に注意。
    private void Rebuild(IReadOnlyList<T> list)
    {
        // render がリスト Signal を書くと構築ループの最中にこの effect が再入し、
        // 内側の再構築完了後に外側ループが旧リストの子を追記して新旧が混在する。
        // 黙って壊れる前に fail-fast する。
        if (_rebuilding)
        {
            throw new InvalidOperationException(
                "For の子構築中にリスト Signal が書かれた (再入)。render 内でリスト Signal を書かないこと。");
        }

        _rebuilding = true;
        try
        {
            // 先に新世代を全部構築してから旧世代を破棄する。render が途中で throw
            // しても旧世代は無傷のまま表示され続け、作りかけの新世代はここで解放する
            // (返却前に throw した子コンストラクタ内部の effect までは救えない)。
            var next = new List<Component>(list.Count);
            try
            {
                foreach (var item in list) next.Add(_render(item));
            }
            catch
            {
                foreach (var c in next) c.Dispose();
                throw;
            }

            foreach (var c in _children) c.Dispose();
            _children.Clear();
            _panel.Children.Clear();

            _children.AddRange(next);
            // 1 個ずつ Add すると子の数だけ変更通知とレイアウト無効化が走るので一括で入れる。
            _panel.Children.AddRange(next.Select(c => c.GetControl()));
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
        foreach (var c in _children) c.Dispose();
    }
}
