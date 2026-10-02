// SPDX-License-Identifier: MIT

using System;
using System.Linq;

using SolidCS;
using SolidCS.Avalonia;

namespace Form;

class Program
{
    [STAThread]
    public static void Main(string[] args) => App.Run(args,
        () => new Window(FormDemo(), title: "Form", width: 480, height: 320));

    static Component FormDemo()
    {
        var name = new Signal<string>("");
        var friendly = new Signal<bool>(true);
        var fontSize = new Signal<double>(14);
        var volume = new Signal<double>(30);
        var qty = new Signal<decimal>(1);
        var mood = new Signal<string>("calm");

        // Content taller than the window (320px) wrapped in a ScrollViewer.
        // Every line reacts to name, so you can confirm reactive updates keep
        // working even in the scrolled-away parts.
        // The root's WithFont (Func overload) reads the fontSize Signal, so the
        // A-/A+ buttons change the whole form's font size at runtime
        // (runtime font change demo).
        return new ScrollViewer(new StackPanel
        {
            // The greeting line is overridden with a static WithFont — the inner
            // (closer) one wins, so it stays 20px Bold and does not follow the
            // root's size changes.
            new TextBlock(() => friendly.Value
                ? (name.Value.Length == 0 ? "Hello, stranger!" : $"Hello, {name.Value}!")
                : (name.Value.Length == 0 ? "Hi." : $"Hi {name.Value}."))
                .WithFont(new Font(Size: 20, Weight: FontWeight.Bold)),

            // Controls for the runtime font change. Stretch makes each Button fill its cell.
            new Grid(columns: "*,Auto,*")
            {
                new Button(() => "A-", () => fontSize.Value = Math.Max(8, fontSize.Value - 2),
                    HorizontalAlignment.Stretch),
                new Label(() => $"{fontSize.Value:0}px"),
                new Button(() => "A+", () => fontSize.Value = Math.Min(32, fontSize.Value + 2),
                    HorizontalAlignment.Stretch),
            },

            // Grid auto-flow mode: rows omitted, children flow left to right;
            // the label column sizes to content (Auto), the input takes the rest (*).
            new Grid(columns: "Auto,*")
            {
                new Label(() => "Name"), new TextBox(name),
                new Label(() => "Style"), new CheckBox(friendly, () => "Use friendly greeting"),
            },

            // Demo of the Phase 1 controls: Slider and ProgressBar share the same
            // Signal (move the Slider and the bar follows), and the
            // NumericUpDown / RadioGroup values are reactively reflected in the
            // summary line below.
            new Grid(columns: "Auto,*")
            {
                new Label(() => "Volume"), new Slider(volume),
                new Label(() => "Qty"), new NumericUpDown(qty, min: 0, max: 99),
                new Label(() => "Mood"), RadioGroup.Of(mood, ["calm", "hyper", "sleepy"],
                    m => m, Orientation.Horizontal),
            },
            new ProgressBar(() => volume.Value),
            new TextBlock(() =>
                $"volume {volume.Value:0}% / qty {qty.Value} / {mood.Value} mood"),

            // Grid explicit mode: two stat cells + a summary row with colSpan 2.
            new Grid(rows: "Auto,Auto", columns: "*,*")
            {
                { new Label(() => $"chars: {name.Value.Length}"), 0, 0 },
                { new Label(() => $"mode: {(friendly.Value ? "friendly" : "plain")}"), 0, 1 },
                { new Label(() => $"greeting for \"{name.Value}\""), 1, 0, 1, 2 },
            },

            new TextBlock(() => string.Join("\n",
                Enumerable.Range(1, 40).Select(i =>
                    $"{i,2}: {(name.Value.Length == 0 ? "(no name)" : name.Value)}"))),
        }.WithFont(() => new Font(Size: fontSize.Value)));
    }
}
