namespace SolidCS.Avalonia;

/// <summary>
/// 選択セマンティクス付きのリスト。API は ComboBox と同型 (SelectorBinding を共有)。
/// 「選択が要るなら ListBox、ただの動的表示なら For」の棲み分け。
/// </summary>
public static class ListBox
{
    public static ListBoxComponent<T> Of<T>(
        Signal<T> selected,
        Func<IReadOnlyList<T>> items,
        Func<T, string> labelFunc,
        HorizontalAlignment horizontalAlignment = HorizontalAlignment.Stretch)
        => new(selected, items, labelFunc, horizontalAlignment);
}

public sealed class ListBoxComponent<T> : Component
{
    private readonly global::Avalonia.Controls.ListBox _list;
    private readonly SelectorBinding<T> _binding;

    internal ListBoxComponent(
        Signal<T> selected,
        Func<IReadOnlyList<T>> items,
        Func<T, string> labelFunc,
        HorizontalAlignment horizontalAlignment)
    {
        _list = new global::Avalonia.Controls.ListBox
        {
            HorizontalAlignment = horizontalAlignment.ToAvalonia(),
        };
        _binding = new SelectorBinding<T>(_list, selected, items, labelFunc);
    }

    public override global::Avalonia.Controls.Control GetControl() => _list;

    protected override void OnDispose() => _binding.Dispose();
}
