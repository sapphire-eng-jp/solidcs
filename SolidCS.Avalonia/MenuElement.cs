namespace SolidCS.Avalonia;

/// <summary>
/// メニュー内に置ける要素 (MenuItem / Separator) のマーカー基底。
/// StackPanel 等の一般コンテナにメニュー要素が紛れ込まないよう、
/// Menu / MenuItem の Add はこの型で受ける。
/// </summary>
public abstract class MenuElement : Component
{
}
