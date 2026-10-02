// SPDX-License-Identifier: MIT

namespace SolidCS.Avalonia;

/// <summary>
/// Font settings passed to Component.WithFont. A null member means "unspecified":
/// the theme default / the value from an outer WithFont is used as-is.
/// Family is a string in Avalonia's FontFamily notation ("Consolas", "Inter, Arial", etc.).
/// </summary>
public sealed record Font(
    string? Family = null,
    double? Size = null,
    FontWeight? Weight = null,
    FontStyle? Style = null);

public enum FontWeight
{
    Thin,
    ExtraLight,
    Light,
    Normal,
    Medium,
    SemiBold,
    Bold,
    ExtraBold,
    Black,
}

public enum FontStyle
{
    Normal,
    Italic,
    Oblique,
}

internal static class FontWeightExtensions
{
    public static global::Avalonia.Media.FontWeight ToAvalonia(this FontWeight weight) => weight switch
    {
        FontWeight.Thin => global::Avalonia.Media.FontWeight.Thin,
        FontWeight.ExtraLight => global::Avalonia.Media.FontWeight.ExtraLight,
        FontWeight.Light => global::Avalonia.Media.FontWeight.Light,
        FontWeight.Normal => global::Avalonia.Media.FontWeight.Normal,
        FontWeight.Medium => global::Avalonia.Media.FontWeight.Medium,
        FontWeight.SemiBold => global::Avalonia.Media.FontWeight.SemiBold,
        FontWeight.Bold => global::Avalonia.Media.FontWeight.Bold,
        FontWeight.ExtraBold => global::Avalonia.Media.FontWeight.ExtraBold,
        FontWeight.Black => global::Avalonia.Media.FontWeight.Black,
        _ => throw new ArgumentOutOfRangeException(nameof(weight), weight, null),
    };
}

internal static class FontStyleExtensions
{
    public static global::Avalonia.Media.FontStyle ToAvalonia(this FontStyle style) => style switch
    {
        FontStyle.Normal => global::Avalonia.Media.FontStyle.Normal,
        FontStyle.Italic => global::Avalonia.Media.FontStyle.Italic,
        FontStyle.Oblique => global::Avalonia.Media.FontStyle.Oblique,
        _ => throw new ArgumentOutOfRangeException(nameof(style), style, null),
    };
}
