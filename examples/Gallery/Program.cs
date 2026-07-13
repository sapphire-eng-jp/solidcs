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

        // Dialog タブ: async ダイアログ。await の続きは UI スレッドで再開されるので
        // 結果をそのまま Signal に書き戻せる。
        var answer = new Signal<string>("(not asked)");
        var path = new Signal<string>("(no file)");

        // Tree タブ: 各ノードが自分の children Signal を持ち、変化したノードの
        // サブツリーだけが更新される。選択ノードに Add child しても展開状態が残る
        // (keyed diff によるノード再利用) ことの確認用。
        var docs = new FsNode("docs");
        docs.Children.Value = [new FsNode("readme.md"), new FsNode("spec.md")];
        var src = new FsNode("src");
        src.Children.Value = [new FsNode("main.cs")];
        var roots = new Signal<IReadOnlyList<FsNode>>([docs, src]);
        var selectedNode = new Signal<FsNode>(docs);
        var childNo = 0;

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
            { "Dialog", new StackPanel
                {
                    new Button(() => "Message...", async () =>
                        await Dialog.Message("Info", "Hello from SolidCS!")),
                    new Button(() => "Confirm...", async () =>
                        answer.Value = await Dialog.Confirm("Confirm", "Proceed with the operation?")
                            ? "OK" : "Cancel"),
                    new Label(() => $"answer: {answer.Value}"),
                    new Button(() => "Open file...", async () =>
                        path.Value = await Dialog.OpenFile("Choose a file",
                            [new FileFilter("Text files", ["*.txt", "*.md"]),
                             new FileFilter("All files", ["*.*"])]) ?? "(cancelled)"),
                    new TextBlock(() => $"file: {path.Value}"),
                } },
            { "Tree", new StackPanel
                {
                    TreeView.Of(
                        () => roots.Value,
                        n => $"{n.Name} ({n.Children.Value.Count})", // ラベルも子数にリアクティブ
                        n => n.Children.Value,
                        selected: selectedNode),
                    new Button(() => "Add child", () =>
                    {
                        var target = selectedNode.Value;
                        target.Children.Value = [.. target.Children.Value, new FsNode($"new-{++childNo}")];
                    }),
                    new Label(() => $"selected: {selectedNode.Value.Name}"),
                } },
        };
    }
}

// Tree タブのノード。Name は不変、Children がノード内のリアクティブ状態。
sealed class FsNode(string name)
{
    public string Name { get; } = name;
    public Signal<IReadOnlyList<FsNode>> Children { get; } = new([]);
}
