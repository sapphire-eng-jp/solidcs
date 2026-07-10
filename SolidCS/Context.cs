using System.Collections.Generic;

namespace SolidCS;


public sealed class Context
{
    public static Context Instance {get;} = new();

    // 論理フロー (UI スレッド / async chain) ごとに別インスタンスを持たせることで、
    // effect 登録時に外側ロックを取らなくても Push/Pop が壊れないようにしている。
    private static readonly AsyncLocal<Stack<Effect>> _subscribers = new();

    public Stack<Effect> Subscribers => _subscribers.Value ??= new Stack<Effect>();

    public IDisposable RegisterEffect(Action callback)
    {
        // Push/Pop と依存追跡は Effect.Run() に集約してあるので、ここでは Run を 1 回起動するだけ。
        var effect = new Effect(callback);
        try
        {
            effect.Run();
        }
        catch
        {
            // 初回 Run が途中で throw すると呼び元はハンドルを受け取れず、途中まで
            // 張られた購読を解除する手段がなくなる。ここで解除してから再 throw する。
            effect.Dispose();
            throw;
        }
        return effect;
    }

    // 依存追跡を切った状態で func を実行する。effect の中で子 Component を構築するとき、
    // 子のコンストラクタで読まれた Signal が外側の effect に購読される過追跡を防ぐのに使う。
    // 番兵 (null) を積む方式ではなくスタックごと空に差し替える方式なのは、Untrack 中の
    // RegisterEffect (子構築) が新しい空スタック上でそのまま正しく依存追跡できるようにするため。
    public T Untrack<T>(Func<T> func)
    {
        var saved = _subscribers.Value;
        _subscribers.Value = new Stack<Effect>();
        try
        {
            return func();
        }
        finally
        {
            // saved は未初期化フローだと null (AsyncLocal の未設定状態)。その場合は
            // null を戻して未設定状態を復元するのが正しい。
            _subscribers.Value = saved!;
        }
    }

    public void Untrack(Action action) => Untrack<object?>(() =>
    {
        action();
        return null;
    });

    private Context()
    {
    }
}
