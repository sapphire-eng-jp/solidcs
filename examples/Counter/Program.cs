using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using System;
using System.Collections.Generic;

using SolidCS;

namespace Counter;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp(args)
        //.StartWithClassicDesktopLifetime(args);
        .Start(AppMain, args);

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp(string[] args)
        => AppBuilder.Configure<Application>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

    public static void AppMain(Application app, string[] args)
    {

        app.Styles.Add(new Avalonia.Themes.Fluent.FluentTheme());
        app.RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Light;

        var win = new Window
        {
            Title = "Counter",
            Height = 512,
            Width = 512,
        };

        // UI構築はlockして行う
        Component root;
        lock(Context.Instance.Lock)
        {
            //root = new Counter();
            root = Counter2.New();
            win.Content = root.GetControl();
        }

        win.Closed += (_, _) => root.Dispose();

        win.Show();
        app.Run(win);
    }

    static class Counter2
    {
        public static Component New()
        {
            var count = new Signal<int>(0);

            var button = new SButton(() => count.Value += 1);
            var label = new SLabel(() => $"Count = {count.Value}");
            var label2 = new SLabel(() => "Hello World");

            return new SStackPanel([label, label2, button]);
        }
    }

    class Counter : Component
    {
        private SStackPanel _panel;
        private Signal<int> count;

        public Counter()
        {
            count = new Signal<int>(0);

            var button = SButton(() => count.Value += 1);
            var label = SLabel(() => $"Count = {count.Value}");
            var label2 = SLabel(() => "Hello World");

            _panel = SStackPanel([label, label2, button]);
        }

        public override Control GetControl() => _panel.GetControl();

        public override void Dispose() => _panel.Dispose();
    }

    abstract class Component : IDisposable
    {
        public abstract Control GetControl();

        public virtual void Dispose() { }

        protected static SLabel SLabel(Func<string> _contentFunc)
        {
            return new SLabel(_contentFunc);
        }

        protected static SButton SButton(Action _onClick)
        {
            return new SButton(_onClick);
        }

        protected static SStackPanel SStackPanel(List<Component> _children)
        {
            return new SStackPanel(_children);
        }
    }

    class SStackPanel : Component
    {
        private StackPanel _stack;
        private List<Component> _children;

        public SStackPanel(List<Component> children)
        {
            _stack = new StackPanel();
            _children = children;
            children.ForEach((x) => _stack.Children.Add(x.GetControl()));
        }

        public override Control GetControl()
        {
            return _stack;
        }

        public override void Dispose()
        {
            foreach (var c in _children) c.Dispose();
        }
    }

    class SButton : Component
    {
        private Button _button;

        public SButton(Action onClick)
        {
            _button = new Button
            {
                Content = "Press Me!",
                FontSize = 24,
                HorizontalAlignment = HorizontalAlignment.Center,
            };
            _button.Click += (object? sender, RoutedEventArgs args) => onClick();
        }

        public override Control GetControl() => _button;
    }

    class SLabel : Component
    {
        public Func<string> ContentFunc {get;}

        private Label _label;
        private IDisposable _effect;

        public SLabel(Func<string> _contentFunc)
        {
            _label = new Label
            {
                HorizontalAlignment = HorizontalAlignment.Center,
            };
            ContentFunc = _contentFunc;
            _effect = Context.Instance.RegisterEffect(
                    () => _label.Content = ContentFunc()
            );
        }

        public override Control GetControl() => _label;

        public override void Dispose() => _effect.Dispose();
    }
}
