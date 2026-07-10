namespace SolidCS.Avalonia;

/// <summary>
/// 条件付き表示。when が真なら child、偽なら fallback (省略時は空) を表示する。
/// デフォルト (unmount) は条件反転のたびに旧側を Dispose して作り直す —
/// effect が確実に解放され、再表示時は状態リセット (SolidJS の Show と同じ)。
/// keepAlive: true にすると一度構築した Component を保持して Content の
/// 差し替えだけで切り替える — 状態保持・再表示が高速。
/// </summary>
public sealed class Show : Component
{
    private readonly global::Avalonia.Controls.ContentControl _host;
    private readonly Func<Component> _child;
    private readonly Func<Component>? _fallback;
    private readonly bool _keepAlive;
    private readonly IDisposable _effect;

    // unmount モード: 現在マウント中の Component (条件反転で Dispose される)
    private Component? _current;
    // keepAlive モード: 一度構築したら保持し続ける
    private Component? _childKept;
    private Component? _fallbackKept;
    // 直前に描画した条件値 (null は未描画)。when が読む Signal が変わっても条件値が
    // 同じなら子を作り直さないためのガード — 派生条件 (Count > 0 等) では条件が
    // 反転しなくても effect は再実行されるため、これがないと unmount モードで
    // 子が無意味に破棄・再構築される。
    private bool? _lastCond;
    // 構築中の再入検出用 (child/fallback ファクトリが when の読む Signal を書くケース)
    private bool _rendering;

    public Show(
        Func<bool> when,
        Func<Component> child,
        Func<Component>? fallback = null,
        bool keepAlive = false)
    {
        _child = child;
        _fallback = fallback;
        _keepAlive = keepAlive;
        _host = new global::Avalonia.Controls.ContentControl();
        _effect = Context.Instance.RegisterEffect(() =>
        {
            var cond = when(); // ここで読まれた Signal だけを追跡する
            // 子の構築 (コンストラクタ内の Signal 読取) が Show の effect に
            // 購読される過追跡を防ぐため、Untrack の中で行う。
            Context.Instance.Untrack(() => Render(cond));
        });
    }

    private void Render(bool cond)
    {
        // ファクトリが when の読む Signal を書くと構築中にこの effect が再入し、
        // 内側で構築した Component が外側の代入に上書きされてリークする。
        // 黙って壊れる前に fail-fast する。
        if (_rendering)
        {
            throw new InvalidOperationException(
                "Show の child/fallback 構築中に when が読む Signal が書かれた (再入)。ファクトリ内でその Signal を書かないこと。");
        }
        if (cond == _lastCond) return;

        _rendering = true;
        try
        {
            // 先に新しい側を構築してから旧側を破棄する。ファクトリが throw しても
            // 旧側が「Dispose 済みなのに表示されたまま」のゾンビにならない
            // (_lastCond も未更新のままなので次の通知でリトライされる)。
            Component? next;
            if (cond)
            {
                next = _keepAlive ? _childKept ??= _child() : _child();
            }
            else if (_fallback is null)
            {
                next = null;
            }
            else
            {
                next = _keepAlive ? _fallbackKept ??= _fallback() : _fallback();
            }

            if (!_keepAlive)
            {
                _current?.Dispose();
                _current = next;
            }
            _host.Content = next?.GetControl();
            _lastCond = cond;
        }
        finally
        {
            _rendering = false;
        }
    }

    public override global::Avalonia.Controls.Control GetControl() => _host;

    public override void Dispose()
    {
        _effect.Dispose();
        _current?.Dispose();
        _childKept?.Dispose();
        _fallbackKept?.Dispose();
    }
}
