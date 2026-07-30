using System;
using System.IO;

using SolidCS;
using SolidCS.Avalonia;

namespace Autosave;

// ライブラリ利用者が Context.RegisterEffect を直接呼ぶ例。
// UI の更新は各コンポーネントが内部の effect でやってくれるので、利用者が
// 自分で effect を作る出番は「UI 以外の副作用を Signal に反応させたいとき」。
// ここではメモの内容 (Signal<string>) が変わるたびファイルへ自動保存する。
class Program
{
    private static readonly string SavePath =
        Path.Combine(Path.GetTempPath(), "solidcs-autosave-memo.txt");

    [STAThread]
    public static void Main(string[] args) => App.Run(args,
        () => new Window(Memo(), title: "Autosave Memo", width: 480, height: 360));

    static Component Memo()
    {
        var text = new Signal<string>(
            File.Exists(SavePath) ? File.ReadAllText(SavePath) : "");
        var enabled = new Signal<bool>(true);
        var status = new Signal<string>("");

        // 保存 effect。中で読んだ Signal (enabled / text) に自動購読され、どちらかが
        // 変わるたび再実行される。登録した瞬間にも 1 回走る (= 起動直後に 1 回保存)。
        // 実アプリならデバウンスを挟むところだが、デモなので毎キーストローク保存。
        // 戻り値の IDisposable で購読を解除できるが、この effect はアプリと同寿命
        // なので破棄しない。
        Context.Instance.RegisterEffect(() =>
        {
            // enabled が false のときは text.Value を読まずに抜ける。effect は実行の
            // たびに依存を張り直すので、この間は text が変わっても再実行されない
            // (依存の動的再追跡)。再びオンにすると enabled の変化で 1 回走り、
            // そこで text を読むので最新の内容がまとめて保存される。
            if (!enabled.Value)
            {
                status.Value = "autosave paused";
                return;
            }
            var content = text.Value;
            File.WriteAllText(SavePath, content);
            // effect の中から別の Signal へ書くのは OK — 読みが購読、書きは通知。
            // status を表示する Label 側の effect が拾って画面が更新される。
            status.Value = $"saved {content.Length} chars at {DateTime.Now:HH:mm:ss}";
        });

        return new DockPanel
        {
            {
                new StackPanel
                {
                    new TextBlock(() => $"file: {SavePath}",
                        HorizontalAlignment.Left, TextWrapping.Wrap),
                    new CheckBox(enabled, () => "Autosave"),
                },
                Dock.Top
            },
            { new Label(() => status.Value, HorizontalAlignment.Left), Dock.Bottom },
            // lastChildFill でメモ欄が残り全面を埋める
            new TextBox(text, multiline: true),
        };
    }
}
