namespace SolidCS.Avalonia;

public sealed class Separator : MenuElement
{
    private readonly global::Avalonia.Controls.Separator _separator = new();

    public override global::Avalonia.Controls.Control GetControl() => _separator;
}
