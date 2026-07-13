namespace SolidCS.Avalonia;

/// <summary>
/// ComboBox / ListBox 共通の「items + 選択中 Signal」バインド。
/// ItemsSource には labelFunc で変換した文字列リストを渡し、T との対応は
/// SelectedIndex で取る (Avalonia のテンプレート機構を公開 API に露出させない)。
/// 「未選択」は selected の値が items に見つからない状態 (SelectedIndex = -1) で
/// 表現し、Signal に null や default を書くことはない。
/// </summary>
internal sealed class SelectorBinding<T> : IDisposable
{
    private readonly global::Avalonia.Controls.Primitives.SelectingItemsControl _control;
    private readonly IDisposable _itemsEffect;
    private readonly IDisposable _selectionEffect;
    private List<T> _items = new();

    internal SelectorBinding(
        global::Avalonia.Controls.Primitives.SelectingItemsControl control,
        Signal<T> selected,
        Func<IReadOnlyList<T>> itemsFunc,
        Func<T, string> labelFunc)
    {
        _control = control;

        // items effect: itemsFunc 内で読まれたリスト Signal の変化で作り直す。
        // ラベルもこのタイミングで再計算される — labelFunc 内の Signal は追跡されない
        // (RadioGroup のラベルと違いリアクティブではない) 点に注意。
        _itemsEffect = Context.Instance.RegisterEffect(() =>
        {
            var list = itemsFunc();
            Context.Instance.Untrack(() =>
            {
                _items = new List<T>(list);
                _control.ItemsSource = _items.Select(labelFunc).ToList();
                // ItemsSource 差し替えで選択が飛ぶので selected から再適用する。
                // selected の読取を Untrack 内で行い、items effect が selected を
                // 購読する過追跡を防ぐ。
                ApplySelection(selected.Value);
            });
        });
        // Signal → UI
        _selectionEffect = Context.Instance.RegisterEffect(
            () => ApplySelection(selected.Value)
        );
        // UI → Signal: 実選択 (index >= 0) だけ書き戻す。ItemsSource 差し替え時の
        // -1 は無視。エコーは Signal setter の equality check で止まる。
        _control.SelectionChanged += (_, _) =>
        {
            var idx = _control.SelectedIndex;
            if (idx >= 0 && idx < _items.Count) selected.Value = _items[idx];
        };
    }

    private void ApplySelection(T value)
    {
        // 見つからなければ -1 = 未選択表示
        _control.SelectedIndex = _items.FindIndex(
            x => EqualityComparer<T>.Default.Equals(x, value));
    }

    public void Dispose()
    {
        _itemsEffect.Dispose();
        _selectionEffect.Dispose();
    }
}
