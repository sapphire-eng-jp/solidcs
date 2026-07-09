namespace SolidCS.Avalonia;

public sealed class Window
{
    private readonly global::Avalonia.Controls.Window _window;

    public Window(
        Component content,
        string title = "",
        double width = double.NaN,
        double height = double.NaN)
    {
        _window = new global::Avalonia.Controls.Window
        {
            Title = title,
            Content = content.GetControl(),
        };
        // NaN (未指定) をそのまま代入すると Avalonia のデフォルト (コンテンツ依存の
        // 自動サイズ) と同じだが、意図を明示するため指定時のみ設定する。
        if (!double.IsNaN(width)) _window.Width = width;
        if (!double.IsNaN(height)) _window.Height = height;

        _window.Closed += (_, _) => content.Dispose();
    }

    internal global::Avalonia.Controls.Window GetWindow() => _window;
}
