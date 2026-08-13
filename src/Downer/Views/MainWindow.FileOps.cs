using Avalonia.Platform.Storage;
using Downer.Dialogs;

namespace Downer.Views;

public partial class MainWindow
{
    private static readonly FilePickerFileType MarkdownFileType = new("Markdown")
    {
        Patterns = new[] { "*.md", "*.markdown", "*.mdown", "*.mkd" },
        AppleUniformTypeIdentifiers = new[] { "net.daringfireball.markdown" },
        MimeTypes = new[] { "text/markdown" },
    };

    private static readonly FilePickerFileType HtmlFileType = new("HTML")
    {
        Patterns = new[] { "*.html", "*.htm" },
        AppleUniformTypeIdentifiers = new[] { "public.html" },
        MimeTypes = new[] { "text/html" },
    };

    /// <summary>File > New Tab: a fresh untitled document, no questions asked.</summary>
    private Task NewFileAsync()
    {
        ActivateTab(AddTab());
        return Task.CompletedTask;
    }

    private async Task OpenFileAsync()
    {
        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Markdown File",
            AllowMultiple = true,
            FileTypeFilter = new[] { MarkdownFileType, FilePickerFileTypes.TextPlain, FilePickerFileTypes.All },
        });

        foreach (var file in files)
        {
            var path = file.TryGetLocalPath();
            if (path is not null)
                await LoadFileAsync(path);
        }
    }

    /// <summary>
    /// Opens a file into a tab: an existing tab with the same path is activated,
    /// a reusable (empty/welcome) active tab is filled, anything else gets a new tab.
    /// </summary>
    internal async Task LoadFileAsync(string path)
    {
        path = Path.GetFullPath(path);

        var existing = _tabs.FirstOrDefault(t =>
            t.FilePath is not null && string.Equals(t.FilePath, path, StringComparison.OrdinalIgnoreCase));
        if (existing is not null)
        {
            ActivateTab(existing);
            return;
        }

        try
        {
            var text = await File.ReadAllTextAsync(path);
            ActivateTab(TabForNewContent());
            SetDocumentText(text, path);
            RememberRecentFile(path);
        }
        catch (Exception ex)
        {
            if (ex is FileNotFoundException or DirectoryNotFoundException)
                ForgetRecentFile(path);
            await AppDialogs.InfoAsync(this, "Could not open file", ex.Message);
        }
    }

    private void SetDocumentText(string text, string? path)
    {
        Editor.Document.Text = text;
        Editor.Document.UndoStack.ClearAll();
        Editor.Document.UndoStack.MarkAsOriginalFile();
        Editor.CaretOffset = 0;
        CurrentFilePath = path;
        _activeTab.IsWelcome = false;
        UpdateWindowChrome();
        UpdateCaretStatus();
    }

    /// <summary>Saves to the current path, or falls back to Save As. Returns true on success.</summary>
    private async Task<bool> SaveAsync()
    {
        if (CurrentFilePath is null)
            return await SaveAsAsync();

        return await WriteToFileAsync(CurrentFilePath);
    }

    private async Task<bool> SaveAsAsync()
    {
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save Markdown File",
            SuggestedFileName = CurrentFilePath is null ? "Untitled.md" : Path.GetFileName(CurrentFilePath),
            DefaultExtension = "md",
            FileTypeChoices = new[] { MarkdownFileType, FilePickerFileTypes.TextPlain },
        });

        var path = file?.TryGetLocalPath();
        if (path is null)
            return false;

        return await WriteToFileAsync(path);
    }

    private static readonly FilePickerFileType PdfFileType = new("PDF")
    {
        Patterns = new[] { "*.pdf" },
        AppleUniformTypeIdentifiers = new[] { "com.adobe.pdf" },
        MimeTypes = new[] { "application/pdf" },
    };

    private void OnExportHtml(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => _ = ExportHtmlAsync();

    private void OnExportPdf(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => _ = ExportPdfAsync();

    private async Task ExportPdfAsync()
    {
        var baseName = CurrentFilePath is null
            ? "Untitled"
            : Path.GetFileNameWithoutExtension(CurrentFilePath);

        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export as PDF",
            SuggestedFileName = baseName + ".pdf",
            DefaultExtension = "pdf",
            FileTypeChoices = new[] { PdfFileType },
        });

        var path = file?.TryGetLocalPath();
        if (path is null)
            return;

        try
        {
            var pdf = Core.PdfExporter.Export(Editor.Text, baseName);
            await File.WriteAllBytesAsync(path, pdf);
        }
        catch (Exception ex)
        {
            await AppDialogs.InfoAsync(this, "Could not export PDF", ex.Message);
        }
    }

    private async Task ExportHtmlAsync()
    {
        var baseName = CurrentFilePath is null
            ? "Untitled"
            : Path.GetFileNameWithoutExtension(CurrentFilePath);

        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export as HTML",
            SuggestedFileName = baseName + ".html",
            DefaultExtension = "html",
            FileTypeChoices = new[] { HtmlFileType },
        });

        var path = file?.TryGetLocalPath();
        if (path is null)
            return;

        try
        {
            var html = Core.HtmlExporter.ToHtmlDocument(Editor.Text, baseName);
            await File.WriteAllTextAsync(path, html);
        }
        catch (Exception ex)
        {
            await AppDialogs.InfoAsync(this, "Could not export HTML", ex.Message);
        }
    }

    private async Task<bool> WriteToFileAsync(string path)
    {
        try
        {
            await File.WriteAllTextAsync(path, Editor.Text);
            CurrentFilePath = path;
            Editor.Document.UndoStack.MarkAsOriginalFile();
            UpdateWindowChrome();
            RememberRecentFile(path);
            return true;
        }
        catch (Exception ex)
        {
            await AppDialogs.InfoAsync(this, "Could not save file", ex.Message);
            return false;
        }
    }
}
