using System.Text;
using System.Text.RegularExpressions;

namespace Downer.Core;

/// <summary>
/// Pure (text, selection) -> (text, selection) markdown transforms.
/// No UI dependencies so every operation is unit-testable.
/// Italic uses '_' so it never collides with bold's '**'.
/// </summary>
public static partial class MarkdownFormatter
{
    [GeneratedRegex(@"^(?<indent>\s*)(?:(?<task>[-*+][ \t]+\[[ xX]\][ \t]?)|(?<bullet>[-*+][ \t]+)|(?<ordered>\d+[.)][ \t]+))?(?<rest>.*)$")]
    private static partial Regex ListPrefixRegex();

    [GeneratedRegex(@"^(?<indent>\s{0,3})(?<hashes>#{1,6})[ \t]+(?<rest>.*)$")]
    private static partial Regex HeadingRegex();

    [GeneratedRegex(@"^(?<indent>\s*)>[ \t]?(?<rest>.*)$")]
    private static partial Regex QuoteRegex();

    // ---- Inline styles ----

    public static EditResult ToggleInline(string text, int start, int length, string marker, string placeholder = "text")
    {
        (start, length) = Clamp(text, start, length);
        (start, length) = TrimSelection(text, start, length);

        if (length == 0)
        {
            var newText = text.Insert(start, marker + placeholder + marker);
            return new EditResult(newText, start + marker.Length, placeholder.Length);
        }

        var selection = text.Substring(start, length);
        var m = marker.Length;

        // Selection itself carries the marker: **bold** -> bold.
        if (selection.Length >= 2 * m
            && selection.StartsWith(marker, StringComparison.Ordinal)
            && selection.EndsWith(marker, StringComparison.Ordinal))
        {
            var inner = selection.Substring(m, selection.Length - 2 * m);

            // "**x**" with marker "*" must wrap (-> ***x***), not unwrap: the
            // bookend belongs to a longer marker run, which we detect by the
            // inner text still being bookended by the same marker.
            var bookendBelongsToLongerRun = inner.Length >= 2 * m
                && inner.StartsWith(marker, StringComparison.Ordinal)
                && inner.EndsWith(marker, StringComparison.Ordinal);

            if (inner.Length > 0 && !bookendBelongsToLongerRun)
            {
                var newText = string.Concat(text.AsSpan(0, start), inner, text.AsSpan(start + length));
                return new EditResult(newText, start, inner.Length);
            }
        }

        // Marker sits just outside the selection: **|bold|** -> bold.
        if (start >= m
            && start + length + m <= text.Length
            && text.AsSpan(start - m, m).SequenceEqual(marker)
            && text.AsSpan(start + length, m).SequenceEqual(marker)
            && !SurroundingMarkersAreLiteral(text, start, length, marker))
        {
            var newText = text.Remove(start + length, m).Remove(start - m, m);
            return new EditResult(newText, start - m, length);
        }

        var wrapped = text.Insert(start + length, marker).Insert(start, marker);
        return new EditResult(wrapped, start + m, length);
    }

    /// <summary>Underscores glued to word characters (my_var_name) are literals, not italic markers.</summary>
    private static bool SurroundingMarkersAreLiteral(string text, int start, int length, string marker)
    {
        if (marker[0] != '_')
            return false;

        var m = marker.Length;
        var beforeIndex = start - m - 1;
        var afterIndex = start + length + m;
        return (beforeIndex >= 0 && char.IsLetterOrDigit(text[beforeIndex]))
            || (afterIndex < text.Length && char.IsLetterOrDigit(text[afterIndex]));
    }

    // ---- Line prefixes (lists, quote) ----

    public static EditResult ToggleLinePrefix(string text, int start, int length, LinePrefixKind kind)
    {
        (start, length) = Clamp(text, start, length);
        var (blockStart, blockEnd) = LineBlock(text, start, length);
        var lines = SplitLines(text[blockStart..blockEnd]);

        var contentLines = lines.Where(l => l.Content.Trim().Length > 0).ToList();
        // A block with no content at all (empty document, blank line) still gets
        // the prefix, so the command visibly starts a list instead of no-opping.
        var blockIsBlank = contentLines.Count == 0;
        var removing = !blockIsBlank && contentLines.All(l => HasPrefix(l.Content, kind));

        var builder = new StringBuilder();
        var number = 1;
        foreach (var line in lines)
        {
            if (!blockIsBlank && line.Content.Trim().Length == 0)
            {
                builder.Append(line.Content).Append(line.Terminator);
                continue;
            }

            builder.Append(removing
                ? RemovePrefix(line.Content, kind)
                : AddPrefix(line.Content, kind, ref number));
            builder.Append(line.Terminator);
        }

        var newBlock = builder.ToString();
        var newText = string.Concat(text.AsSpan(0, blockStart), newBlock, text.AsSpan(blockEnd));
        return new EditResult(newText, blockStart, newBlock.Length);
    }

    private static bool HasPrefix(string content, LinePrefixKind kind)
    {
        if (kind == LinePrefixKind.Quote)
            return QuoteRegex().IsMatch(content);

        var match = ListPrefixRegex().Match(content);
        return kind switch
        {
            LinePrefixKind.Bullet => match.Groups["bullet"].Success,
            LinePrefixKind.Ordered => match.Groups["ordered"].Success,
            LinePrefixKind.Task => match.Groups["task"].Success,
            _ => false,
        };
    }

    private static string RemovePrefix(string content, LinePrefixKind kind)
    {
        if (kind == LinePrefixKind.Quote)
        {
            var quote = QuoteRegex().Match(content);
            return quote.Success ? quote.Groups["indent"].Value + quote.Groups["rest"].Value : content;
        }

        var match = ListPrefixRegex().Match(content);
        return match.Groups["indent"].Value + match.Groups["rest"].Value;
    }

    private static string AddPrefix(string content, LinePrefixKind kind, ref int number)
    {
        if (kind == LinePrefixKind.Quote)
            return HasPrefix(content, kind) ? content : "> " + content;

        // Converting between list kinds replaces the existing list marker.
        var match = ListPrefixRegex().Match(content);
        var indent = match.Groups["indent"].Value;
        var rest = match.Groups["rest"].Value;

        return kind switch
        {
            LinePrefixKind.Bullet => $"{indent}- {rest}",
            LinePrefixKind.Task => $"{indent}- [ ] {rest}",
            LinePrefixKind.Ordered => $"{indent}{number++}. {rest}",
            _ => content,
        };
    }

    // ---- Headings ----

    /// <summary>Sets the heading level for all selected lines; level 0 clears. Re-applying the same level toggles it off.</summary>
    public static EditResult SetHeading(string text, int start, int length, int level)
    {
        if (level is < 0 or > 6)
            throw new ArgumentOutOfRangeException(nameof(level));

        (start, length) = Clamp(text, start, length);
        var (blockStart, blockEnd) = LineBlock(text, start, length);
        var lines = SplitLines(text[blockStart..blockEnd]);

        var contentLines = lines.Where(l => l.Content.Trim().Length > 0).ToList();
        var blockIsBlank = contentLines.Count == 0;
        var allAtLevel = level > 0
            && contentLines.Count > 0
            && contentLines.All(l => HeadingRegex().Match(l.Content) is { Success: true } m && m.Groups["hashes"].Length == level);
        var effectiveLevel = allAtLevel ? 0 : level;

        var builder = new StringBuilder();
        foreach (var line in lines)
        {
            if (!blockIsBlank && line.Content.Trim().Length == 0)
            {
                builder.Append(line.Content).Append(line.Terminator);
                continue;
            }

            var match = HeadingRegex().Match(line.Content);
            var body = match.Success ? match.Groups["rest"].Value : line.Content;
            builder.Append(effectiveLevel > 0 ? new string('#', effectiveLevel) + " " + body : body);
            builder.Append(line.Terminator);
        }

        var newBlock = builder.ToString();
        var newText = string.Concat(text.AsSpan(0, blockStart), newBlock, text.AsSpan(blockEnd));
        return new EditResult(newText, blockStart, newBlock.Length);
    }

    // ---- Insertions ----

    public static EditResult InsertLink(string text, int start, int length, bool image = false)
    {
        (start, length) = Clamp(text, start, length);
        (start, length) = TrimSelection(text, start, length);

        var selection = text.Substring(start, length);
        var bang = image ? "!" : "";
        var textPlaceholder = image ? "alt text" : "text";
        const string urlPlaceholder = "url";

        string label, url;
        bool selectUrl;
        if (length == 0)
        {
            label = textPlaceholder;
            url = urlPlaceholder;
            selectUrl = false;
        }
        else if (selection.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                 || selection.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            label = textPlaceholder;
            url = selection;
            selectUrl = false;
        }
        else
        {
            label = selection;
            url = urlPlaceholder;
            selectUrl = true;
        }

        var inserted = $"{bang}[{label}]({url})";
        var newText = string.Concat(text.AsSpan(0, start), inserted, text.AsSpan(start + length));

        var labelStart = start + bang.Length + 1;
        var urlStart = labelStart + label.Length + 2;
        return selectUrl
            ? new EditResult(newText, urlStart, url.Length)
            : new EditResult(newText, labelStart, label.Length);
    }

    public static EditResult InsertTable(string text, int start, int length, int rows = 3, int columns = 3)
    {
        if (rows < 1 || columns < 1)
            throw new ArgumentOutOfRangeException(rows < 1 ? nameof(rows) : nameof(columns));

        (start, length) = Clamp(text, start, length);

        var builder = new StringBuilder();
        builder.Append('|');
        for (var c = 1; c <= columns; c++)
            builder.Append($" Header {c} |");
        builder.Append('\n').Append('|');
        for (var c = 1; c <= columns; c++)
            builder.Append(" --- |");
        for (var r = 0; r < rows; r++)
        {
            builder.Append('\n').Append('|');
            for (var c = 0; c < columns; c++)
                builder.Append("   |");
        }

        return InsertBlock(text, start, length, builder.ToString(), selectionOffset: 2, selectionLength: "Header 1".Length);
    }

    public static EditResult InsertHorizontalRule(string text, int start, int length)
    {
        (start, length) = Clamp(text, start, length);
        return InsertBlock(text, start, length, "---", selectionOffset: 3, selectionLength: 0);
    }

    public static EditResult InsertCodeBlock(string text, int start, int length)
    {
        (start, length) = Clamp(text, start, length);
        var selection = text.Substring(start, length);

        var body = length == 0 ? "" : selection.TrimEnd('\n', '\r');
        var block = $"```\n{body}\n```";

        // Caret lands right after the opening fence so a language can be typed.
        return InsertBlock(text, start, length, block, selectionOffset: 3, selectionLength: 0);
    }

    /// <summary>Replaces the selection with a block element, padding with blank lines so it stands alone.</summary>
    private static EditResult InsertBlock(string text, int start, int length, string block, int selectionOffset, int selectionLength)
    {
        var before = text.AsSpan(0, start);
        var after = text.AsSpan(start + length);

        var prefix = before.Length == 0 || EndsWithBlankLine(before)
            ? ""
            : before.EndsWith("\n") ? "\n" : "\n\n";
        var suffix = after.Length == 0
            ? "\n"
            : StartsWithBlankLine(after) ? "" : after.StartsWith("\n") ? "\n" : "\n\n";

        var newText = string.Concat(before, prefix + block + suffix, after);
        return new EditResult(newText, start + prefix.Length + selectionOffset, selectionLength);
    }

    private static bool EndsWithBlankLine(ReadOnlySpan<char> span)
    {
        var trimmed = span.TrimEnd('\r');
        if (!trimmed.EndsWith("\n"))
            return false;
        trimmed = trimmed[..^1].TrimEnd('\r');
        return trimmed.Length == 0 || trimmed.EndsWith("\n");
    }

    private static bool StartsWithBlankLine(ReadOnlySpan<char> span)
    {
        var trimmed = span.TrimStart('\r');
        if (!trimmed.StartsWith("\n"))
            return false;
        trimmed = trimmed[1..].TrimStart('\r');
        return trimmed.Length == 0 || trimmed.StartsWith("\n");
    }

    // ---- Shared helpers ----

    private static (int Start, int Length) Clamp(string text, int start, int length)
    {
        start = Math.Clamp(start, 0, text.Length);
        length = Math.Clamp(length, 0, text.Length - start);
        return (start, length);
    }

    private static (int Start, int Length) TrimSelection(string text, int start, int length)
    {
        while (length > 0 && char.IsWhiteSpace(text[start]))
        {
            start++;
            length--;
        }
        while (length > 0 && char.IsWhiteSpace(text[start + length - 1]))
            length--;
        return (start, length);
    }

    /// <summary>Expands a selection to whole lines. Returns [start, end) excluding the final line terminator.</summary>
    private static (int BlockStart, int BlockEnd) LineBlock(string text, int start, int length)
    {
        var blockStart = TextLines.LineStart(text, start);

        // A selection ending just after a newline should not pull in the next line.
        var selEnd = start + length;
        if (length > 0 && text[selEnd - 1] == '\n')
            selEnd--;

        var blockEnd = TextLines.LineEnd(text, selEnd, blockStart);
        return (blockStart, blockEnd);
    }

    private readonly record struct Line(string Content, string Terminator);

    private static List<Line> SplitLines(string block)
    {
        var lines = new List<Line>();
        var index = 0;
        while (index <= block.Length)
        {
            var newline = block.IndexOf('\n', index);
            if (newline < 0)
            {
                lines.Add(new Line(block[index..], ""));
                break;
            }

            var contentEnd = newline > index && block[newline - 1] == '\r' ? newline - 1 : newline;
            lines.Add(new Line(block[index..contentEnd], block[contentEnd..(newline + 1)]));
            index = newline + 1;
        }
        return lines;
    }
}
