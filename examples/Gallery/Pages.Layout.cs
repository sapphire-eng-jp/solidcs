using System;
using System.Linq;

using SolidCS;
using SolidCS.Avalonia;

namespace Gallery;

static partial class Pages
{
    static Component StackPanelPage() => Page("StackPanel",
        "Stacks children in one direction. Vertical by default; pass Orientation.Horizontal to go sideways. Nesting works.",
        new StackPanel(Orientation.Horizontal)
        {
            new Label(() => "[A]"), new Label(() => "[B]"), new Label(() => "[C]"),
        },
        new StackPanel
        {
            new Label(() => "vertical 1"),
            new Label(() => "vertical 2"),
        });

    static Component WrapPanelPage()
    {
        var selected = new Signal<string>("(none)");
        var tags = new WrapPanel();
        foreach (var name in Enumerable.Range(1, 12).Select(i => $"tag-{i:00}"))
        {
            tags.Add(new Button(() => name, () => selected.Value = name));
        }
        return Page("WrapPanel",
            "Lays children out in a row and wraps when the width runs out — resize the window to see the wrap point move.",
            tags,
            new Label(() => $"selected: {selected.Value}"));
    }

    static Component DockPanelPage() => Page("DockPanel",
        "Docks children to the edges; the last child fills the rest (lastChildFill). The Gallery shell itself is a DockPanel with this sidebar docked Left.",
        new DockPanel
        {
            { new Label(() => "[ Top ]"), Dock.Top },
            { new Label(() => "[ Bottom ]"), Dock.Bottom },
            { new Label(() => "[ Left ]"), Dock.Left },
            { new Label(() => "[ Right ]"), Dock.Right },
            new Label(() => "[ Fill ]"),
        });

    static Component GridPage()
    {
        var name = new Signal<string>("");
        return Page("Grid",
            "Rows / columns via a mini-language (\"Auto\", \"*\", \"2*\", \"100\"). Auto-flow mode (rows omitted) flows children left to right; explicit mode places them with { child, row, col }.",
            new Grid(columns: "Auto,*")
            {
                new Label(() => "Name"), new TextBox(name),
                new Label(() => "Len"), new Label(() => $"{name.Value.Length}", HorizontalAlignment.Left),
            },
            new Grid(rows: "Auto,Auto", columns: "*,*")
            {
                { new Label(() => "row 0 col 0"), 0, 0 },
                { new Label(() => "row 0 col 1"), 0, 1 },
                { new Label(() => $"colSpan 2: hello {name.Value}"), 1, 0, 1, 2 },
            });
    }

    static Component ScrollViewerPage()
    {
        // Static content that reads no Signal — built once at construction time
        var lines = string.Join("\n",
            Enumerable.Range(1, 40).Select(i => $"line {i,2} of 40"));
        return Page("ScrollViewer",
            "Wraps a single child and makes it scrollable. This content pane is itself a ScrollViewer — the lines below overflow the window, so scroll down.",
            new TextBlock(() => lines));
    }
}
