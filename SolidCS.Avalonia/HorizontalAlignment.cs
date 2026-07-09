namespace SolidCS.Avalonia;

public enum HorizontalAlignment
{
    Stretch,
    Left,
    Center,
    Right,
}

internal static class HorizontalAlignmentExtensions
{
    public static global::Avalonia.Layout.HorizontalAlignment ToAvalonia(this HorizontalAlignment alignment) => alignment switch
    {
        HorizontalAlignment.Stretch => global::Avalonia.Layout.HorizontalAlignment.Stretch,
        HorizontalAlignment.Left => global::Avalonia.Layout.HorizontalAlignment.Left,
        HorizontalAlignment.Center => global::Avalonia.Layout.HorizontalAlignment.Center,
        HorizontalAlignment.Right => global::Avalonia.Layout.HorizontalAlignment.Right,
        _ => throw new ArgumentOutOfRangeException(nameof(alignment), alignment, null),
    };
}
