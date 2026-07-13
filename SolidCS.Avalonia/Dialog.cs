namespace SolidCS.Avalonia;

/// <summary>ファイルダイアログの拡張子フィルタ。Patterns は "*.txt" 形式。</summary>
public sealed record FileFilter(string Name, string[] Patterns);

/// <summary>
/// モーダルダイアログ。すべて async で、await の続きは Avalonia の
/// SynchronizationContext により UI スレッドで再開されるので、結果をそのまま
/// Signal に書き戻してよい。Button の onClick (Action) には async ラムダを渡す。
/// owner は App.Run が表示したメインウィンドウ — それ以前の呼び出しは
/// InvalidOperationException で fail-fast。
/// </summary>
public static class Dialog
{
    private static global::Avalonia.Controls.Window Owner =>
        App.MainWindow ?? throw new InvalidOperationException(
            "Dialog は App.Run がメインウィンドウを表示した後にのみ使える。");

    /// <summary>OK ボタンだけの通知ダイアログ。</summary>
    public static Task Message(string title, string message, string ok = "OK")
        => ShowBox(title, message, ok, cancelText: null);

    /// <summary>OK / Cancel の確認ダイアログ。X で閉じた場合は false。</summary>
    public static Task<bool> Confirm(
        string title, string message, string ok = "OK", string cancel = "Cancel")
        => ShowBox(title, message, ok, cancel);

    /// <summary>ファイルを開くダイアログ。選択されたパスを返す。キャンセルは null。</summary>
    public static async Task<string?> OpenFile(
        string title = "Open", IReadOnlyList<FileFilter>? filters = null)
    {
        var files = await Owner.StorageProvider.OpenFilePickerAsync(
            new global::Avalonia.Platform.Storage.FilePickerOpenOptions
            {
                Title = title,
                AllowMultiple = false,
                FileTypeFilter = ToAvalonia(filters),
            });
        return files.Count > 0
            ? global::Avalonia.Platform.Storage.StorageProviderExtensions.TryGetLocalPath(files[0])
            : null;
    }

    /// <summary>名前を付けて保存ダイアログ。選択されたパスを返す。キャンセルは null。</summary>
    public static async Task<string?> SaveFile(
        string title = "Save",
        string? suggestedName = null,
        IReadOnlyList<FileFilter>? filters = null)
    {
        var file = await Owner.StorageProvider.SaveFilePickerAsync(
            new global::Avalonia.Platform.Storage.FilePickerSaveOptions
            {
                Title = title,
                SuggestedFileName = suggestedName,
                FileTypeChoices = ToAvalonia(filters),
            });
        return file is null
            ? null
            : global::Avalonia.Platform.Storage.StorageProviderExtensions.TryGetLocalPath(file);
    }

    private static IReadOnlyList<global::Avalonia.Platform.Storage.FilePickerFileType>? ToAvalonia(
        IReadOnlyList<FileFilter>? filters)
        => filters?.Select(f =>
            new global::Avalonia.Platform.Storage.FilePickerFileType(f.Name)
            {
                Patterns = f.Patterns,
            }).ToList();

    // Message / Confirm 共通の実装。Avalonia に MessageBox が無いので小さな
    // モーダル Window を組む。中身は静的 (リアクティブ性なし) なので Component は
    // 使わず素の Avalonia コントロールで済ませる。
    private static async Task<bool> ShowBox(
        string title, string message, string okText, string? cancelText)
    {
        var owner = Owner;
        var dialog = new global::Avalonia.Controls.Window
        {
            Title = title,
            SizeToContent = global::Avalonia.Controls.SizeToContent.WidthAndHeight,
            CanResize = false,
            ShowInTaskbar = false,
            WindowStartupLocation =
                global::Avalonia.Controls.WindowStartupLocation.CenterOwner,
            // owner が常に最前面 (Topmost) だと非 topmost のダイアログは owner の
            // 裏に開いてしまい「見えないモーダルに親がロックされる」事故になる。
            // owner の Topmost を継承して同じ z-order 帯に出す。
            Topmost = owner.Topmost,
        };
        var text = new global::Avalonia.Controls.TextBlock
        {
            Text = message,
            TextWrapping = global::Avalonia.Media.TextWrapping.Wrap,
            MaxWidth = 400,
            Margin = new global::Avalonia.Thickness(24, 20, 24, 0),
        };
        var buttons = new global::Avalonia.Controls.StackPanel
        {
            Orientation = global::Avalonia.Layout.Orientation.Horizontal,
            HorizontalAlignment = global::Avalonia.Layout.HorizontalAlignment.Right,
            Spacing = 8,
            Margin = new global::Avalonia.Thickness(24, 20, 24, 20),
        };
        // IsDefault / IsCancel で Enter / Esc がそのまま効く
        var okButton = new global::Avalonia.Controls.Button
        {
            Content = okText, IsDefault = true, MinWidth = 80,
            HorizontalContentAlignment = global::Avalonia.Layout.HorizontalAlignment.Center,
        };
        okButton.Click += (_, _) => dialog.Close(true);
        buttons.Children.Add(okButton);
        if (cancelText is not null)
        {
            var cancelButton = new global::Avalonia.Controls.Button
            {
                Content = cancelText, IsCancel = true, MinWidth = 80,
                HorizontalContentAlignment = global::Avalonia.Layout.HorizontalAlignment.Center,
            };
            cancelButton.Click += (_, _) => dialog.Close(false);
            buttons.Children.Add(cancelButton);
        }
        var root = new global::Avalonia.Controls.StackPanel();
        root.Children.Add(text);
        root.Children.Add(buttons);
        dialog.Content = root;

        // X で閉じられた場合は default(bool) = false が返る
        return await dialog.ShowDialog<bool>(owner);
    }
}
