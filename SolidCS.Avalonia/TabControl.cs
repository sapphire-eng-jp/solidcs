using System.Collections;

namespace SolidCS.Avalonia;

/// <summary>
/// タブ切り替え。collection initializer の `{ header, content }` でタブを足す。
/// header は静的 string と リアクティブ Func&lt;string&gt; の両対応。
/// content は全タブぶん構築時に作られてタブ切替でも生き続ける (keepAlive 相当 —
/// 入力状態が保持される)。切替のたびに作り直したい場合は content を Show で包む。
/// </summary>
public sealed class TabControl : Component, IEnumerable<Component>
{
    private readonly global::Avalonia.Controls.TabControl _tabs;
    private readonly List<Component> _contents = new();
    private readonly List<IDisposable> _effects = new();

    public TabControl()
    {
        _tabs = new global::Avalonia.Controls.TabControl();
    }

    public void Add(string header, Component content) => Add(() => header, content);

    public void Add(Func<string> headerFunc, Component content)
    {
        var item = new global::Avalonia.Controls.TabItem
        {
            Content = content.GetControl(),
        };
        _effects.Add(Context.Instance.RegisterEffect(
            () => item.Header = headerFunc()
        ));
        _contents.Add(content);
        _tabs.Items.Add(item);
    }

    public override global::Avalonia.Controls.Control GetControl() => _tabs;

    protected override void OnDispose()
    {
        foreach (var e in _effects) e.Dispose();
        foreach (var c in _contents) c.Dispose();
    }

    public IEnumerator<Component> GetEnumerator() => _contents.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
