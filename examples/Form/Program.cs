using System;

using SolidCS;
using SolidCS.Avalonia;

namespace Form;

class Program
{
    [STAThread]
    public static void Main(string[] args) => App.Run(args,
        () => new Window(FormDemo(), title: "Form", width: 480, height: 320));

    static Component FormDemo()
    {
        var name = new Signal<string>("");
        var friendly = new Signal<bool>(true);

        return new StackPanel
        {
            new TextBlock(() => friendly.Value
                ? (name.Value.Length == 0 ? "Hello, stranger!" : $"Hello, {name.Value}!")
                : (name.Value.Length == 0 ? "Hi." : $"Hi {name.Value}.")),
            new TextBox(name),
            new CheckBox(friendly, () => "Use friendly greeting"),
            new Label(() => $"name length = {name.Value.Length}"),
        };
    }
}
