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
            new TextBox(name),
            new CheckBox(friendly, () => "Use friendly greeting"),
            new Label(() => $"name length = {name.Value.Length}"),
            new TextBlock(() => string.Join("\n",
                Enumerable.Range(1, 40).Select(i =>
                    $"{i,2}: {(name.Value.Length == 0 ? "(no name)" : name.Value)}"))),
        });
    }
}
