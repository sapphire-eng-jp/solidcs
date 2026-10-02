// SPDX-License-Identifier: MIT

namespace SolidCS.Avalonia;

public enum Orientation
{
    Horizontal,
    Vertical,
}

internal static class OrientationExtensions
{
    public static global::Avalonia.Layout.Orientation ToAvalonia(this Orientation orientation) => orientation switch
    {
        Orientation.Horizontal => global::Avalonia.Layout.Orientation.Horizontal,
        Orientation.Vertical => global::Avalonia.Layout.Orientation.Vertical,
        _ => throw new ArgumentOutOfRangeException(nameof(orientation), orientation, null),
    };
}
