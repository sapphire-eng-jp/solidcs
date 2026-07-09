using System.Collections;

namespace SolidCS.Avalonia;

public sealed class DockPanel : Component, IEnumerable<Component>
{
    private readonly global::Avalonia.Controls.DockPanel _dock;
    private readonly List<Component> _children = new();

    public DockPanel(bool lastChildFill = true)
    {
        _dock = new global::Avalonia.Controls.DockPanel
        {
            LastChildFill = lastChildFill,
        };
    }

    /// <summary>
    /// Dock 指定なしで子を追加する。Avalonia のデフォルト (Left) 扱いだが、
    /// 最後の子は lastChildFill (デフォルト true) により残り全面を埋める。
    /// </summary>
    public void Add(Component child)
    {
        _children.Add(child);
        _dock.Children.Add(child.GetControl());
    }

    public void Add(Component child, Dock dock)
    {
        var control = child.GetControl();
        global::Avalonia.Controls.DockPanel.SetDock(control, dock.ToAvalonia());
        _children.Add(child);
        _dock.Children.Add(control);
    }

    public override global::Avalonia.Controls.Control GetControl() => _dock;

    public override void Dispose()
    {
        foreach (var c in _children) c.Dispose();
    }

    public IEnumerator<Component> GetEnumerator() => _children.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}
