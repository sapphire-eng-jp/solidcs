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

        var stack = new StackPanel();

        var button = new Button
        {
            Content = "Press Me!",
            FontSize = 24,
            HorizontalAlignment = HorizontalAlignment.Center,
        };

        button.Click += (object? sender, RoutedEventArgs args) => count.Value += 1;

        var label = new SLabel(() => $"Count = {count.Value}");
        var label2 = new SLabel(() => "Hello World");

        stack.Children.Add(label.GetControl());
        stack.Children.Add(label2.GetControl());
        stack.Children.Add(button);
        win.Content = stack;

        win.Show();
        app.Run(win);
    }

    interface IComponent
    {
        public Control GetControl();
    }

    class SStackLabel : IComponent
    {
        private StackPanel _stack;

        public SStackLabel(List<IComponent> children)
        {
            _stack = new StackPanel();
            children.ForEach((x) => _stack.Children.Add(x.GetControl()));
        }

        public Control GetControl()
        {
            return _stack;
        }
    }

    class SLabel : IComponent
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

        public Control GetControl() => _label;
    }
}
