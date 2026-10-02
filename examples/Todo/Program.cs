// SPDX-License-Identifier: MIT

using System;
using System.Collections.Generic;
using System.Linq;

using SolidCS;
using SolidCS.Avalonia;

namespace Todo;

// A TODO item. Title is immutable; Done is the item's only piece of reactive state.
sealed class TodoItem(string title)
{
    public string Title { get; } = title;
    public Signal<bool> Done { get; } = new(false);
}

class Program
{
    [STAThread]
    public static void Main(string[] args) => App.Run(args,
        () => new Window(TodoApp(), title: "Todo", width: 400, height: 480));

    // For + Show demo. The list follows the immutable-update rule for
    // Signal<IReadOnlyList<T>> (todos.Value = [.. todos.Value, item]) —
    // an in-place Add would not trigger a notification.
    static Component TodoApp()
    {
        var todos = new Signal<IReadOnlyList<TodoItem>>([]);
        var input = new Signal<string>("");
        var showStats = new Signal<bool>(true);
        // Running number for row renders. Demonstrates that the keyed diff (For v2)
        // reuses existing row Components — adding or removing rows does not change
        // the #n of the rows that stay.
        var renderCount = 0;

        void Add()
        {
            var title = input.Value.Trim();
            if (title == "") return;
            todos.Value = [.. todos.Value, new TodoItem(title)];
            input.Value = "";
        }

        return new StackPanel
        {
            new Label(() => "SolidCS Todo"),
            // Input row: the Add button is docked right; the TextBox fills the remaining width
            new DockPanel
            {
                { new Button(() => "Add", Add), Dock.Right },
                new TextBox(input),
            },
            // The list itself. Replacing todos adds/removes rows. With key omitted,
            // the item itself is the key (TodoItem is a class, so by reference) —
            // under the immutable-update rule existing instances carry over into
            // the new list, so existing rows are reused.
            For.Each(() => todos.Value, todo => TodoRow(todos, todo, ++renderCount)),
            new CheckBox(showStats, () => "show stats"),
            new Show(() => showStats.Value,
                () => new Label(() => $"done: {todos.Value.Count(t => t.Done.Value)} / {todos.Value.Count}"),
                () => new Label(() => "(stats hidden)")),
        };
    }

    static Component TodoRow(Signal<IReadOnlyList<TodoItem>> todos, TodoItem todo, int renderNo) =>
        new StackPanel(Orientation.Horizontal)
        {
            new CheckBox(todo.Done, () => $"#{renderNo} {todo.Title}"),
            new Button(() => "x",
                () => todos.Value = todos.Value.Where(t => t != todo).ToList()),
        };
}
