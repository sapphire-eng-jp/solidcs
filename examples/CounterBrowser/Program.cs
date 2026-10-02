// SPDX-License-Identifier: MIT

using System;
using System.Threading.Tasks;

using SolidCS;
using SolidCS.Avalonia;

namespace CounterBrowser;

class Program
{
    // The browser build uses App.RunBrowser instead of App.Run. There is no Window,
    // so we pass the root Component directly (it gets mounted into the
    // <div id="out"> in index.html).
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
