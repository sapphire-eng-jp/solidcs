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
    /// The main window shown by Run. Used as the owner for Dialog.
    /// (Run does not go through IClassicDesktopStyleApplicationLifetime, so the
    /// window cannot be obtained from Avalonia's lifetime — we keep it ourselves.)
    /// Always null in the browser (there are no Windows, so Dialog is unusable).
    /// </summary>
    internal static global::Avalonia.Controls.Window? MainWindow { get; private set; }

#if BROWSER
    /// <summary>
    /// Initializes Avalonia in the browser (WebAssembly) and mounts the Component
    /// returned by rootFactory into the page's div (divId). The browser is single
    /// view only and has no notion of a Window, so unlike Run this takes the root
    /// Component directly rather than a Window.
    /// rootFactory is called on the UI thread after Avalonia has finished
    /// initializing — always create Components inside it.
    /// (args is currently unused, but the signature mirrors Run to make moving
    /// between the desktop and browser versions easy.)
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
    /// StartBrowserAppAsync expects MainView to be set from
    /// OnFrameworkInitializationCompleted (the lifetime is
    /// ISingleViewApplicationLifetime), so we use a dedicated subclass
    /// rather than a plain Application.
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
    /// Initializes the Avalonia application, shows the main window, and blocks
    /// until the window is closed.
    /// windowFactory is called on the UI thread after Avalonia has finished
    /// initializing — always create Components / Windows inside it.
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
