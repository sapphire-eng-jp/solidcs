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
            // Click is a bubbling routed event, so clicks on child MenuItems also
            // reach the parent's handler. Only pick up clicks on this item itself.
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

    protected override void OnDispose()
    {
        _effect.Dispose();
        foreach (var c in _children) c.Dispose();
    }

    public IEnumerator<MenuElement> GetEnumerator() => _children.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
