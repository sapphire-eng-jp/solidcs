using System.Collections;

namespace SolidCS.Avalonia;

public sealed class Menu : Component, IEnumerable<MenuItem>
{
    private readonly global::Avalonia.Controls.Menu _menu = new();
    private readonly List<MenuItem> _items = new();

    public void Add(MenuItem item)
    {
        _items.Add(item);
        _menu.Items.Add(item.GetControl());
    }

    public override global::Avalonia.Controls.Control GetControl() => _menu;

    protected override void OnDispose()
    {
        foreach (var i in _items) i.Dispose();
    }

    public IEnumerator<MenuItem> GetEnumerator() => _items.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
