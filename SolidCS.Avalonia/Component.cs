namespace SolidCS.Avalonia;

public abstract class Component : IDisposable
{
    private IDisposable? _fontEffect;
    private readonly List<global::Avalonia.Styling.Style> _fontStyles = new(2);

    public abstract global::Avalonia.Controls.Control GetControl();

    /// <summary>Static font settings. Use the Func overload to change fonts at runtime.</summary>
    public Component WithFont(Font font) => WithFont(() => font);

    /// <summary>
    /// Sets the font reactively. Reapplied whenever a Signal read inside fontFunc
    /// changes. The font is applied via an Avalonia Style, so applying it to a
    /// container cascades to all descendants, and with nested WithFont calls the
    /// one closer to the control wins.
    /// </summary>
    public Component WithFont(Func<Font> fontFunc)
    {
        if (_fontEffect is not null)
        {
            throw new InvalidOperationException(
                "WithFont can be called only once per Component. Combine all settings into a single Font.");
        }
        var control = GetControl();
        _fontEffect = Context.Instance.RegisterEffect(() => ApplyFont(control, fontFunc()));
        return this;
    }

    private void ApplyFont(global::Avalonia.Controls.Control control, Font font)
    {
        // On a runtime change (effect re-run), remove the previous Styles before reapplying.
        foreach (var s in _fontStyles) control.Styles.Remove(s);
        _fontStyles.Clear();

        // Why a Style rather than TextElement's inherited attached properties:
        // Fluent's ControlTheme sets FontSize etc. via setters on Button and friends,
        // and inherited values from an ancestor lose to those. A Style takes precedence
        // over a ControlTheme, so applying it to a container reaches even the
        // theme-styled controls. FontSize etc. live on separate types for
        // TemplatedControl-derived controls vs. TextBlock, hence two Styles.
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

        if (templated.Setters.Count == 0) return; // all members null — apply nothing

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
    /// Cleanup for derived classes (disposing effects, propagating to children).
    /// No base call needed — Dispose() takes care of the WithFont effect.
    /// </summary>
    protected virtual void OnDispose() { }
}
