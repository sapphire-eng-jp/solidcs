namespace SolidCS.Avalonia;

public sealed class TextBlock : Component
{
    private readonly global::Avalonia.Controls.TextBlock _textBlock;
    private readonly IDisposable _effect;

    public TextBlock(
        Func<string> textFunc,
        HorizontalAlignment horizontalAlignment = HorizontalAlignment.Center)
    {
        _textBlock = new global::Avalonia.Controls.TextBlock
        {
            HorizontalAlignment = horizontalAlignment.ToAvalonia(),
        };
        _effect = Context.Instance.RegisterEffect(
            () => _textBlock.Text = textFunc()
        );
    }

    public override global::Avalonia.Controls.Control GetControl() => _textBlock;

    protected override void OnDispose() => _effect.Dispose();
}
