namespace SolidCS.Avalonia;

public abstract class Component : IDisposable
{
    public abstract global::Avalonia.Controls.Control GetControl();

    public virtual void Dispose() { }
}
