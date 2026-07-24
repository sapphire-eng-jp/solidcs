using System;
using System.Collections.Generic;
using System.Linq;

using SolidCS;
using SolidCS.Avalonia;

namespace Gallery;

// 対応コンポーネントのショーケース。左ペインのコントロール一覧 (カテゴリ別 ListBox)
// で選択し、右ペインに該当ページを表示する。ページの実体は Pages.*.cs にある。
class Program
{
    [STAThread]
    public static void Main(string[] args) => App.Run(args,
        () =>
        {
            var page = new Signal<string>("Label");
            var root = new DockPanel
            {
                { Sidebar(page), Dock.Left },
                Content(page),
            };
            return new Window(root, title: "Gallery", width: 860, height: 600,
                menu: Pages.MainMenu(page));
        });

    // 左ペイン: カテゴリ見出し + ListBox。全 ListBox が同じ page Signal を共有する —
    // SelectorBinding は「selected の値が items に無い状態 = 未選択 (-1)」なので、
    // 他カテゴリの項目を選ぶと前のカテゴリの選択表示が自然に外れる。
    static Component Sidebar(Signal<string> page)
    {
        var panel = new StackPanel();
        foreach (var (category, items) in Pages.Catalog)
        {
            panel.Add(Pages.Heading(category, 13));
            var names = items.Select(i => i.Name).ToArray();
            panel.Add(ListBox.Of(page, () => names, n => n));
        }
        return new ScrollViewer(panel);
    }

    // 右ペイン: page の値を単一要素の keyed リストとして For に渡すページスイッチャ。
    // page が変わると key が変わり、keyed diff が旧ページを Dispose して新ページ
    // だけを構築する (unmount 切替)。名前→ファクトリの索引は ToDictionary なので
    // Name の重複はここで fail-fast。
    static Component Content(Signal<string> page)
    {
        var byName = Pages.Catalog
            .SelectMany(c => c.Items)
            .ToDictionary(i => i.Name, i => i.Create);
        return new ScrollViewer(
            For.Each(() => (IReadOnlyList<string>)[page.Value], name => byName[name]()));
    }
}
