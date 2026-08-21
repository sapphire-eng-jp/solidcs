using System;

using SolidCS;
using SolidCS.Avalonia;

namespace Gallery;

static partial class Pages
{
    // Record of menu clicks. Bridges the menu bar (MainMenu) and the Menu page —
    // both live entirely within this file.
    static readonly Signal<string> MenuLog = new("(nothing clicked yet)");

    // The menu bar passed to the Window's menu argument (called from Program).
    // Click results are shown on the Menu page via MenuLog, and the second
    // top-level item doubles as a reactive header demo.
    public static Menu MainMenu(Signal<string> page) => new()
    {
        new MenuItem(() => "File")
        {
            new MenuItem(() => "New", () => MenuLog.Value = "File > New"),
            new MenuItem(() => "Open", () => MenuLog.Value = "File > Open"),
            new Separator(),
            new MenuItem(() => "Go to Menu page", () => page.Value = "Menu"),
        },
        new MenuItem(() => $"Page: {page.Value}"),
    };

    static Component DialogPage()
    {
        var answer = new Signal<string>("(not asked)");
        var path = new Signal<string>("(no file)");
        FileFilter[] openFilters =
            [new("Text files", ["*.txt", "*.md"]), new("All files", ["*.*"])];
        void SetPath(string? p) => path.Value = p ?? "(cancelled)";
        return Page("Dialog",
            "Modal dialogs as async statics. The await continuation resumes on the UI thread, so results can be written straight back into a Signal. Button onClick takes an async lambda.",
            new Button(() => "Message...", async () =>
                await Dialog.Message("Info", "Hello from SolidCS!")),
            new Button(() => "Confirm...", async () =>
                answer.Value = await Dialog.Confirm("Confirm", "Proceed with the operation?")
                    ? "OK" : "Cancel"),
            new Label(() => $"answer: {answer.Value}"),
            new Button(() => "Open file...", async () =>
                SetPath(await Dialog.OpenFile("Choose a file", openFilters))),
            new Button(() => "Save file...", async () =>
                SetPath(await Dialog.SaveFile("Save as", suggestedName: "notes.txt",
                    filters: [new FileFilter("Text files", ["*.txt"])]))),
            new TextBlock(() => $"file: {path.Value}"));
    }

    static Component MenuPage() => Page("Menu",
        "The menu bar at the top of this window is a Menu passed to the Window (menu: argument). Items have reactive headers — see \"Page: ...\" update as you navigate. Try File > New.",
        new Label(() => $"last clicked: {MenuLog.Value}"));
}
