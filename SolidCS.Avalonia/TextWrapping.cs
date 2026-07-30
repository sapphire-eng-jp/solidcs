namespace SolidCS.Avalonia;

public enum TextWrapping
{
    /// <summary>折り返さない。はみ出した分は切り詰められる (Avalonia デフォルト)。</summary>
    NoWrap,
    /// <summary>単語境界で折り返す。</summary>
    Wrap,
    /// <summary>単語境界で折り返すが、1 語が幅を超える場合ははみ出しを許す。</summary>
    WrapWithOverflow,
}

internal static class TextWrappingExtensions
{
    public static global::Avalonia.Media.TextWrapping ToAvalonia(
        this TextWrapping wrapping) => wrapping switch
    {
        TextWrapping.NoWrap => global::Avalonia.Media.TextWrapping.NoWrap,
        TextWrapping.Wrap => global::Avalonia.Media.TextWrapping.Wrap,
        TextWrapping.WrapWithOverflow => global::Avalonia.Media.TextWrapping.WrapWithOverflow,
        _ => throw new ArgumentOutOfRangeException(nameof(wrapping), wrapping, null),
    };
}
