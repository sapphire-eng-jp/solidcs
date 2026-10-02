// SPDX-License-Identifier: MIT

using System;

using SolidCS.Avalonia;

namespace Gallery;

// The page catalog and shared layout. The pages themselves live in per-category
// partial files (Pages.Basic.cs / Pages.Input.cs / ...).
static partial class Pages
{
    // Both the sidebar and the page switcher (Program.Content) are built from
    // this list. Name is the page-switching key, so it must be unique across all
    // categories (duplicates fail fast in Content's ToDictionary).
    public static readonly
        (string Category, (string Name, Func<Component> Create)[] Items)[] Catalog =
    [
        ("Basic",
            [("Label", LabelPage), ("TextBlock", TextBlockPage), ("Button", ButtonPage)]),
        ("Input",
            [("TextBox", TextBoxPage), ("CheckBox", CheckBoxPage), ("Slider", SliderPage),
             ("NumericUpDown", NumericUpDownPage), ("RadioGroup", RadioGroupPage)]),
        ("Selection",
            [("ComboBox", ComboBoxPage), ("ListBox", ListBoxPage), ("TreeView", TreeViewPage)]),
        ("Display",
            [("ProgressBar", ProgressBarPage), ("Font", FontPage)]),
        ("Layout",
            [("StackPanel", StackPanelPage), ("WrapPanel", WrapPanelPage),
             ("DockPanel", DockPanelPage), ("Grid", GridPage), ("ScrollViewer", ScrollViewerPage)]),
        ("Dynamic",
            [("Show", ShowPage), ("For", ForPage), ("TabControl", TabControlPage)]),
        ("Window",
            [("Dialog", DialogPage), ("Menu", MenuPage)]),
    ];

    // Shared heading style (used by both page titles and sidebar category headings).
    internal static Component Heading(string text, double size) =>
        new TextBlock(() => text, HorizontalAlignment.Left)
            .WithFont(new Font(Size: size, Weight: FontWeight.Bold));

    // Shared page layout: title + description + the demo itself.
    // The description wraps to the pane width (textWrapping: Wrap).
    static Component Page(string title, string description, params Component[] demo)
    {
        var panel = new StackPanel
        {
            Heading(title, 22),
            new TextBlock(() => description, HorizontalAlignment.Left, TextWrapping.Wrap),
        };
        foreach (var c in demo) panel.Add(c);
        return panel;
    }
}
