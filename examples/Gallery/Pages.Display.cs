using System;

using SolidCS;
using SolidCS.Avalonia;

namespace Gallery;

static partial class Pages
{
    static Component ProgressBarPage()
    {
        var progress = new Signal<double>(40);
        return Page("ProgressBar",
            "Display-only (Func<double>, not two-way). The slider and the bars share one Signal; the second bar shows a derived value.",
            new Slider(progress),
            new ProgressBar(() => progress.Value),
            new ProgressBar(() => 100 - progress.Value),
            new Label(() => $"{progress.Value:0}% / inverted {100 - progress.Value:0}%"));
    }

    static Component FontPage()
    {
        var size = new Signal<double>(14);
        return Page("Font",
            "WithFont applies a Style-based font to a component and all its descendants. The Func overload is reactive; a nested WithFont (closer one) wins over the outer.",
            new Grid(columns: "*,Auto,*")
            {
                new Button(() => "A-", () => size.Value = Math.Max(8, size.Value - 2),
                    HorizontalAlignment.Stretch),
                new Label(() => $"{size.Value:0}px"),
                new Button(() => "A+", () => size.Value = Math.Min(32, size.Value + 2),
                    HorizontalAlignment.Stretch),
            },
            new StackPanel
            {
                new Label(() => "this subtree follows the A-/A+ size"),
                new CheckBox(new Signal<bool>(true), () => "checkbox text too"),
            }.WithFont(() => new Font(Size: size.Value)),
            new TextBlock(() => "fixed 18px Bold Italic (nested WithFont wins)")
                .WithFont(new Font(Size: 18, Weight: FontWeight.Bold, Style: FontStyle.Italic)),
            new TextBlock(() => "monospace via Family: \"Consolas\"")
                .WithFont(new Font(Family: "Consolas")));
    }
}
