namespace SolidCS.Avalonia;

public sealed class TextBlock : Component
{
    private readonly global::Avalonia.Controls.TextBlock _textBlock;
    private readonly IDisposable _effect;

    public TextBlock(
        Func<string> textFunc,
        global::Avalonia.Layout.HorizontalAlignment horizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Center)
    {
        _textBlock = new global::Avalonia.Controls.TextBlock
        {
            HorizontalAlignment = horizontalAlignment,
        };
        _effect = Context.Instance.RegisterEffect(
            () => _textBlock.Text = textFunc()
        );
    }

    public override global::Avalonia.Controls.Control GetControl() => _textBlock;

    public override void Dispose() => _effect.Dispose();
}
