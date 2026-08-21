namespace SolidCS.Avalonia;

public sealed class Slider : Component
{
    private readonly global::Avalonia.Controls.Slider _slider;
    private readonly IDisposable _effect;

    public Slider(
        Signal<double> value,
        double min = 0,
        double max = 100,
        HorizontalAlignment horizontalAlignment = HorizontalAlignment.Stretch)
    {
        _slider = new global::Avalonia.Controls.Slider
        {
            Minimum = min,
            Maximum = max,
            HorizontalAlignment = horizontalAlignment.ToAvalonia(),
        };
        // Signal → UI
        _effect = Context.Instance.RegisterEffect(
            () => _slider.Value = value.Value
        );
        // UI → Signal. Echoes are stopped by the Signal setter's equality check.
        _slider.ValueChanged += (_, _) => value.Value = _slider.Value;
    }

    public override global::Avalonia.Controls.Control GetControl() => _slider;

    protected override void OnDispose() => _effect.Dispose();
}
