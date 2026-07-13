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
        var fontSize = new Signal<double>(14);
        var volume = new Signal<double>(30);
        var qty = new Signal<decimal>(1);
        var mood = new Signal<string>("calm");

        // ウィンドウ (320px) より縦に長いコンテンツを ScrollViewer で包む。
        // 各行が name に反応するので、スクロール先でもリアクティブ更新が生きて
        // いることを確認できる。
        // root の WithFont(Func 版) が fontSize Signal を読むので、A-/A+ ボタンで
        // フォーム全体のフォントサイズが実行時に変わる (実行時フォント変更のデモ)。
        return new ScrollViewer(new StackPanel
        {
            // 挨拶行は WithFont の静的指定で上書き — 内側 (近い方) が勝つので
            // root のサイズ変更に追従せず 20px Bold のまま。
            new TextBlock(() => friendly.Value
                ? (name.Value.Length == 0 ? "Hello, stranger!" : $"Hello, {name.Value}!")
                : (name.Value.Length == 0 ? "Hi." : $"Hi {name.Value}."))
                .WithFont(new Font(Size: 20, Weight: FontWeight.Bold)),

            // 実行時フォント変更の操作列。Button を Stretch でセル幅いっぱいにする。
            new Grid(columns: "*,Auto,*")
            {
                new Button(() => "A-", () => fontSize.Value = Math.Max(8, fontSize.Value - 2),
                    HorizontalAlignment.Stretch),
                new Label(() => $"{fontSize.Value:0}px"),
                new Button(() => "A+", () => fontSize.Value = Math.Min(32, fontSize.Value + 2),
                    HorizontalAlignment.Stretch),
            },

            // Grid の auto-flow モード: rows 省略で子を左→右に流し込み、
            // ラベル列は中身ぶん (Auto)、入力欄が残り全部 (*)。
            new Grid(columns: "Auto,*")
            {
                new Label(() => "Name"), new TextBox(name),
                new Label(() => "Style"), new CheckBox(friendly, () => "Use friendly greeting"),
            },

            // Phase 1 コントロール群のデモ: Slider と ProgressBar は同じ Signal を
            // 共有し (Slider を動かすとバーが追従)、NumericUpDown / RadioGroup の
            // 値は下のサマリ行にリアクティブ反映される。
            new Grid(columns: "Auto,*")
            {
                new Label(() => "Volume"), new Slider(volume),
                new Label(() => "Qty"), new NumericUpDown(qty, min: 0, max: 99),
                new Label(() => "Mood"), RadioGroup.Of(mood, ["calm", "hyper", "sleepy"],
                    m => m, Orientation.Horizontal),
            },
            new ProgressBar(() => volume.Value),
            new TextBlock(() =>
                $"volume {volume.Value:0}% / qty {qty.Value} / {mood.Value} mood"),

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
        }.WithFont(() => new Font(Size: fontSize.Value)));
    }
}
