namespace SolidCS.Avalonia;

public enum ScrollBarVisibility
{
    /// <summary>スクロールしない。コンテンツはビューポートの寸法に切り詰められる。</summary>
    Disabled,
    /// <summary>コンテンツがはみ出すときだけスクロールバーを表示する。</summary>
    Auto,
    /// <summary>スクロールは可能だがスクロールバーは表示しない。</summary>
    Hidden,
    /// <summary>常にスクロールバーを表示する。</summary>
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
