using System;

using SolidCS;
using SolidCS.Avalonia;

namespace Gallery;

static partial class Pages
{
    static Component LabelPage()
    {
        var count = new Signal<int>(0);
        return Page("Label",
            "Caption control. The content func re-runs whenever a Signal\nread inside it changes.",
            new Label(() => "static text"),
            new Label(() => $"reactive: clicked {count.Value} times"),
            new Button(() => "click me", () => count.Value += 1),
            new Label(() => "aligned Left", HorizontalAlignment.Left),
            new Label(() => "aligned Right", HorizontalAlignment.Right));
    }

    static Component TextBlockPage()
    {
        var count = new Signal<int>(0);
        return Page("TextBlock",
            "Lightweight text display. Prefer this over Label for plain\nstrings — Text (string) is cheaper than Content (object).",
            new TextBlock(() => "line one\nline two\nline three (multi-line via \\n)"),
            new TextBlock(() => $"reactive: {count.Value * 2} = {count.Value} * 2"),
            new Button(() => "+1", () => count.Value += 1));
    }

    static Component ButtonPage()
    {
        var count = new Signal<int>(0);
        return Page("Button",
            "Content is reactive like Label. onClick is a plain Action\n(async lambdas are fine too — see the Dialog page).",
            new Button(() => count.Value == 0 ? "never clicked" : $"clicked {count.Value} times",
                () => count.Value += 1),
            new Button(() => "stretched", () => count.Value += 1, HorizontalAlignment.Stretch),
            new Button(() => "reset", () => count.Value = 0));
    }
}
