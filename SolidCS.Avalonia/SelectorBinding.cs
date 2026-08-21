namespace SolidCS.Avalonia;

/// <summary>
/// "items + selected Signal" binding shared by ComboBox and ListBox.
/// ItemsSource receives a string list produced by labelFunc, and the mapping
/// back to T goes through SelectedIndex (keeping Avalonia's templating
/// machinery out of the public API).
/// "No selection" is represented by the selected value not being found in
/// items (SelectedIndex = -1); null or default is never written to the Signal.
/// </summary>
internal sealed class SelectorBinding<T> : IDisposable
{
    private readonly global::Avalonia.Controls.Primitives.SelectingItemsControl _control;
    private readonly IDisposable _itemsEffect;
    private readonly IDisposable _selectionEffect;
    private List<T> _items = new();

    internal SelectorBinding(
        global::Avalonia.Controls.Primitives.SelectingItemsControl control,
        Signal<T> selected,
        Func<IReadOnlyList<T>> itemsFunc,
        Func<T, string> labelFunc)
    {
        _control = control;

        // items effect: rebuild when the list Signal read inside itemsFunc
        // changes. Labels are also recomputed at this point — note that
        // Signals inside labelFunc are NOT tracked (unlike RadioGroup's
        // labels, they are not reactive).
        _itemsEffect = Context.Instance.RegisterEffect(() =>
        {
            var list = itemsFunc();
            Context.Instance.Untrack(() =>
            {
                _items = new List<T>(list);
                _control.ItemsSource = _items.Select(labelFunc).ToList();
                // Replacing ItemsSource drops the selection, so re-apply it
                // from selected. Read selected inside Untrack to keep the
                // items effect from over-tracking a subscription to it.
                ApplySelection(selected.Value);
            });
        });
        // Signal → UI
        _selectionEffect = Context.Instance.RegisterEffect(
            () => ApplySelection(selected.Value)
        );
        // UI → Signal: write back only real selections (index >= 0). The -1
        // that occurs when ItemsSource is replaced is ignored. Echoes are
        // stopped by the Signal setter's equality check.
        _control.SelectionChanged += (_, _) =>
        {
            var idx = _control.SelectedIndex;
            if (idx >= 0 && idx < _items.Count) selected.Value = _items[idx];
        };
    }

    private void ApplySelection(T value)
    {
        // Not found means -1 = shown as no selection
        _control.SelectedIndex = _items.FindIndex(
            x => EqualityComparer<T>.Default.Equals(x, value));
    }

    public void Dispose()
    {
        _itemsEffect.Dispose();
        _selectionEffect.Dispose();
    }
}
