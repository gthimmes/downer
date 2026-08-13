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

    /// <summary>Asks the user to save if dirty. Returns true when it is OK to proceed.</summary>
    private async Task<bool> ConfirmLoseChangesAsync()
    {
        if (!IsDirty)
            return true;

        if (AutosaveApplies)
            return await SaveAsync();

        var choice = await AppDialogs.ConfirmUnsavedAsync(this, DisplayFileName);
        return choice switch
        {
            UnsavedChoice.Save => await SaveAsync(),
            UnsavedChoice.DontSave => true,
            _ => false,
        };
    }

    private async Task NewFileAsync()
    {
        if (!await ConfirmLoseChangesAsync())
            return;

        SetDocumentText("", null);
    }

    private async Task OpenFileAsync()
    {
        if (!await ConfirmLoseChangesAsync())
            return;

        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Markdown File",
            AllowMultiple = false,
            FileTypeFilter = new[] { MarkdownFileType, FilePickerFileTypes.TextPlain, FilePickerFileTypes.All },
        });

        var path = files.FirstOrDefault()?.TryGetLocalPath();
        if (path is not null)
            await LoadFileAsync(path);
    }

    internal async Task LoadFileAsync(string path)
    {
        try
        {
            var text = await File.ReadAllTextAsync(path);
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
        _currentFilePath = path;
        UpdateWindowChrome();
        UpdateCaretStatus();
    }

    /// <summary>Saves to the current path, or falls back to Save As. Returns true on success.</summary>
    private async Task<bool> SaveAsync()
    {
        if (_currentFilePath is null)
            return await SaveAsAsync();

        return await WriteToFileAsync(_currentFilePath);
    }

    private async Task<bool> SaveAsAsync()
    {
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save Markdown File",
            SuggestedFileName = _currentFilePath is null ? "Untitled.md" : Path.GetFileName(_currentFilePath),
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
        var baseName = _currentFilePath is null
            ? "Untitled"
            : Path.GetFileNameWithoutExtension(_currentFilePath);

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
        var baseName = _currentFilePath is null
            ? "Untitled"
            : Path.GetFileNameWithoutExtension(_currentFilePath);

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
            _currentFilePath = path;
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
