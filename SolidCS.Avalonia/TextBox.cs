namespace SolidCS.Avalonia;

public sealed class TextBox : Component
{
    private readonly global::Avalonia.Controls.TextBox _textBox;
    private readonly IDisposable _effect;

    public TextBox(
        Signal<string> value,
        HorizontalAlignment horizontalAlignment = HorizontalAlignment.Stretch)
    {
        _textBox = new global::Avalonia.Controls.TextBox
        {
            HorizontalAlignment = horizontalAlignment.ToAvalonia(),
        };
        // Signal → UI: 他所で value.Value が更新されたら TextBox に反映
        _effect = Context.Instance.RegisterEffect(
            () => _textBox.Text = value.Value
        );
        // UI → Signal: ユーザー入力を Signal に書き戻す。
        // Signal.setter の equality check により、effect 経由の代入で起きる
        // TextChanged のエコーは同値で止まり、無限ループにならない。
        _textBox.TextChanged += (_, _) => value.Value = _textBox.Text ?? "";
    }

    public override global::Avalonia.Controls.Control GetControl() => _textBox;

    protected override void OnDispose() => _effect.Dispose();
}
