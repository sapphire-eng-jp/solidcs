// SPDX-License-Identifier: MIT

using System.Collections;

namespace SolidCS.Avalonia;

/// <summary>
/// Tab switching. Add tabs via `{ header, content }` in a collection initializer.
/// header accepts both a static string and a reactive Func&lt;string&gt;.
/// content for every tab is built at construction time and stays alive across tab
/// switches (keepAlive-equivalent — input state is preserved). To rebuild on each
/// switch, wrap the content in Show.
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
