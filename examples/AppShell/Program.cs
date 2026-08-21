using System;
using System.Linq;

using SolidCS;
using SolidCS.Avalonia;

namespace AppShell;

class Program
{
    [STAThread]
    public static void Main(string[] args) => App.Run(args,
        () => new Window(Shell(), title: "AppShell", width: 640, height: 400));

    // Demo of building an app shell with DockPanel: header docked Top, status bar
    // Bottom, sidebar Left, and the last child (the main content) fills the rest.
    static Component Shell()
    {
        var page = new Signal<string>("Home");
        var visits = new Signal<int>(0);

        return new DockPanel
        {
            { new Label(() => "SolidCS AppShell Demo"), Dock.Top },
            { new Label(() => $"page = {page.Value} / visits = {visits.Value}",
                HorizontalAlignment.Left), Dock.Bottom },
            { Sidebar(page, visits), Dock.Left },
            Content(page),
        };
    }

    static Component Sidebar(Signal<string> page, Signal<int> visits)
    {
        Action goTo(string name) => () =>
        {
            page.Value = name;
            visits.Value += 1;
        };

        return new StackPanel
        {
            new Button(() => "Home", goTo("Home")),
            new Button(() => "Settings", goTo("Settings")),
            new Button(() => "About", goTo("About")),
        };
    }

    static Component Content(Signal<string> page) => new StackPanel
    {
        new Label(() => $"[ {page.Value} ]"),
        new TextBlock(() => page.Value switch
        {
            "Home" => "Welcome home!",
            "Settings" => "Nothing to configure yet.",
            "About" => "SolidJS-like reactive UI in C#.",
            _ => "???",
        }),
        Tags(),
    };

    // WrapPanel demo. The tag buttons wrap based on the window width
    // (resize the window and the wrap point moves).
    static Component Tags()
    {
        var selected = new Signal<string>("(none)");

        var tags = new WrapPanel();
        foreach (var name in Enumerable.Range(1, 15).Select(i => $"tag-{i:00}"))
        {
            tags.Add(new Button(() => name, () => selected.Value = name));
        }

        return new StackPanel
        {
            new Label(() => $"selected = {selected.Value}"),
            tags,
        };
    }
}
