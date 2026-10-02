// SPDX-License-Identifier: MIT

using System.Collections.Generic;

namespace SolidCS;


public sealed class Context
{
    public static Context Instance {get;} = new();

    // Each logical flow (UI thread / async chain) gets its own instance, so Push/Pop
    // stays safe during effect registration without taking an outer lock.
    private static readonly AsyncLocal<Stack<Effect>> _subscribers = new();

    public Stack<Effect> Subscribers => _subscribers.Value ??= new Stack<Effect>();

    public IDisposable RegisterEffect(Action callback)
    {
        // Push/Pop and dependency tracking are centralized in Effect.Run(), so all we do here is kick off Run once.
        var effect = new Effect(callback);
        try
        {
            effect.Run();
        }
        catch
        {
            // If the initial Run throws partway through, the caller never receives the handle
            // and has no way to release the subscriptions set up so far. Release them here,
            // then rethrow.
            effect.Dispose();
            throw;
        }
        return effect;
    }

    // Runs func with dependency tracking disabled. Used when building child Components inside
    // an effect, to prevent over-tracking where Signals read in a child's constructor get
    // subscribed to the outer effect. We swap in a fresh empty stack rather than pushing a
    // sentinel (null) so that RegisterEffect calls during Untrack (child construction) can
    // still track their own dependencies correctly on the new empty stack.
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
            // saved is null on a flow that was never initialized (AsyncLocal's unset state).
            // In that case, restoring null to bring back the unset state is correct.
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
