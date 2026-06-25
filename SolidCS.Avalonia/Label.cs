namespace SolidCS.Avalonia;

public sealed class Label : Component
{
    private readonly global::Avalonia.Controls.Label _label;
    private readonly IDisposable _effect;

    public Label(
        Func<string> contentFunc,
        global::Avalonia.Layout.HorizontalAlignment horizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Center)
    {
        _label = new global::Avalonia.Controls.Label
        {
            HorizontalAlignment = horizontalAlignment,
        };
        _effect = Context.Instance.RegisterEffect(
            () => _label.Content = contentFunc()
        );
    }

    public override global::Avalonia.Controls.Control GetControl() => _label;

    public override void Dispose() => _effect.Dispose();
}
