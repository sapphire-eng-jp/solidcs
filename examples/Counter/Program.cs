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
        var count = new Signal<int>(0);

        app.Styles.Add(new Avalonia.Themes.Fluent.FluentTheme());
        app.RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Light;

        var win = new Window
        {
            Title = "Counter",
            Height = 512,
            Width = 512,
        };


        var button = new SButton(() => count.Value += 1);
        var label = new SLabel(() => $"Count = {count.Value}");
        var label2 = new SLabel(() => "Hello World");

        var stack = new SStackPanel([label, label2, button]);

        win.Content = stack.GetControl();

        win.Show();
        app.Run(win);
    }

    abstract class Component
    {
        public abstract Control GetControl();
    }

    class SStackPanel : Component
    {
        private StackPanel _stack;

        public SStackPanel(List<Component> children)
        {
            _stack = new StackPanel();
            children.ForEach((x) => _stack.Children.Add(x.GetControl()));
        }

        public override Control GetControl()
        {
            return _stack;
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

        public SLabel(Func<string> _contentFunc)
        {
            _label = new Label
            {
                HorizontalAlignment = HorizontalAlignment.Center,
            };
            ContentFunc = _contentFunc;
            Context.Instance.RegisterEffect(
                    () => _label.Content = ContentFunc()
            );
        }

        public override Control GetControl() => _label;
    }
}
