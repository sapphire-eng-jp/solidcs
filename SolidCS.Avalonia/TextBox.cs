namespace SolidCS.Avalonia;

public sealed class TextBox : Component
{
    private readonly global::Avalonia.Controls.TextBox _textBox;
    private readonly IDisposable _effect;

    public TextBox(
        Signal<string> value,
        HorizontalAlignment horizontalAlignment = HorizontalAlignment.Stretch,
        bool multiline = false)
    {
        _textBox = new global::Avalonia.Controls.TextBox
        {
            HorizontalAlignment = horizontalAlignment.ToAvalonia(),
        };
        if (multiline)
        {
            // Multi-line mode: accept Enter as a newline and wrap lines. Align content
            // to the top so text doesn't float at the vertical center when the control
            // is stretched vertically (e.g. filling a DockPanel).
            _textBox.AcceptsReturn = true;
            _textBox.TextWrapping = global::Avalonia.Media.TextWrapping.Wrap;
            _textBox.VerticalContentAlignment = global::Avalonia.Layout.VerticalAlignment.Top;
        }
        // Signal → UI: reflect updates to value.Value made elsewhere into the TextBox
        _effect = Context.Instance.RegisterEffect(
            () => _textBox.Text = value.Value
        );
        // UI → Signal: write user input back into the Signal.
        // Thanks to the Signal setter's equality check, the TextChanged echo caused
        // by assignments via the effect stops on the equal value — no infinite loop.
        _textBox.TextChanged += (_, _) => value.Value = _textBox.Text ?? "";
    }

    public override global::Avalonia.Controls.Control GetControl() => _textBox;

    protected override void OnDispose() => _effect.Dispose();
}
