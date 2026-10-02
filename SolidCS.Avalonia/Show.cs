// SPDX-License-Identifier: MIT

namespace SolidCS.Avalonia;

/// <summary>
/// Conditional rendering. Shows child while when is true, otherwise fallback
/// (empty if omitted). The default (unmount) Disposes the old side and builds
/// the new one every time the condition flips — effects are reliably released
/// and state is reset on re-display (same as SolidJS's Show).
/// With keepAlive: true, a Component built once is kept and switching only
/// swaps the Content — state is preserved and re-display is fast.
/// </summary>
public sealed class Show : Component
{
    private readonly global::Avalonia.Controls.ContentControl _host;
    private readonly Func<Component> _child;
    private readonly Func<Component>? _fallback;
    private readonly bool _keepAlive;
    private readonly IDisposable _effect;

    // unmount mode: the currently mounted Component (Disposed when the condition flips)
    private Component? _current;
    // keepAlive mode: once built, kept forever
    private Component? _childKept;
    private Component? _fallbackKept;
    // The condition value last rendered (null = not rendered yet). Guard that
    // avoids rebuilding the child when a Signal read by when changes but the
    // condition value stays the same — for derived conditions (Count > 0 etc.)
    // the effect re-runs even when the condition doesn't flip, and without
    // this guard unmount mode would pointlessly destroy and rebuild the child.
    private bool? _lastCond;
    // Re-entrancy detection during construction (for the case where the
    // child/fallback factory writes a Signal that when reads)
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
            var cond = when(); // track only the Signals read here
            // Build the child inside Untrack, so that Signal reads in the
            // child's constructor don't get over-tracked into Show's effect.
            Context.Instance.Untrack(() => Render(cond));
        });
    }

    private void Render(bool cond)
    {
        // If a factory writes a Signal that when reads, this effect re-enters
        // during construction and the Component built on the inner call gets
        // overwritten by the outer assignment and leaks.
        // Fail fast instead of breaking silently.
        if (_rendering)
        {
            throw new InvalidOperationException(
                "A Signal read by when was written while Show was building its child/fallback (re-entrancy). Do not write that Signal inside the factory.");
        }
        if (cond == _lastCond) return;

        _rendering = true;
        try
        {
            // Build the new side before destroying the old one. If the factory
            // throws, the old side never becomes a "Disposed but still
            // displayed" zombie (and _lastCond is left unchanged, so the next
            // notification retries).
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

    protected override void OnDispose()
    {
        _effect.Dispose();
        _current?.Dispose();
        _childKept?.Dispose();
        _fallbackKept?.Dispose();
    }
}
