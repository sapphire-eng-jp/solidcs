using Avalonia.Layout;

namespace SolidCS.Avalonia;

public sealed class Label : Component
{
    private readonly global::Avalonia.Controls.Label _label;
    private readonly IDisposable _effect;

    public Label(Func<string> contentFunc)
    {
        _label = new global::Avalonia.Controls.Label
        {
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        _effect = Context.Instance.RegisterEffect(
            () => _label.Content = contentFunc()
        );
    }

    public override global::Avalonia.Controls.Control GetControl() => _label;

    public override void Dispose() => _effect.Dispose();
}
