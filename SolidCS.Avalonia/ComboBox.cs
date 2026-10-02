// SPDX-License-Identifier: MIT

namespace SolidCS.Avalonia;

/// <summary>
/// Drop-down selection. A group-level API of "list of options + Signal&lt;T&gt; of the
/// current selection" (same shape as RadioGroup). items is reactive (the options are
/// swapped out when a list Signal read inside the `Func` changes). The entry point is
/// the `ComboBox.Of(...)` static factory (constructors don't get generic inference).
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
