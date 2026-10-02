// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;

using SolidCS;
using SolidCS.Avalonia;

namespace Gallery;

static partial class Pages
{
    static Component ComboBoxPage()
    {
        var fruit = new Signal<string>("apple");
        string[] fruits = ["apple", "banana", "cherry", "durian"];
        return Page("ComboBox",
            "Drop-down selection: Signal<T> + items + labelFunc. These items are static (no Signal read — evaluated once).",
            ComboBox.Of(fruit, () => fruits, f => f),
            new Label(() => $"selected: {fruit.Value}"),
            new Button(() => "select cherry from code", () => fruit.Value = "cherry"));
    }

    static Component ListBoxPage()
    {
        var members = new Signal<IReadOnlyList<string>>(["Alice", "Bob"]);
        var member = new Signal<string>("Alice");
        return Page("ListBox",
            "Same API as ComboBox. Items are reactive here — adding one rebuilds the list while the selection is kept.",
            ListBox.Of(member, () => members.Value, m => m),
            new Button(() => "Add member",
                () => members.Value = [.. members.Value, $"User{members.Value.Count + 1}"]),
            new Label(() => $"selected: {member.Value}"));
    }

    static Component TreeViewPage()
    {
        // Each node holds its own children Signal, and only the changed node's
        // subtree is updated via keyed diff — for verifying that expansion state
        // survives.
        var docs = new FsNode("docs", new FsNode("readme.md"), new FsNode("spec.md"));
        var src = new FsNode("src", new FsNode("main.cs"));
        var roots = new Signal<IReadOnlyList<FsNode>>([docs, src]);
        var selectedNode = new Signal<FsNode>(docs);
        var childNo = 0;

        return Page("TreeView",
            "Each node has its own children Signal; only the changed subtree updates. Expand a node, add a child — the expansion state survives (keyed diff reuses the TreeViewItem).",
            TreeView.Of(
                () => roots.Value,
                n => $"{n.Name} ({n.Children.Value.Count})",
                n => n.Children.Value,
                selected: selectedNode),
            new Button(() => "Add child to selected", () =>
            {
                var target = selectedNode.Value;
                target.Children.Value = [.. target.Children.Value, new FsNode($"new-{++childNo}")];
            }),
            new Label(() => $"selected: {selectedNode.Value.Name}"));
    }
}

// Node for the TreeView page. Name is immutable; Children is the node's reactive state.
sealed class FsNode(string name, params FsNode[] children)
{
    public string Name { get; } = name;
    public Signal<IReadOnlyList<FsNode>> Children { get; } = new(children);
}
