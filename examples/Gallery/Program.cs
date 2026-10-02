// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Linq;

using SolidCS;
using SolidCS.Avalonia;

namespace Gallery;

// Showcase of the supported components. Pick a control from the left pane
// (per-category ListBoxes) and the matching page shows in the right pane.
// The pages themselves live in Pages.*.cs.
class Program
{
    [STAThread]
    public static void Main(string[] args) => App.Run(args,
        () =>
        {
            var page = new Signal<string>("Label");
            var root = new DockPanel
            {
                { Sidebar(page), Dock.Left },
                Content(page),
            };
            return new Window(root, title: "Gallery", width: 860, height: 600,
                menu: Pages.MainMenu(page));
        });

    // Left pane: category headings + ListBoxes. All ListBoxes share the same page
    // Signal — SelectorBinding treats "selected value not in items" as
    // unselected (-1), so picking an item in another category naturally clears
    // the previous category's selection highlight.
    static Component Sidebar(Signal<string> page)
    {
        var panel = new StackPanel();
        foreach (var (category, items) in Pages.Catalog)
        {
            panel.Add(Pages.Heading(category, 13));
            var names = items.Select(i => i.Name).ToArray();
            panel.Add(ListBox.Of(page, () => names, n => n));
        }
        return new ScrollViewer(panel);
    }

    // Right pane: a page switcher that feeds the page value to For as a
    // single-element keyed list. When page changes, the key changes, and the
    // keyed diff disposes the old page and builds only the new one (unmount
    // switching). The name-to-factory index is a ToDictionary, so duplicate
    // Names fail fast here.
    static Component Content(Signal<string> page)
    {
        var byName = Pages.Catalog
            .SelectMany(c => c.Items)
            .ToDictionary(i => i.Name, i => i.Create);
        return new ScrollViewer(
            For.Each(() => (IReadOnlyList<string>)[page.Value], name => byName[name]()));
    }
}
