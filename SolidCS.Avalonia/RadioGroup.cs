namespace SolidCS.Avalonia;

/// <summary>
/// A group of mutually exclusive radio buttons. Rather than managing a bool per
/// RadioButton, the whole group is driven by "a list of options + a
/// Signal&lt;T&gt; of the current selection".
/// C# cannot infer generic type arguments on constructors, so the entry point is
/// the `RadioGroup.Of(...)` static factory (same convention as For.Each).
/// </summary>
public static class RadioGroup
{
    public static RadioGroupComponent<T> Of<T>(
        Signal<T> selected,
        IEnumerable<T> options,
        Func<T, string> labelFunc,
        Orientation orientation = Orientation.Vertical)
        => new(selected, options, labelFunc, orientation);
}

public sealed class RadioGroupComponent<T> : Component
{
    private readonly global::Avalonia.Controls.StackPanel _panel;
    private readonly List<IDisposable> _effects = new();

    internal RadioGroupComponent(
        Signal<T> selected,
        IEnumerable<T> options,
        Func<T, string> labelFunc,
        Orientation orientation)
    {
        // RadioButtons auto-group for exclusivity per parent container.
        // Each group has its own StackPanel, so no GroupName is needed —
        // multiple RadioGroups on the same screen don't interfere.
        _panel = new global::Avalonia.Controls.StackPanel
        {
            Orientation = orientation.ToAvalonia(),
        };
        foreach (var option in options)
        {
            var radio = new global::Avalonia.Controls.RadioButton();
            // Labels are reactive, as state-like props should be (updated when Signals read in labelFunc change)
            _effects.Add(Context.Instance.RegisterEffect(
                () => radio.Content = labelFunc(option)
            ));
            // Signal → UI: only the selected option gets IsChecked = true
            _effects.Add(Context.Instance.RegisterEffect(
                () => radio.IsChecked = EqualityComparer<T>.Default.Equals(selected.Value, option)
            ));
            // UI → Signal: only pick up the side that got checked (ignore the false
            // from the side unchecked by exclusivity).
            // Echoes are stopped by the Signal setter's equality check.
            radio.IsCheckedChanged += (_, _) =>
            {
                if (radio.IsChecked == true) selected.Value = option;
            };
            _panel.Children.Add(radio);
        }
    }

    public override global::Avalonia.Controls.Control GetControl() => _panel;

    protected override void OnDispose()
    {
        foreach (var e in _effects) e.Dispose();
    }
}
