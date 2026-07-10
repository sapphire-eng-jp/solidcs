using System.Collections;

namespace SolidCS.Avalonia;

public sealed class WrapPanel : Component, IEnumerable<Component>
{
    private readonly global::Avalonia.Controls.WrapPanel _wrap;
    private readonly List<Component> _children = new();

    // デフォルトは Avalonia と同じ Horizontal (StackPanel の Vertical とは逆) 。
    public WrapPanel(Orientation orientation = Orientation.Horizontal)
    {
        _wrap = new global::Avalonia.Controls.WrapPanel
        {
            Orientation = orientation.ToAvalonia(),
        };
    }

    public void Add(Component child)
    {
        _children.Add(child);
        _wrap.Children.Add(child.GetControl());
    }

    public override global::Avalonia.Controls.Control GetControl() => _wrap;

    protected override void OnDispose()
    {
        foreach (var c in _children) c.Dispose();
    }

    public IEnumerator<Component> GetEnumerator() => _children.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
