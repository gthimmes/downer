using System.Reflection;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Downer.Dialogs;

namespace Downer.Views;

public partial class MainWindow
{
    private void SetUpDragDrop()
    {
        DragDrop.SetAllowDrop(this, true);
        AddHandler(DragDrop.DragOverEvent, OnDragOver);
        AddHandler(DragDrop.DropEvent, OnDrop);
    }

    // The pre-DataTransfer drag API is obsolete in Avalonia 11.3 but still the
    // stable cross-version surface; revisit when the 12.x migration happens.
#pragma warning disable CS0618
    private void OnDragOver(object? sender, DragEventArgs e) =>
        e.DragEffects = e.Data.Contains(DataFormats.Files) ? DragDropEffects.Copy : DragDropEffects.None;

    private async void OnDrop(object? sender, DragEventArgs e)
    {
        var path = e.Data.GetFiles()?.FirstOrDefault()?.TryGetLocalPath();
        if (path is null || !File.Exists(path))
            return;

        if (await ConfirmLoseChangesAsync())
            await LoadFileAsync(path);
    }
#pragma warning restore CS0618

    // ---- Help menu ----

    private async void OnWelcomeGuide(object? sender, RoutedEventArgs e)
    {
        if (await ConfirmLoseChangesAsync())
            ShowWelcomeDocument();
    }

    private async void OnAbout(object? sender, RoutedEventArgs e)
    {
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "1.0.0";
        await AppDialogs.InfoAsync(this, "About Downer",
            $"Downer {version}\n\n" +
            "A platform-independent markdown editor built with .NET and Avalonia UI.\n\n" +
            "Editor: AvaloniaEdit with TextMate highlighting\n" +
            "Preview: Markdown.Avalonia\n" +
            "Export: Markdig\n\n" +
            "https://github.com/gthimmes/downer");
    }

    private void ShowWelcomeDocument()
    {
        SetDocumentText(WelcomeText, null);
        SchedulePreviewRefresh();
    }

    private const string WelcomeText = """
        # Welcome to Downer

        A fast, platform-independent **markdown editor** for Windows, macOS, and Linux.

        ## Getting started

        You are editing **formatted markdown** right now — headings are big, bold is bold,
        and the syntax markers fade into the background. Press `Ctrl/Cmd+E` to flip to the
        raw *Markdown Source* view and back.

        Want a rendered preview pane too? Use the **View** menu (or `Ctrl/Cmd+1..3`) to
        switch between *Editor*, *Split*, and *Preview* layouts.

        ## Formatting

        Select text and hit the toolbar or these shortcuts:

        | Action | Shortcut |
        | --- | --- |
        | **Bold** | `Ctrl/Cmd+B` |
        | _Italic_ | `Ctrl/Cmd+I` |
        | ~~Strikethrough~~ | `Ctrl/Cmd+Shift+X` |
        | `Inline code` | `Ctrl/Cmd+Shift+C` |
        | [Link](https://example.com) | `Ctrl/Cmd+K` |
        | Formatted / Source view | `Ctrl/Cmd+E` |
        | Bullet list | `Ctrl/Cmd+Shift+8` |
        | Numbered list | `Ctrl/Cmd+Shift+7` |
        | Task list | `Ctrl/Cmd+Shift+9` |
        | Find / Replace | `Ctrl/Cmd+F` / `Ctrl/Cmd+H` |

        ## Lists that keep up with you

        Press `Enter` inside a list and the next marker appears automatically:

        - [x] Write a markdown file
        - [ ] Press Enter at the end of this line
        - [ ] Watch the next checkbox appear

        Press `Enter` on an *empty* item to exit the list.

        ## More

        - Live preview with synced scrolling
        - Export to standalone HTML (`File > Export as HTML…`)
        - Light, dark, and follow-the-OS themes (`View > Theme`)
        - Drag a `.md` file onto the window to open it

        ```csharp
        // Syntax-highlighted code blocks, of course.
        Console.WriteLine("Happy writing!");
        ```

        ---

        *This welcome guide is just an unsaved document — start typing or open a file.*
        """;
}
