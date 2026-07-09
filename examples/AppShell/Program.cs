using System;

using SolidCS;
using SolidCS.Avalonia;

namespace AppShell;

class Program
{
    [STAThread]
    public static void Main(string[] args) => App.Run(args,
        () => new Window(Shell(), title: "AppShell", width: 640, height: 400));

    // DockPanel でアプリの外枠を組むデモ。上にヘッダー、下にステータスバー、
    // 左にサイドバーをドックし、最後の子 (メインコンテンツ) が残り全面を埋める。
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
    };
}
