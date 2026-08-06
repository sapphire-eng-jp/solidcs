using System;
using System.Threading.Tasks;

using SolidCS;
using SolidCS.Avalonia;

namespace CounterBrowser;

class Program
{
    // ブラウザ版は App.Run ではなく App.RunBrowser。Window が無いので
    // root の Component を直接渡す (index.html の <div id="out"> にマウントされる)。
    public static Task Main(string[] args) => App.RunBrowser(args, Counter);

    static Component Counter()
    {
        var count = new Signal<int>(0);
        return new StackPanel
        {
            new Label(() => $"Count = {count.Value}"),
            new Label(() => "Hello WebAssembly"),
            new Button(() => $"+1 ({count.Value})", () => count.Value += 1)
                .WithFont(new Font(Size: 24)),
        };
    }
}
