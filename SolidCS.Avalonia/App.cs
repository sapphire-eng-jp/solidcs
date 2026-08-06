using Avalonia;

#if BROWSER
using Avalonia.Browser;
#else
using static Avalonia.Controls.DesktopApplicationExtensions;
#endif

namespace SolidCS.Avalonia;

public static class App
{
    /// <summary>
    /// Run が表示したメインウィンドウ。Dialog の owner に使う。
    /// (Run は IClassicDesktopStyleApplicationLifetime を経由しないので
    /// Avalonia の lifetime からは取れない — ここで自前で持つ。)
    /// browser では常に null (Window が存在しないため Dialog は使えない)。
    /// </summary>
    internal static global::Avalonia.Controls.Window? MainWindow { get; private set; }

#if BROWSER
    /// <summary>
    /// ブラウザ (WebAssembly) 上で Avalonia を初期化し、rootFactory が返す Component を
    /// ページ内の div (divId) にマウントする。ブラウザは single view のみで Window の
    /// 概念が無いため、Run と違い Window ではなく root の Component を直接受け取る。
    /// rootFactory は Avalonia の初期化完了後に UI スレッド上で呼ばれる —
    /// Component の生成は必ずこの中で行うこと。
    /// (args は現状未使用だが、Run と同じ形にして desktop 版との行き来を容易にしている。)
    /// </summary>
    public static Task RunBrowser(string[] args, Func<Component> rootFactory, string divId = "out")
    {
        BrowserApp.RootFactory = rootFactory;
        return AppBuilder.Configure<BrowserApp>()
            .WithInterFont()
            .LogToTrace()
            .StartBrowserAppAsync(divId);
    }

    /// <summary>
    /// StartBrowserAppAsync は OnFrameworkInitializationCompleted 経由で MainView を
    /// 設定する流儀 (lifetime は ISingleViewApplicationLifetime) なので、
    /// 素の Application ではなく専用サブクラスを立てる。
    /// </summary>
    private sealed class BrowserApp : global::Avalonia.Application
    {
        internal static Func<Component>? RootFactory;

        public override void OnFrameworkInitializationCompleted()
        {
            Styles.Add(new global::Avalonia.Themes.Fluent.FluentTheme());
            RequestedThemeVariant = global::Avalonia.Styling.ThemeVariant.Light;

            if (ApplicationLifetime is
                global::Avalonia.Controls.ApplicationLifetimes.ISingleViewApplicationLifetime single)
            {
                single.MainView = RootFactory!().GetControl();
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
#else
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
#endif
}
