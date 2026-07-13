using Avalonia;
using static Avalonia.Controls.DesktopApplicationExtensions;

namespace SolidCS.Avalonia;

public static class App
{
    /// <summary>
    /// Run が表示したメインウィンドウ。Dialog の owner に使う。
    /// (Run は IClassicDesktopStyleApplicationLifetime を経由しないので
    /// Avalonia の lifetime からは取れない — ここで自前で持つ。)
    /// </summary>
    internal static global::Avalonia.Controls.Window? MainWindow { get; private set; }

    /// <summary>
    /// Avalonia アプリケーションを初期化してメインウィンドウを表示し、
    /// ウィンドウが閉じられるまでブロックする。
    /// windowFactory は Avalonia の初期化完了後に UI スレッド上で呼ばれる —
    /// Component / Window の生成は必ずこの中で行うこと。
    /// </summary>
    public static void Run(string[] args, Func<Window> windowFactory)
    {
        AppBuilder.Configure<global::Avalonia.Application>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace()
            .Start((app, _) =>
            {
                app.Styles.Add(new global::Avalonia.Themes.Fluent.FluentTheme());
                app.RequestedThemeVariant = global::Avalonia.Styling.ThemeVariant.Light;

                var win = windowFactory().GetWindow();
                MainWindow = win;
                win.Show();
                app.Run(win);
            }, args);
    }
}
