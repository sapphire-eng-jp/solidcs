using System;
using System.Collections.Generic;
using System.Linq;

using SolidCS;
using SolidCS.Avalonia;

namespace Gallery;

static partial class Pages
{
    static Component ShowPage()
    {
        var visible = new Signal<bool>(true);
        return Page("Show",
            "Conditional rendering. Default is unmount (dispose and rebuild on toggle — state resets); keepAlive: true keeps the component alive. Type in both boxes, then toggle twice.",
            new CheckBox(visible, () => "mounted"),
            new Label(() => "unmount mode (input resets):", HorizontalAlignment.Left),
            new Show(() => visible.Value,
                () => new TextBox(new Signal<string>("")),
                () => new Label(() => "(hidden — will be rebuilt)")),
            new Label(() => "keepAlive mode (input survives):", HorizontalAlignment.Left),
            new Show(() => visible.Value,
                () => new TextBox(new Signal<string>("")),
                keepAlive: true));
    }

    static Component ForPage()
    {
        var todos = new Signal<IReadOnlyList<TodoItem>>([new("try the gallery")]);
        var input = new Signal<string>("");
        // render 通し番号 — keyed diff で既存行が再利用され、増減しても既存行の
        // #n が変わらないことの実証用。
        var renderCount = 0;

        void Add()
        {
            var title = input.Value.Trim();
            if (title == "") return;
            todos.Value = [.. todos.Value, new TodoItem(title)];
            input.Value = "";
        }

        return Page("For",
            "Dynamic list with keyed diff. Immutable update rule: assign a new list (todos.Value = [.. todos.Value, item]). Existing rows keep their #n — they are reused, not re-rendered.",
            new DockPanel
            {
                { new Button(() => "Add", Add), Dock.Right },
                new TextBox(input),
            },
            For.Each(() => todos.Value, todo => TodoRow(todos, todo, ++renderCount)),
            new Label(() => $"done: {todos.Value.Count(t => t.Done.Value)} / {todos.Value.Count}"));
    }

    static Component TodoRow(Signal<IReadOnlyList<TodoItem>> todos, TodoItem todo, int renderNo) =>
        new StackPanel(Orientation.Horizontal)
        {
            new CheckBox(todo.Done, () => $"#{renderNo} {todo.Title}"),
            new Button(() => "x",
                () => todos.Value = todos.Value.Where(t => t != todo).ToList()),
        };

    static Component TabControlPage()
    {
        var note = new Signal<string>("");
        return Page("TabControl",
            "Tabs via collection initializer { header, content }. All contents are built once and stay alive (keepAlive) — the input on the second tab survives switching. Headers can be reactive.",
            new TabControl
            {
                { "First", new Label(() => "static header, static content") },
                { () => $"Note ({note.Value.Length})", new StackPanel
                    {
                        new TextBlock(() => "type, switch tabs, come back:"),
                        new TextBox(note),
                    } },
            });
    }
}

// For ページの行アイテム。Title は不変、Done だけがアイテム内のリアクティブ状態。
sealed class TodoItem(string title)
{
    public string Title { get; } = title;
    public Signal<bool> Done { get; } = new(false);
}
