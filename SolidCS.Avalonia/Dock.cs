namespace SolidCS.Avalonia;

public enum Dock
{
    Left,
    Top,
    Right,
    Bottom,
}

internal static class DockExtensions
{
    public static global::Avalonia.Controls.Dock ToAvalonia(this Dock dock) => dock switch
    {
        Dock.Left => global::Avalonia.Controls.Dock.Left,
        Dock.Top => global::Avalonia.Controls.Dock.Top,
        Dock.Right => global::Avalonia.Controls.Dock.Right,
        Dock.Bottom => global::Avalonia.Controls.Dock.Bottom,
        _ => throw new ArgumentOutOfRangeException(nameof(dock), dock, null),
    };
}
