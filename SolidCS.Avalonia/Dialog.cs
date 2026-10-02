// SPDX-License-Identifier: MIT

namespace SolidCS.Avalonia;

/// <summary>Extension filter for file dialogs. Patterns use the "*.txt" form.</summary>
public sealed record FileFilter(string Name, string[] Patterns);

/// <summary>
/// Modal dialogs. All methods are async, and the continuation after await resumes
/// on the UI thread via Avalonia's SynchronizationContext, so the result can be
/// written straight back into a Signal. Pass an async lambda as a Button's
/// onClick (Action).
/// The owner is the main window shown by App.Run — calling before that
/// fails fast with InvalidOperationException.
/// </summary>
public static class Dialog
{
    private static global::Avalonia.Controls.Window Owner =>
        App.MainWindow ?? throw new InvalidOperationException(
            "Dialog can be used only after App.Run has shown the main window.");

    /// <summary>Notification dialog with only an OK button.</summary>
    public static Task Message(string title, string message, string ok = "OK")
        => ShowBox(title, message, ok, cancelText: null);

    /// <summary>OK / Cancel confirmation dialog. Closing with the X returns false.</summary>
    public static Task<bool> Confirm(
        string title, string message, string ok = "OK", string cancel = "Cancel")
        => ShowBox(title, message, ok, cancel);

    /// <summary>Open-file dialog. Returns the selected path, or null on cancel.</summary>
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

    /// <summary>Save-as dialog. Returns the selected path, or null on cancel.</summary>
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

    // Shared implementation for Message / Confirm. Avalonia has no MessageBox,
    // so we build a small modal Window. The content is static (no reactivity),
    // so plain Avalonia controls suffice — no Components involved.
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
            // If the owner is always-on-top (Topmost), a non-topmost dialog opens
            // behind it — an "invisible modal locks its parent" accident. Inherit
            // the owner's Topmost so the dialog appears in the same z-order band.
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
        // IsDefault / IsCancel make Enter / Esc work out of the box
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

        // Closing with the X returns default(bool) = false
        return await dialog.ShowDialog<bool>(owner);
    }
}
