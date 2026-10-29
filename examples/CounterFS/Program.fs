// SPDX-License-Identifier: MIT

module Program

open System
open SolidCS
open SolidCS.Avalonia

// F# has no collection-initializer syntax (`new StackPanel { a, b }` in C#),
// so children are added through small helpers that call `Add` for each item.
// The list elements are upcast to the container's child type implicitly
// because the expected element type is known from the helper's signature.
// `new` is spelled out because Component is IDisposable (FS0760 otherwise).
let private stack (children: Component list) : StackPanel =
    let panel = new StackPanel()
    for child in children do panel.Add child
    panel

let private menu (items: MenuItem list) : Menu =
    let m = new Menu()
    for item in items do m.Add item
    m

let private submenu (children: MenuElement list) (item: MenuItem) : MenuItem =
    for child in children do item.Add child
    item

let private counter (count: Signal<int>) : Component =
    stack [
        new Label(fun () -> $"Count = {count.Value}")
        new Label(fun () -> "Hello World")
        // The Button font used to be hardcoded to 24px in the wrapper; with the
        // introduction of WithFont that hardcoding was removed and the choice
        // moved to the call site.
        (new Button((fun () -> $"+1 ({count.Value})"), (fun () -> count.Value <- count.Value + 1)))
            .WithFont(Font(Size = 24.0))
    ]

let private mainWindow () : Window =
    let count = Signal<int>(0)

    let menuBar =
        menu [
            new MenuItem(fun () -> "File")
            |> submenu [
                new MenuItem((fun () -> $"Reset ({count.Value})"), (fun () -> count.Value <- 0))
                new Separator()
                new MenuItem((fun () -> "Set to 100"), (fun () -> count.Value <- 100))
            ]
            new MenuItem(fun () -> $"Count: {count.Value}")
        ]

    Window(counter count, title = "Counter (F#)", width = 512.0, height = 512.0, menu = menuBar)

[<EntryPoint; STAThread>]
let main args =
    App.Run(args, fun () -> mainWindow ())
    0
