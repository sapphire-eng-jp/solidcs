# SolidCS

SolidJS-inspired reactive primitives (Signal / Effect) for C#, plus a small
declarative GUI framework for [Avalonia](https://avaloniaui.net/) built on top
of them. Compose desktop (and browser/WebAssembly) UIs from plain C# — no XAML,
no view models, no INotifyPropertyChanged.

> **Status:** experimental. APIs may change without notice.

## Packages

| Package | Description |
|---|---|
| `SolidCS` | UI-framework-agnostic reactive core: `Signal<T>`, `Effect`, `Context.Untrack`. |
| `SolidCS.Avalonia` | Reactive Avalonia components (`Label`, `Button`, `TextBox`, `For`, `Show`, …) and app bootstrap (`App`, `Window`, `Dialog`). Targets desktop and browser (WebAssembly). |

## Quick start

```csharp
using System;
using SolidCS;
using SolidCS.Avalonia;

class Program
{
    [STAThread]
    public static void Main(string[] args) => App.Run(args,
        () => new Window(Counter(), title: "Counter", width: 300, height: 200));

    static Component Counter()
    {
        var count = new Signal<int>(0);
        return new StackPanel
        {
            new Label(() => $"Count = {count.Value}"),
            new Button(() => "+1", () => count.Value += 1),
        };
    }
}
```

Reading `count.Value` inside a component's reactive callback subscribes it
automatically; writing `count.Value` re-runs exactly the effects that depend on
it. `Window` wires up disposal on close, so there is nothing else to manage.

## How it works

- `Signal<T>` holds a value and notifies subscribers when it changes
  (no-op on equal writes).
- `Effect` re-runs its callback when any Signal read during the previous run
  changes, re-tracking dependencies each run — conditional reads just work.
- `SolidCS.Avalonia` components accept `Func<T>` props for anything reactive
  and bind Signals two-way for input controls. Dynamic lists (`For`) and
  conditional rendering (`Show`) use keyed reconciliation, mirroring SolidJS.

Lists follow an immutable-update convention:
`todos.Value = [.. todos.Value, item]` — in-place mutation does not notify.

## Examples

The [`examples/`](examples/) folder contains runnable samples, from a minimal
counter to a full component gallery:

```
dotnet run --project examples/Counter     # minimal counter
dotnet run --project examples/Gallery     # showcase of all components
dotnet run --project examples/Todo        # dynamic lists (For/Show)
dotnet run --project examples/CounterBrowser   # WebAssembly build (requires: dotnet workload install wasm-tools)
```

`examples/CounterVB` shows the library used from VB.NET.

## Building

```
dotnet build SolidCS.sln
dotnet test SolidCS.Tests
```

## License

[MIT](LICENSE)
