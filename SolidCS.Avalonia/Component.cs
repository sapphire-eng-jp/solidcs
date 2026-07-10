namespace SolidCS.Avalonia;

public abstract class Component : IDisposable
{
    private IDisposable? _fontEffect;
    private readonly List<global::Avalonia.Styling.Style> _fontStyles = new(2);

    public abstract global::Avalonia.Controls.Control GetControl();

    /// <summary>静的なフォント設定。実行時に変えたい場合は Func 版を使う。</summary>
    public Component WithFont(Font font) => WithFont(() => font);

    /// <summary>
    /// フォントをリアクティブに設定する。fontFunc 内で読まれた Signal が変わるたび
    /// 再適用される。適用は Avalonia の Style で行うため、コンテナに当てると子孫全体へ
    /// カスケードし、ネストした WithFont は control に近い方が勝つ。
    /// </summary>
    public Component WithFont(Func<Font> fontFunc)
    {
        if (_fontEffect is not null)
        {
            throw new InvalidOperationException(
                "WithFont は 1 つの Component に 1 回だけ呼べる。設定は 1 つの Font にまとめること。");
        }
        var control = GetControl();
        _fontEffect = Context.Instance.RegisterEffect(() => ApplyFont(control, fontFunc()));
        return this;
    }

    private void ApplyFont(global::Avalonia.Controls.Control control, Font font)
    {
        // 実行時変更 (effect の再実行) では前回の Style を外してから当て直す。
        foreach (var s in _fontStyles) control.Styles.Remove(s);
        _fontStyles.Clear();

        // TextElement の継承 attached property ではなく Style で当てる理由:
        // Fluent の ControlTheme は Button 等の FontSize を setter で持っており、
        // 祖先からの継承値はそれに負ける。Style は ControlTheme より優先されるので
        // コンテナに当ててもテーマ持ちのコントロールまで届く。
        // FontSize 等は TemplatedControl 系と TextBlock が別々の型で持つため 2 本当てる。
        var templated = new global::Avalonia.Styling.Style(x =>
            global::Avalonia.Styling.Selectors.Is<global::Avalonia.Controls.Primitives.TemplatedControl>(x));
        var textBlock = new global::Avalonia.Styling.Style(x =>
            global::Avalonia.Styling.Selectors.Is<global::Avalonia.Controls.TextBlock>(x));

        void Set(
            global::Avalonia.AvaloniaProperty templatedProp,
            global::Avalonia.AvaloniaProperty textBlockProp,
            object value)
        {
            templated.Setters.Add(new global::Avalonia.Styling.Setter(templatedProp, value));
            textBlock.Setters.Add(new global::Avalonia.Styling.Setter(textBlockProp, value));
        }

        if (font.Family is not null)
        {
            Set(global::Avalonia.Controls.Primitives.TemplatedControl.FontFamilyProperty,
                global::Avalonia.Controls.TextBlock.FontFamilyProperty,
                global::Avalonia.Media.FontFamily.Parse(font.Family));
        }
        if (font.Size is not null)
        {
            Set(global::Avalonia.Controls.Primitives.TemplatedControl.FontSizeProperty,
                global::Avalonia.Controls.TextBlock.FontSizeProperty,
                font.Size.Value);
        }
        if (font.Weight is not null)
        {
            Set(global::Avalonia.Controls.Primitives.TemplatedControl.FontWeightProperty,
                global::Avalonia.Controls.TextBlock.FontWeightProperty,
                font.Weight.Value.ToAvalonia());
        }
        if (font.Style is not null)
        {
            Set(global::Avalonia.Controls.Primitives.TemplatedControl.FontStyleProperty,
                global::Avalonia.Controls.TextBlock.FontStyleProperty,
                font.Style.Value.ToAvalonia());
        }

        if (templated.Setters.Count == 0) return; // 全メンバー null なら何も当てない

        control.Styles.Add(templated);
        control.Styles.Add(textBlock);
        _fontStyles.Add(templated);
        _fontStyles.Add(textBlock);
    }

    public void Dispose()
    {
        _fontEffect?.Dispose();
        OnDispose();
    }

    /// <summary>
    /// 派生クラスの解放処理 (effect の解放・子への伝播)。base 呼び出しは不要 —
    /// WithFont の effect は Dispose() 側が解放する。
    /// </summary>
    protected virtual void OnDispose() { }
}
