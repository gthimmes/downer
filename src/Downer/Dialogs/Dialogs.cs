using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Downer.Dialogs;

public enum UnsavedChoice
{
    Save,
    DontSave,
    Cancel
}

/// <summary>Small code-built modal dialogs (Avalonia has no built-in message box).</summary>
public static class AppDialogs
{
    public static async Task<UnsavedChoice> ConfirmUnsavedAsync(Window owner, string fileName)
    {
        var choice = UnsavedChoice.Cancel;

        var dialog = BuildDialog("Unsaved changes");

        var message = new TextBlock
        {
            Text = $"Do you want to save the changes to “{fileName}”?\n\nYour changes will be lost if you don't save them.",
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 380,
        };

        var save = MakeButton("Save", isDefault: true);
        var dontSave = MakeButton("Don't Save");
        var cancel = MakeButton("Cancel", isCancel: true);

        save.Click += (_, _) => { choice = UnsavedChoice.Save; dialog.Close(); };
        dontSave.Click += (_, _) => { choice = UnsavedChoice.DontSave; dialog.Close(); };
        cancel.Click += (_, _) => { choice = UnsavedChoice.Cancel; dialog.Close(); };

        dialog.Content = BuildLayout(message, save, dontSave, cancel);
        await dialog.ShowDialog(owner);
        return choice;
    }

    public static async Task InfoAsync(Window owner, string title, string message)
    {
        var dialog = BuildDialog(title);

        var text = new TextBlock
        {
            Text = message,
            TextWrapping = TextWrapping.Wrap,
            MaxWidth = 420,
        };

        var ok = MakeButton("OK", isDefault: true, isCancel: true);
        ok.Click += (_, _) => dialog.Close();

        dialog.Content = BuildLayout(text, ok);
        await dialog.ShowDialog(owner);
    }

    private static Window BuildDialog(string title) => new()
    {
        Title = title,
        SizeToContent = SizeToContent.WidthAndHeight,
        CanResize = false,
        ShowInTaskbar = false,
        WindowStartupLocation = WindowStartupLocation.CenterOwner,
    };

    private static Button MakeButton(string caption, bool isDefault = false, bool isCancel = false) => new()
    {
        Content = caption,
        MinWidth = 90,
        HorizontalContentAlignment = HorizontalAlignment.Center,
        IsDefault = isDefault,
        IsCancel = isCancel,
    };

    private static Control BuildLayout(Control message, params Button[] buttons)
    {
        var buttonRow = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8,
        };
        foreach (var button in buttons)
            buttonRow.Children.Add(button);

        var root = new StackPanel { Margin = new Thickness(20), Spacing = 20 };
        root.Children.Add(message);
        root.Children.Add(buttonRow);
        return root;
    }
}
