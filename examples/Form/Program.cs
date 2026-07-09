using System;
using System.Linq;

using SolidCS;
using SolidCS.Avalonia;

namespace Form;

class Program
{
    [STAThread]
    public static void Main(string[] args) => App.Run(args,
        () => new Window(FormDemo(), title: "Form", width: 480, height: 320));

    static Component FormDemo()
    {
        var name = new Signal<string>("");
        var friendly = new Signal<bool>(true);

        // ウィンドウ (320px) より縦に長いコンテンツを ScrollViewer で包む。
        // 各行が name に反応するので、スクロール先でもリアクティブ更新が生きて
        // いることを確認できる。
        return new ScrollViewer(new StackPanel
        {
            new TextBlock(() => friendly.Value
                ? (name.Value.Length == 0 ? "Hello, stranger!" : $"Hello, {name.Value}!")
                : (name.Value.Length == 0 ? "Hi." : $"Hi {name.Value}.")),

            // Grid の auto-flow モード: rows 省略で子を左→右に流し込み、
            // ラベル列は中身ぶん (Auto)、入力欄が残り全部 (*)。
            new Grid(columns: "Auto,*")
            {
                new Label(() => "Name"), new TextBox(name),
                new Label(() => "Style"), new CheckBox(friendly, () => "Use friendly greeting"),
            },

            // Grid の explicit モード: 統計 2 セル + colSpan 2 のサマリ行。
            new Grid(rows: "Auto,Auto", columns: "*,*")
            {
                { new Label(() => $"chars: {name.Value.Length}"), 0, 0 },
                { new Label(() => $"mode: {(friendly.Value ? "friendly" : "plain")}"), 0, 1 },
                { new Label(() => $"greeting for \"{name.Value}\""), 1, 0, 1, 2 },
            },

            new TextBlock(() => string.Join("\n",
                Enumerable.Range(1, 40).Select(i =>
                    $"{i,2}: {(name.Value.Length == 0 ? "(no name)" : name.Value)}"))),
        });
    }
}
