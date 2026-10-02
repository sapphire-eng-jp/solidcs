// SPDX-License-Identifier: MIT

namespace SolidCS.Avalonia;

/// <summary>
/// Marker base class for elements that can go inside a menu (MenuItem / Separator).
/// Menu / MenuItem accept this type in their Add methods so that menu elements
/// can't stray into general containers like StackPanel.
/// </summary>
public abstract class MenuElement : Component
{
}
