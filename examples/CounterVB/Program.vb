Imports SolidCS
Imports SolidCS.Avalonia

Module Program
    <STAThread>
    Sub Main(args As String())
        App.Run(args, AddressOf MainWindow)
    End Sub

    Function MainWindow() As Window
        Dim count As New Signal(Of Integer)(0)

        Dim menu As New Menu From {
            New MenuItem(Function() "File") From {
                New MenuItem(Function() $"Reset ({count.Value})", Sub() count.Value = 0),
                New Separator(),
                New MenuItem(Function() "Set to 100", Sub() count.Value = 100)
            },
            New MenuItem(Function() $"Count: {count.Value}")
        }

        Return New Window(Counter(count),
            title:="Counter (VB)", width:=512, height:=512, menu:=menu)
    End Function

    Function Counter(count As Signal(Of Integer)) As Component
        Return New StackPanel From {
            New Label(Function() $"Count = {count.Value}"),
            New Label(Function() "Hello World"),
            New Button(Function() $"+1 ({count.Value})", Sub() count.Value += 1).
                WithFont(New Font(Size:=24))
        }
    End Function
End Module
