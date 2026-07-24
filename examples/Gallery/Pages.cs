using System;

using SolidCS.Avalonia;

namespace Gallery;

// ページの一覧と共通レイアウト。各ページの実体はカテゴリ別の partial ファイル
// (Pages.Basic.cs / Pages.Input.cs / ...) にある。
static partial class Pages
{
    // sidebar とページ切替 (Program.Content) の両方がこの一覧から組まれる。
    // Name はページ切替の key なので全カテゴリを通して一意であること
    // (重複は Content の ToDictionary で fail-fast)。
    public static readonly
        (string Category, (string Name, Func<Component> Create)[] Items)[] Catalog =
    [
        ("Basic",
            [("Label", LabelPage), ("TextBlock", TextBlockPage), ("Button", ButtonPage)]),
        ("Input",
            [("TextBox", TextBoxPage), ("CheckBox", CheckBoxPage), ("Slider", SliderPage),
             ("NumericUpDown", NumericUpDownPage), ("RadioGroup", RadioGroupPage)]),
        ("Selection",
            [("ComboBox", ComboBoxPage), ("ListBox", ListBoxPage), ("TreeView", TreeViewPage)]),
        ("Display",
            [("ProgressBar", ProgressBarPage), ("Font", FontPage)]),
        ("Layout",
            [("StackPanel", StackPanelPage), ("WrapPanel", WrapPanelPage),
             ("DockPanel", DockPanelPage), ("Grid", GridPage), ("ScrollViewer", ScrollViewerPage)]),
        ("Dynamic",
            [("Show", ShowPage), ("For", ForPage), ("TabControl", TabControlPage)]),
        ("Window",
            [("Dialog", DialogPage), ("Menu", MenuPage)]),
    ];

    // 見出しの共通スタイル (ページタイトルと sidebar のカテゴリ見出しで共用)。
    internal static Component Heading(string text, double size) =>
        new TextBlock(() => text, HorizontalAlignment.Left)
            .WithFont(new Font(Size: size, Weight: FontWeight.Bold));

    // ページ共通レイアウト: タイトル + 説明 + デモ本体。
    // TextBlock は折り返さないので description は "\n" で手動改行する。
    static Component Page(string title, string description, params Component[] demo)
    {
        var panel = new StackPanel
        {
            Heading(title, 22),
            new TextBlock(() => description, HorizontalAlignment.Left),
        };
        foreach (var c in demo) panel.Add(c);
        return panel;
    }
}
