using System;

using SolidCS;
using SolidCS.Avalonia;

namespace Counter;

class Program
{
    [STAThread]
    public static void Main(string[] args) => App.Run(args,
        () => new Window(Counter(), title: "Counter", width: 512, height: 512));

    static Component Counter()
    {
        var count = new Signal<int>(0);

        return new StackPanel
        {
            new Label(() => $"Count = {count.Value}"),
            new Label(() => "Hello World"),
            new Button(() => $"+1 ({count.Value})", () => count.Value += 1),
        };
    }
}
