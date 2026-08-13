using AvaloniaEdit.Document;

namespace Downer.Views;

/// <summary>One open document: its text buffer, file identity, and caret memory.</summary>
public sealed class DocumentTab
{
    public TextDocument Document { get; } = new();
    public string? FilePath { get; set; }
    public int CaretOffset { get; set; }

    /// <summary>True while the tab still shows the pristine welcome guide, so it can be reused.</summary>
    public bool IsWelcome { get; set; }

    public bool IsDirty => !Document.UndoStack.IsOriginalFile;
    public string Title => FilePath is null ? "Untitled" : Path.GetFileName(FilePath);
}
