// SPDX-License-Identifier: MIT

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
        // Display-only, so this is a reactive Func<double> display rather than a two-way binding.
        _effect = Context.Instance.RegisterEffect(
            () => _bar.Value = valueFunc()
        );
    }

    public override global::Avalonia.Controls.Control GetControl() => _bar;

    protected override void OnDispose() => _effect.Dispose();
}
