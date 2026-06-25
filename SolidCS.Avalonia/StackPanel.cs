using System.Collections;

namespace SolidCS.Avalonia;

public sealed class StackPanel : Component, IEnumerable<Component>
{
    private readonly global::Avalonia.Controls.StackPanel _stack;
    private readonly List<Component> _children = new();

    public StackPanel(
        global::Avalonia.Layout.Orientation orientation = global::Avalonia.Layout.Orientation.Vertical)
    {
        _stack = new global::Avalonia.Controls.StackPanel
        {
            Orientation = orientation,
        };
    }

    public void Add(Component child)
    {
        _children.Add(child);
        _stack.Children.Add(child.GetControl());
    }

    public override global::Avalonia.Controls.Control GetControl() => _stack;

    public override void Dispose()
    {
        foreach (var c in _children) c.Dispose();
    }

    public IEnumerator<Component> GetEnumerator() => _children.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
