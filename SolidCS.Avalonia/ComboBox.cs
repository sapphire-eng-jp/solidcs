namespace SolidCS.Avalonia;

/// <summary>
/// ドロップダウン選択。「選択肢のリスト + 選択中の Signal&lt;T&gt;」のグループ単位 API
/// (RadioGroup と同型)。items はリアクティブ (`Func` 内で読まれたリスト Signal の
/// 変化で選択肢が入れ替わる)。入口は `ComboBox.Of(...)` static ファクトリ
/// (コンストラクタはジェネリック推論が効かない)。
/// </summary>
public static class ComboBox
{
    public static ComboBoxComponent<T> Of<T>(
        Signal<T> selected,
        Func<IReadOnlyList<T>> items,
        Func<T, string> labelFunc,
        HorizontalAlignment horizontalAlignment = HorizontalAlignment.Stretch)
        => new(selected, items, labelFunc, horizontalAlignment);
}

public sealed class ComboBoxComponent<T> : Component
{
    private readonly global::Avalonia.Controls.ComboBox _combo;
    private readonly SelectorBinding<T> _binding;

    internal ComboBoxComponent(
        Signal<T> selected,
        Func<IReadOnlyList<T>> items,
        Func<T, string> labelFunc,
        HorizontalAlignment horizontalAlignment)
    {
        _combo = new global::Avalonia.Controls.ComboBox
        {
            HorizontalAlignment = horizontalAlignment.ToAvalonia(),
        };
        _binding = new SelectorBinding<T>(_combo, selected, items, labelFunc);
    }

    public override global::Avalonia.Controls.Control GetControl() => _combo;

    protected override void OnDispose() => _binding.Dispose();
}
