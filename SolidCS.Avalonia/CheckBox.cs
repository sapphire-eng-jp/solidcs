namespace SolidCS.Avalonia;

public sealed class CheckBox : Component
{
    private readonly global::Avalonia.Controls.CheckBox _checkBox;
    private readonly IDisposable _contentEffect;
    private readonly IDisposable _valueEffect;

    public CheckBox(
        Signal<bool> value,
        Func<string> contentFunc,
        HorizontalAlignment horizontalAlignment = HorizontalAlignment.Center)
    {
        _checkBox = new global::Avalonia.Controls.CheckBox
        {
            HorizontalAlignment = horizontalAlignment.ToAvalonia(),
        };
        _contentEffect = Context.Instance.RegisterEffect(
            () => _checkBox.Content = contentFunc()
        );
        _valueEffect = Context.Instance.RegisterEffect(
            () => _checkBox.IsChecked = value.Value
        );
        // IsChecked は bool? (Indeterminate 状態あり)。Signal<bool> に丸める。
        _checkBox.IsCheckedChanged += (_, _) => value.Value = _checkBox.IsChecked ?? false;
    }

    public override global::Avalonia.Controls.Control GetControl() => _checkBox;

    public override void Dispose()
    {
        _contentEffect.Dispose();
        _valueEffect.Dispose();
    }
}
