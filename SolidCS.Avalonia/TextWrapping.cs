// SPDX-License-Identifier: MIT

namespace SolidCS.Avalonia;

public enum TextWrapping
{
    /// <summary>No wrapping. Overflowing text is clipped (Avalonia default).</summary>
    NoWrap,
    /// <summary>Wrap at word boundaries.</summary>
    Wrap,
    /// <summary>Wrap at word boundaries, but let a single word overflow if it exceeds the width.</summary>
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
