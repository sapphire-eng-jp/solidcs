using Avalonia;
using System;
using static Avalonia.Controls.DesktopApplicationExtensions;

using SolidCS;
using SolidCS.Avalonia;

namespace Form;

class Program
{
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp(args)
        .Start(AppMain, args);

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
            Title = "Form",
            Height = 320,
            Width = 480,
        };

        var root = FormDemo();
        win.Content = root.GetControl();
        win.Closed += (_, _) => root.Dispose();

        win.Show();
        app.Run(win);
    }

    static Component FormDemo()
    {
        var name = new Signal<string>("");
        var friendly = new Signal<bool>(true);

        return new StackPanel
        {
            new TextBlock(() => friendly.Value
                ? (name.Value.Length == 0 ? "Hello, stranger!" : $"Hello, {name.Value}!")
                : (name.Value.Length == 0 ? "Hi." : $"Hi {name.Value}.")),
            new TextBox(name),
            new CheckBox(friendly, () => "Use friendly greeting"),
            new Label(() => $"name length = {name.Value.Length}"),
        };
    }
}
