using System.Text;
using System.Text.RegularExpressions;

namespace Downer.Core;

public enum ColumnAlignment
{
    None,
    Left,
    Right,
    Center,
}

/// <summary>Pads pipe-table cells so columns align in a monospace rendering.</summary>
public static partial class TableFormatter
{
    [GeneratedRegex(@"^:?-+:?$")]
    private static partial Regex SeparatorCellRegex();

    public static bool IsTableLine(string lineContent) =>
        lineContent.TrimStart().StartsWith('|');

    /// <summary>Formats the contiguous table block containing <paramref name="offset"/>, or returns null when there is none.</summary>
    public static EditResult? FormatTableAt(string text, int offset)
    {
        offset = Math.Clamp(offset, 0, text.Length);
        var anchorStart = TextLines.LineStart(text, offset);
        var anchorEnd = TextLines.LineEnd(text, anchorStart, anchorStart);
        if (!IsTableLine(text[anchorStart..anchorEnd]))
            return null;

        var blockStart = anchorStart;
        while (blockStart > 0)
        {
            var prevStart = TextLines.LineStart(text, blockStart - 1);
            var prevEnd = TextLines.LineEnd(text, prevStart, prevStart);
            if (!IsTableLine(text[prevStart..prevEnd]))
                break;
            blockStart = prevStart;
        }

        var blockEnd = anchorEnd;
        while (true)
        {
            var cursor = blockEnd;
            if (cursor < text.Length && text[cursor] == '\r')
                cursor++;
            if (cursor >= text.Length || text[cursor] != '\n')
                break;
            var nextStart = cursor + 1;
            var nextEnd = TextLines.LineEnd(text, nextStart, nextStart);
            if (nextStart >= text.Length || !IsTableLine(text[nextStart..nextEnd]))
                break;
            blockEnd = nextEnd;
        }

        var lines = text[blockStart..blockEnd].Split('\n').Select(l => l.TrimEnd('\r')).ToList();

        // Note: escaped pipes (\|) are not treated specially.
        var rows = lines.Select(line =>
        {
            var cells = SplitCells(line);
            var isSeparator = cells.Count > 0 && cells.All(c => c.Trim().Length > 0 && SeparatorCellRegex().IsMatch(c.Trim()));
            return (Cells: cells.Select(c => c.Trim()).ToList(), IsSeparator: isSeparator);
        }).ToList();

        var columnCount = rows.Max(r => r.Cells.Count);
        var alignments = new ColumnAlignment[columnCount];
        var separator = rows.FirstOrDefault(r => r.IsSeparator);
        if (separator.Cells is not null)
        {
            for (var c = 0; c < separator.Cells.Count && c < columnCount; c++)
                alignments[c] = ParseAlignment(separator.Cells[c]);
        }

        var widths = new int[columnCount];
        for (var c = 0; c < columnCount; c++)
        {
            var width = 3;
            foreach (var row in rows.Where(r => !r.IsSeparator))
            {
                if (c < row.Cells.Count)
                    width = Math.Max(width, row.Cells[c].Length);
            }
            widths[c] = width;
        }

        var newline = TextLines.DetectNewline(text);
        var builder = new StringBuilder();
        for (var i = 0; i < rows.Count; i++)
        {
            var (cells, isSeparator) = rows[i];
            builder.Append('|');
            for (var c = 0; c < columnCount; c++)
            {
                if (isSeparator)
                    builder.Append(SeparatorCell(widths[c], alignments[c]));
                else
                    builder.Append(' ').Append(Pad(c < cells.Count ? cells[c] : "", widths[c], alignments[c])).Append(' ');
                builder.Append('|');
            }
            if (i < rows.Count - 1)
                builder.Append(newline);
        }

        var newBlock = builder.ToString();
        var newText = string.Concat(text.AsSpan(0, blockStart), newBlock, text.AsSpan(blockEnd));
        return new EditResult(newText, blockStart, newBlock.Length);
    }

    /// <summary>Formats every table block in the document.</summary>
    public static string FormatAllTables(string text)
    {
        var offset = 0;
        while (offset < text.Length)
        {
            var lineEnd = TextLines.LineEnd(text, offset, offset);
            if (IsTableLine(text[offset..lineEnd]))
            {
                var result = FormatTableAt(text, offset);
                if (result is not null)
                {
                    text = result.Text;
                    lineEnd = result.SelectionStart + result.SelectionLength;
                }
            }

            var nextNewline = text.IndexOf('\n', Math.Min(lineEnd, text.Length));
            if (nextNewline < 0)
                break;
            offset = nextNewline + 1;
        }
        return text;
    }

    private static List<string> SplitCells(string line)
    {
        var trimmed = line.Trim();
        if (trimmed.StartsWith('|'))
            trimmed = trimmed[1..];
        if (trimmed.EndsWith('|'))
            trimmed = trimmed[..^1];
        return trimmed.Split('|').ToList();
    }

    private static ColumnAlignment ParseAlignment(string cell)
    {
        var startsColon = cell.StartsWith(':');
        var endsColon = cell.EndsWith(':');
        return (startsColon, endsColon) switch
        {
            (true, true) => ColumnAlignment.Center,
            (true, false) => ColumnAlignment.Left,
            (false, true) => ColumnAlignment.Right,
            _ => ColumnAlignment.None,
        };
    }

    private static string SeparatorCell(int width, ColumnAlignment alignment) => alignment switch
    {
        ColumnAlignment.Left => ":" + new string('-', width) + " ",
        ColumnAlignment.Right => " " + new string('-', width) + ":",
        ColumnAlignment.Center => ":" + new string('-', width) + ":",
        _ => " " + new string('-', width) + " ",
    };

    private static string Pad(string cell, int width, ColumnAlignment alignment)
    {
        var space = width - cell.Length;
        return alignment switch
        {
            ColumnAlignment.Right => cell.PadLeft(width),
            ColumnAlignment.Center => new string(' ', space / 2) + cell + new string(' ', space - space / 2),
            _ => cell.PadRight(width),
        };
    }
}
