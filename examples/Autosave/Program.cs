using System;
using System.IO;

using SolidCS;
using SolidCS.Avalonia;

namespace Autosave;

// Example of a library user calling Context.RegisterEffect directly.
// UI updates are handled by each component's internal effects, so the time to
// write your own effect is when you want a non-UI side effect to react to a
// Signal. Here, every change to the memo content (Signal<string>) is
// auto-saved to a file.
class Program
{
    private static readonly string SavePath =
        Path.Combine(Path.GetTempPath(), "solidcs-autosave-memo.txt");

    [STAThread]
    public static void Main(string[] args) => App.Run(args,
        () => new Window(Memo(), title: "Autosave Memo", width: 480, height: 360));

    static Component Memo()
    {
        var text = new Signal<string>(
            File.Exists(SavePath) ? File.ReadAllText(SavePath) : "");
        var enabled = new Signal<bool>(true);
        var status = new Signal<string>("");

        // The save effect. It auto-subscribes to the Signals it reads (enabled /
        // text) and re-runs whenever either changes. It also runs once at
        // registration (= one save right after startup). A real app would add
        // debouncing here; this demo saves on every keystroke. The returned
        // IDisposable would unsubscribe, but this effect lives as long as the
        // app, so we never dispose it.
        Context.Instance.RegisterEffect(() =>
        {
            // When enabled is false, bail out without reading text.Value. The
            // effect re-tracks its dependencies on every run, so while disabled
            // it does not re-run on text changes (dynamic dependency
            // re-tracking). Turning it back on runs the effect once via the
            // enabled change, which reads text — so the latest content gets
            // saved in one go.
            if (!enabled.Value)
            {
                status.Value = "autosave paused";
                return;
            }
            var content = text.Value;
            File.WriteAllText(SavePath, content);
            // Writing to another Signal from inside an effect is fine — reads
            // subscribe, writes notify. The Label's effect that displays status
            // picks this up and updates the screen.
            status.Value = $"saved {content.Length} chars at {DateTime.Now:HH:mm:ss}";
        });

        return new DockPanel
        {
            {
                new StackPanel
                {
                    new TextBlock(() => $"file: {SavePath}",
                        HorizontalAlignment.Left, TextWrapping.Wrap),
                    new CheckBox(enabled, () => "Autosave"),
                },
                Dock.Top
            },
            { new Label(() => status.Value, HorizontalAlignment.Left), Dock.Bottom },
            // lastChildFill makes the memo box fill the remaining space
            new TextBox(text, multiline: true),
        };
    }
}
