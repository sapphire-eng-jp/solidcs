namespace SolidCS.Avalonia;

public enum ScrollBarVisibility
{
    /// <summary>No scrolling. Content is clipped to the viewport dimensions.</summary>
    Disabled,
    /// <summary>Show the scroll bar only when the content overflows.</summary>
    Auto,
    /// <summary>Scrolling is possible, but no scroll bar is shown.</summary>
    Hidden,
    /// <summary>Always show the scroll bar.</summary>
    Visible,
}

internal static class ScrollBarVisibilityExtensions
{
    public static global::Avalonia.Controls.Primitives.ScrollBarVisibility ToAvalonia(
        this ScrollBarVisibility visibility) => visibility switch
    {
        ScrollBarVisibility.Disabled => global::Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
        ScrollBarVisibility.Auto => global::Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
        ScrollBarVisibility.Hidden => global::Avalonia.Controls.Primitives.ScrollBarVisibility.Hidden,
        ScrollBarVisibility.Visible => global::Avalonia.Controls.Primitives.ScrollBarVisibility.Visible,
        _ => throw new ArgumentOutOfRangeException(nameof(visibility), visibility, null),
    };
}
