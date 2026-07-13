namespace SolidCS.Avalonia;

/// <summary>
/// 排他選択のラジオボタン群。個々の RadioButton の bool を管理させず、
/// 「選択肢のリスト + 選択中の Signal&lt;T&gt;」のグループ単位で扱う。
/// C# はコンストラクタでジェネリック型推論が効かないため、
/// `RadioGroup.Of(...)` の static ファクトリを入口にする (For.Each と同じ流儀)。
/// </summary>
public static class RadioGroup
{
    public static RadioGroupComponent<T> Of<T>(
        Signal<T> selected,
        IEnumerable<T> options,
        Func<T, string> labelFunc,
        Orientation orientation = Orientation.Vertical)
        => new(selected, options, labelFunc, orientation);
}

public sealed class RadioGroupComponent<T> : Component
{
    private readonly global::Avalonia.Controls.StackPanel _panel;
    private readonly List<IDisposable> _effects = new();

    internal RadioGroupComponent(
        Signal<T> selected,
        IEnumerable<T> options,
        Func<T, string> labelFunc,
        Orientation orientation)
    {
        // RadioButton の排他は同一親コンテナ単位で自動グループ化される。
        // 各グループが自前の StackPanel を持つので GroupName の指定は不要 —
        // 同じ画面に複数の RadioGroup を置いても干渉しない。
        _panel = new global::Avalonia.Controls.StackPanel
        {
            Orientation = orientation.ToAvalonia(),
        };
        foreach (var option in options)
        {
            var radio = new global::Avalonia.Controls.RadioButton();
            // ラベルは状態系の流儀どおりリアクティブ (labelFunc 内の Signal 変化で更新)
            _effects.Add(Context.Instance.RegisterEffect(
                () => radio.Content = labelFunc(option)
            ));
            // Signal → UI: 選択中の option だけ IsChecked = true
            _effects.Add(Context.Instance.RegisterEffect(
                () => radio.IsChecked = EqualityComparer<T>.Default.Equals(selected.Value, option)
            ));
            // UI → Signal: チェックされた側だけ拾う (排他で外れる側の false は無視)。
            // エコーは Signal setter の equality check で止まる。
            radio.IsCheckedChanged += (_, _) =>
            {
                if (radio.IsChecked == true) selected.Value = option;
            };
            _panel.Children.Add(radio);
        }
    }

    public override global::Avalonia.Controls.Control GetControl() => _panel;

    protected override void OnDispose()
    {
        foreach (var e in _effects) e.Dispose();
    }
}
