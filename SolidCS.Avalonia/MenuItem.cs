using System.Collections;

namespace SolidCS.Avalonia;

public sealed class MenuItem : MenuElement, IEnumerable<MenuElement>
{
    private readonly global::Avalonia.Controls.MenuItem _item;
    private readonly IDisposable _effect;
    private readonly List<MenuElement> _children = new();

    public MenuItem(Func<string> headerFunc, Action? onClick = null)
    {
        _item = new global::Avalonia.Controls.MenuItem();
        _effect = Context.Instance.RegisterEffect(
            () => _item.Header = headerFunc()
        );
        if (onClick is not null)
        {
            // Click は bubble する routed event なので、子 MenuItem のクリックが
            // 親のハンドラにも届く。自分自身のクリックだけ拾う。
            _item.Click += (_, e) =>
            {
                if (e.Source == _item) onClick();
            };
        }
    }

    public void Add(MenuElement child)
    {
        _children.Add(child);
        _item.Items.Add(child.GetControl());
    }

    public override global::Avalonia.Controls.Control GetControl() => _item;

    public override void Dispose()
    {
        _effect.Dispose();
        foreach (var c in _children) c.Dispose();
    }

    public IEnumerator<MenuElement> GetEnumerator() => _children.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
