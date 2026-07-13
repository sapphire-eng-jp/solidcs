namespace SolidCS.Avalonia;

public sealed class ProgressBar : Component
{
    private readonly global::Avalonia.Controls.ProgressBar _bar;
    private readonly IDisposable _effect;

    public ProgressBar(
        Func<double> valueFunc,
        double min = 0,
        double max = 100,
        HorizontalAlignment horizontalAlignment = HorizontalAlignment.Stretch)
    {
        _bar = new global::Avalonia.Controls.ProgressBar
        {
            Minimum = min,
            Maximum = max,
            HorizontalAlignment = horizontalAlignment.ToAvalonia(),
        };
        // 表示専用なので双方向バインドではなく Func<double> のリアクティブ表示。
        _effect = Context.Instance.RegisterEffect(
            () => _bar.Value = valueFunc()
        );
    }

    public override global::Avalonia.Controls.Control GetControl() => _bar;

    protected override void OnDispose() => _effect.Dispose();
}
