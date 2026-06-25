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
        effect.Run();
        return effect;
    }

    private Context()
    {
    }
}
