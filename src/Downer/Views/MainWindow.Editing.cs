using Avalonia.Input;
using Avalonia.Interactivity;
using Downer.Core;

namespace Downer.Views;

public partial class MainWindow
{
    private void SetUpEditingBehaviors()
    {
        // Tunnel so we can claim Enter before the editor inserts a plain newline.
        Editor.TextArea.AddHandler(KeyDownEvent, OnEditorKeyDown, RoutingStrategies.Tunnel);
    }

    private void OnEditorKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || e.KeyModifiers != KeyModifiers.None || Editor.SelectionLength > 0)
            return;

        var edit = AutoListContinuation.OnEnter(Editor.Text, Editor.CaretOffset);
        if (edit is null)
            return;

        Editor.Document.Replace(edit.ReplaceStart, edit.ReplaceLength, edit.InsertText);
        Editor.CaretOffset = edit.CaretOffset;
        e.Handled = true;
    }

    private void ApplyFormat(Func<string, int, int, EditResult> transform)
    {
        var result = transform(Editor.Text, Editor.SelectionStart, Editor.SelectionLength);
        ApplyEdit(result);
    }

    /// <summary>Applies an EditResult as a minimal single replace, keeping undo and highlighting sane.</summary>
    private void ApplyEdit(EditResult result)
    {
        var oldText = Editor.Text;
        var newText = result.Text;

        if (!string.Equals(oldText, newText, StringComparison.Ordinal))
        {
            var maxCommon = Math.Min(oldText.Length, newText.Length);
            var prefix = 0;
            while (prefix < maxCommon && oldText[prefix] == newText[prefix])
                prefix++;

            var suffix = 0;
            while (suffix < maxCommon - prefix
                   && oldText[oldText.Length - 1 - suffix] == newText[newText.Length - 1 - suffix])
                suffix++;

            Editor.Document.Replace(
                prefix,
                oldText.Length - prefix - suffix,
                newText.Substring(prefix, newText.Length - prefix - suffix));
        }

        Editor.Select(result.SelectionStart, result.SelectionLength);
        Editor.Focus();
    }

    // ---- Format menu / toolbar ----

    private void OnBold(object? s, RoutedEventArgs e) =>
        ApplyFormat((t, st, ln) => MarkdownFormatter.ToggleInline(t, st, ln, "**", "bold"));

    private void OnItalic(object? s, RoutedEventArgs e) =>
        ApplyFormat((t, st, ln) => MarkdownFormatter.ToggleInline(t, st, ln, "_", "italic"));

    private void OnStrikethrough(object? s, RoutedEventArgs e) =>
        ApplyFormat((t, st, ln) => MarkdownFormatter.ToggleInline(t, st, ln, "~~", "strikethrough"));

    private void OnInlineCode(object? s, RoutedEventArgs e) =>
        ApplyFormat((t, st, ln) => MarkdownFormatter.ToggleInline(t, st, ln, "`", "code"));

    private void OnHeading(object? sender, RoutedEventArgs e)
    {
        if (sender is Avalonia.Controls.Control { Tag: string tag } && int.TryParse(tag, out var level))
            ApplyFormat((t, st, ln) => MarkdownFormatter.SetHeading(t, st, ln, level));
    }

    private void OnBulletList(object? s, RoutedEventArgs e) =>
        ApplyFormat((t, st, ln) => MarkdownFormatter.ToggleLinePrefix(t, st, ln, LinePrefixKind.Bullet));

    private void OnOrderedList(object? s, RoutedEventArgs e) =>
        ApplyFormat((t, st, ln) => MarkdownFormatter.ToggleLinePrefix(t, st, ln, LinePrefixKind.Ordered));

    private void OnTaskList(object? s, RoutedEventArgs e) =>
        ApplyFormat((t, st, ln) => MarkdownFormatter.ToggleLinePrefix(t, st, ln, LinePrefixKind.Task));

    private void OnBlockquote(object? s, RoutedEventArgs e) =>
        ApplyFormat((t, st, ln) => MarkdownFormatter.ToggleLinePrefix(t, st, ln, LinePrefixKind.Quote));

    // ---- Insert menu / toolbar ----

    private void OnInsertLink(object? s, RoutedEventArgs e) =>
        ApplyFormat((t, st, ln) => MarkdownFormatter.InsertLink(t, st, ln));

    private void OnInsertImage(object? s, RoutedEventArgs e) =>
        ApplyFormat((t, st, ln) => MarkdownFormatter.InsertLink(t, st, ln, image: true));

    private void OnInsertTable(object? s, RoutedEventArgs e) =>
        ApplyFormat((t, st, ln) => MarkdownFormatter.InsertTable(t, st, ln));

    private void OnInsertRule(object? s, RoutedEventArgs e) =>
        ApplyFormat(MarkdownFormatter.InsertHorizontalRule);

    private void OnInsertCodeBlock(object? s, RoutedEventArgs e) =>
        ApplyFormat(MarkdownFormatter.InsertCodeBlock);
}
