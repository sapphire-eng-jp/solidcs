using System;
using System.Collections.Generic;
using System.Linq;

using SolidCS;
using SolidCS.Avalonia;

namespace Todo;

// TODO アイテム。Title は不変、Done だけがアイテム内のリアクティブ状態。
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

    // For + Show のデモ。リストは Signal<IReadOnlyList<T>> のイミュータブル更新規約
    // (todos.Value = [.. todos.Value, item]) — in-place の Add では通知が飛ばない。
    static Component TodoApp()
    {
        var todos = new Signal<IReadOnlyList<TodoItem>>([]);
        var input = new Signal<string>("");
        var showStats = new Signal<bool>(true);

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
            // 入力行: Add ボタンを右にドックし、TextBox が残り幅を埋める
            new DockPanel
            {
                { new Button(() => "Add", Add), Dock.Right },
                new TextBox(input),
            },
            // リスト本体。todos の差し替えで行が増減する (v1 は全破棄方式なので
            // 増減のたびにチェック状態以外のアイテム内 UI 状態はリセットされる)
            For.Each(() => todos.Value, todo => TodoRow(todos, todo)),
            new CheckBox(showStats, () => "show stats"),
            new Show(() => showStats.Value,
                () => new Label(() => $"done: {todos.Value.Count(t => t.Done.Value)} / {todos.Value.Count}"),
                () => new Label(() => "(stats hidden)")),
        };
    }

    static Component TodoRow(Signal<IReadOnlyList<TodoItem>> todos, TodoItem todo) =>
        new StackPanel(Orientation.Horizontal)
        {
            new CheckBox(todo.Done, () => todo.Title),
            new Button(() => "x",
                () => todos.Value = todos.Value.Where(t => t != todo).ToList()),
        };
}
