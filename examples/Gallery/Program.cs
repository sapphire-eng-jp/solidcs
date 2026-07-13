using System;
using System.Collections.Generic;

using SolidCS;
using SolidCS.Avalonia;

namespace Gallery;

class Program
{
    [STAThread]
    public static void Main(string[] args) => App.Run(args,
        () => new Window(GalleryDemo(), title: "Gallery", width: 520, height: 400));

    static Component GalleryDemo()
    {
        // Combo タブ: 静的な選択肢 (Signal を読まない items は一度だけ評価される)
        var fruit = new Signal<string>("apple");
        string[] fruits = ["apple", "banana", "cherry", "durian"];

        // List タブ: items もリアクティブ — 追加ボタンで選択肢が増える
        var members = new Signal<IReadOnlyList<string>>(["Alice", "Bob"]);
        var member = new Signal<string>("Alice");

        // State タブ: TabControl は全タブの content を保持する (keepAlive 相当) ので
        // タブを離れても入力が消えない。ヘッダーはリアクティブに文字数を出す。
        var note = new Signal<string>("");

        return new TabControl
        {
            { "Combo", new StackPanel
                {
                    ComboBox.Of(fruit, () => fruits, f => f),
                    new Label(() => $"selected: {fruit.Value}"),
                } },
            { () => $"List ({members.Value.Count})", new StackPanel
                {
                    ListBox.Of(member, () => members.Value, m => m),
                    new Button(() => "Add member",
                        () => members.Value = [.. members.Value, $"User{members.Value.Count + 1}"]),
                    new Label(() => $"selected: {member.Value}"),
                } },
            { () => $"State ({note.Value.Length})", new StackPanel
                {
                    new TextBlock(() => "Type here, switch tabs, and come back:"),
                    new TextBox(note),
                } },
        };
    }
}
