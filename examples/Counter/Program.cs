using System;

using SolidCS;
using SolidCS.Avalonia;

namespace Counter;

class Program
{
    [STAThread]
    public static void Main(string[] args) => App.Run(args, MainWindow);

    static Window MainWindow()
    {
        var count = new Signal<int>(0);

        var menu = new Menu
        {
            new MenuItem(() => "File")
            {
                new MenuItem(() => $"Reset ({count.Value})", () => count.Value = 0),
                new Separator(),
                new MenuItem(() => "Set to 100", () => count.Value = 100),
            },
            new MenuItem(() => $"Count: {count.Value}"),
        };

        return new Window(Counter(count),
            title: "Counter", width: 512, height: 512, menu: menu);
    }

    static Component Counter(Signal<int> count) => new StackPanel
    {
        new Label(() => $"Count = {count.Value}"),
        new Label(() => "Hello World"),
        new Button(() => $"+1 ({count.Value})", () => count.Value += 1),
    };
}
