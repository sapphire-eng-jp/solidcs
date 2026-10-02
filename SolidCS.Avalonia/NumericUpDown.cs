// SPDX-License-Identifier: MIT

namespace SolidCS.Avalonia;

public sealed class NumericUpDown : Component
{
    private readonly global::Avalonia.Controls.NumericUpDown _numeric;
    private readonly IDisposable _effect;

    public NumericUpDown(
        Signal<decimal> value,
        decimal? min = null,
        decimal? max = null,
        decimal increment = 1,
        HorizontalAlignment horizontalAlignment = HorizontalAlignment.Stretch)
    {
        _numeric = new global::Avalonia.Controls.NumericUpDown
        {
            Increment = increment,
            HorizontalAlignment = horizontalAlignment.ToAvalonia(),
        };
        // null means "no limit" = keep Avalonia's defaults (decimal.MinValue / MaxValue).
        if (min is not null) _numeric.Minimum = min.Value;
        if (max is not null) _numeric.Maximum = max.Value;

        // Signal → UI
        _effect = Context.Instance.RegisterEffect(
            () => _numeric.Value = value.Value
        );
        // UI → Signal. Value is decimal? (the text field can be emptied), so coerce to 0.
        _numeric.ValueChanged += (_, _) => value.Value = _numeric.Value ?? 0;
    }

    public override global::Avalonia.Controls.Control GetControl() => _numeric;

    protected override void OnDispose() => _effect.Dispose();
}
