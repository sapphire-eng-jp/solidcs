using Avalonia;
using System;
using static Avalonia.Controls.DesktopApplicationExtensions;

using SolidCS;
using SolidCS.Avalonia;

namespace Counter;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp(args)
        .Start(AppMain, args);

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp(string[] args)
        => AppBuilder.Configure<Application>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

    public static void AppMain(Application app, string[] args)
    {
        app.Styles.Add(new global::Avalonia.Themes.Fluent.FluentTheme());
        app.RequestedThemeVariant = global::Avalonia.Styling.ThemeVariant.Light;

        var win = new global::Avalonia.Controls.Window
        {
            Title = "Counter",
            Height = 512,
            Width = 512,
        };

        var root = Counter();
        win.Content = root.GetControl();
        win.Closed += (_, _) => root.Dispose();

        win.Show();
        app.Run(win);
    }

    static Component Counter()
    {
        var count = new Signal<int>(0);

        return new StackPanel
        {
            new Label(() => $"Count = {count.Value}"),
            new Label(() => "Hello World"),
            new Button(() => count.Value += 1),
        };
    }
}
