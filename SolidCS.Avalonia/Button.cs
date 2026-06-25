using Avalonia.Layout;

namespace SolidCS.Avalonia;

public sealed class Button : Component
{
    private readonly global::Avalonia.Controls.Button _button;

    public Button(Action onClick)
    {
        _button = new global::Avalonia.Controls.Button
        {
            Content = "Press Me!",
            FontSize = 24,
            HorizontalAlignment = HorizontalAlignment.Center,
        };
        _button.Click += (_, _) => onClick();
    }

    public override global::Avalonia.Controls.Control GetControl() => _button;
}
