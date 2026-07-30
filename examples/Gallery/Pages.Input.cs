using System;

using SolidCS;
using SolidCS.Avalonia;

namespace Gallery;

static partial class Pages
{
    static Component TextBoxPage()
    {
        var text = new Signal<string>("hello");
        return Page("TextBox",
            "Two-way binding to a Signal<string>. Both boxes below share the same Signal — type in either one.",
            new TextBox(text),
            new TextBox(text),
            new TextBlock(() => $"value: \"{text.Value}\" ({text.Value.Length} chars)"),
            new Button(() => "clear", () => text.Value = ""));
    }

    static Component CheckBoxPage()
    {
        var on = new Signal<bool>(true);
        return Page("CheckBox",
            "Two-way binding to a Signal<bool>. The content label is reactive and can read the same Signal.",
            new CheckBox(on, () => on.Value ? "checked" : "unchecked"),
            new Label(() => $"value: {on.Value}"),
            new Button(() => "toggle from code", () => on.Value = !on.Value));
    }

    static Component SliderPage()
    {
        var volume = new Signal<double>(40);
        var temp = new Signal<double>(20);
        return Page("Slider",
            "Two-way binding to a Signal<double>. min / max default to 0-100; the second slider uses a custom range.",
            new Slider(volume),
            new Label(() => $"volume: {volume.Value:0}"),
            new Slider(temp, min: -20, max: 40),
            new Label(() => $"temperature: {temp.Value:0.0} degrees"));
    }

    static Component NumericUpDownPage()
    {
        var qty = new Signal<decimal>(1);
        var rate = new Signal<decimal>(0.5m);
        return Page("NumericUpDown",
            "Two-way binding to a Signal<decimal>. min / max are optional (null = unbounded); increment defaults to 1.",
            new NumericUpDown(qty, min: 0, max: 99),
            new NumericUpDown(rate, min: 0, max: 1, increment: 0.1m),
            new Label(() => $"qty: {qty.Value} / rate: {rate.Value}"));
    }

    static Component RadioGroupPage()
    {
        var mood = new Signal<string>("calm");
        var size = new Signal<string>("M");
        return Page("RadioGroup",
            "Exclusive selection as a group: Signal<T> + options + labelFunc. Two groups never interfere with each other.",
            RadioGroup.Of(mood, ["calm", "hyper", "sleepy"], m => m, Orientation.Horizontal),
            RadioGroup.Of(size, ["S", "M", "L"], s => $"size {s}"),
            new Label(() => $"mood: {mood.Value} / size: {size.Value}"));
    }
}
