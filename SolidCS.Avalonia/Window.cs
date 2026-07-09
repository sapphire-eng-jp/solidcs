namespace SolidCS.Avalonia;

public sealed class Window
{
    private readonly global::Avalonia.Controls.Window _window;

    public Window(
        Component content,
        string title = "",
        double width = double.NaN,
        double height = double.NaN,
        Menu? menu = null)
    {
        _window = new global::Avalonia.Controls.Window
        {
            Title = title,
            Content = BuildRoot(content, menu),
        };
        // NaN (未指定) をそのまま代入すると Avalonia のデフォルト (コンテンツ依存の
        // 自動サイズ) と同じだが、意図を明示するため指定時のみ設定する。
        if (!double.IsNaN(width)) _window.Width = width;
        if (!double.IsNaN(height)) _window.Height = height;

        _window.Closed += (_, _) =>
        {
            content.Dispose();
            menu?.Dispose();
        };
    }

    private static global::Avalonia.Controls.Control BuildRoot(Component content, Menu? menu)
    {
        if (menu is null) return content.GetControl();

        // メニューバーを上端にドックし、残り全面を content が埋める
        // (DockPanel は LastChildFill がデフォルト true)。
        var dock = new global::Avalonia.Controls.DockPanel();
        var menuControl = menu.GetControl();
        global::Avalonia.Controls.DockPanel.SetDock(
            menuControl, global::Avalonia.Controls.Dock.Top);
        dock.Children.Add(menuControl);
        dock.Children.Add(content.GetControl());
        return dock;
    }

    internal global::Avalonia.Controls.Window GetWindow() => _window;
}
