// SPDX-License-Identifier: MIT

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
        // Assigning NaN (unspecified) directly would match Avalonia's default
        // (content-dependent auto sizing), but we set only when specified
        // to make the intent explicit.
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

        // Dock the menu bar at the top; content fills the rest
        // (DockPanel defaults LastChildFill to true).
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
