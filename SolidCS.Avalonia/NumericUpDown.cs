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
        // null は「制限なし」= Avalonia のデフォルト (decimal.MinValue / MaxValue) のまま。
        if (min is not null) _numeric.Minimum = min.Value;
        if (max is not null) _numeric.Maximum = max.Value;

        // Signal → UI
        _effect = Context.Instance.RegisterEffect(
            () => _numeric.Value = value.Value
        );
        // UI → Signal。Value は decimal? (テキスト欄を空にできる) なので 0 に丸める。
        _numeric.ValueChanged += (_, _) => value.Value = _numeric.Value ?? 0;
    }

    public override global::Avalonia.Controls.Control GetControl() => _numeric;

    protected override void OnDispose() => _effect.Dispose();
}
