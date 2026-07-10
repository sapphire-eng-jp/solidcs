namespace SolidCS.Avalonia;

public sealed class ScrollViewer : Component
{
    private readonly global::Avalonia.Controls.ScrollViewer _scrollViewer;
    private readonly Component _content;

    public ScrollViewer(
        Component content,
        ScrollBarVisibility verticalScrollBar = ScrollBarVisibility.Auto,
        ScrollBarVisibility horizontalScrollBar = ScrollBarVisibility.Disabled)
    {
        _content = content;
        _scrollViewer = new global::Avalonia.Controls.ScrollViewer
        {
            Content = content.GetControl(),
            VerticalScrollBarVisibility = verticalScrollBar.ToAvalonia(),
            HorizontalScrollBarVisibility = horizontalScrollBar.ToAvalonia(),
        };
    }

    public override global::Avalonia.Controls.Control GetControl() => _scrollViewer;

    protected override void OnDispose() => _content.Dispose();
}
