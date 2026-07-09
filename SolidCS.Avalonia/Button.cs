namespace SolidCS.Avalonia;

public sealed class Button : Component
{
    private readonly global::Avalonia.Controls.Button _button;
    private readonly IDisposable _effect;

    public Button(
        Func<string> contentFunc,
        Action onClick,
        HorizontalAlignment horizontalAlignment = HorizontalAlignment.Center)
    {
        _button = new global::Avalonia.Controls.Button
        {
            FontSize = 24,
            HorizontalAlignment = horizontalAlignment.ToAvalonia(),
        };
        _effect = Context.Instance.RegisterEffect(
            () => _button.Content = contentFunc()
        );
        _button.Click += (_, _) => onClick();
    }

    public override global::Avalonia.Controls.Control GetControl() => _button;

    public override void Dispose() => _effect.Dispose();
}
