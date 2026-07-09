namespace SolidCS.Avalonia;

public sealed class Label : Component
{
    private readonly global::Avalonia.Controls.Label _label;
    private readonly IDisposable _effect;

    public Label(
        Func<string> contentFunc,
        HorizontalAlignment horizontalAlignment = HorizontalAlignment.Center)
    {
        _label = new global::Avalonia.Controls.Label
        {
            HorizontalAlignment = horizontalAlignment.ToAvalonia(),
        };
        _effect = Context.Instance.RegisterEffect(
            () => _label.Content = contentFunc()
        );
    }

    public override global::Avalonia.Controls.Control GetControl() => _label;

    public override void Dispose() => _effect.Dispose();
}
