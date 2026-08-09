using System.Text.RegularExpressions;

namespace Downer.Core;

/// <summary>A single splice to apply to the document in response to Enter.</summary>
public sealed record ContinuationEdit(int ReplaceStart, int ReplaceLength, string InsertText, int CaretOffset);

/// <summary>
/// Pressing Enter inside a list/quote continues it with the next marker;
/// pressing Enter on an empty item removes the marker (exits the list).
/// </summary>
public static partial class AutoListContinuation
{
    // Quantifiers kept in lockstep with MarkdownFormatter.ListPrefixRegex/QuoteRegex
    // so Enter-continuation and toggle-formatting recognize the same lines.
    [GeneratedRegex(@"^(?<indent>\s*)(?:(?<task>(?<taskchar>[-*+])[ \t]+\[[ xX]\][ \t]?)|(?<bullet>(?<bulletchar>[-*+])[ \t]+)|(?<ordered>(?<num>\d+)(?<numsep>[.)])[ \t]+)|(?<quote>>[ \t]?))")]
    private static partial Regex MarkerRegex();

    /// <summary>Returns the edit to apply for Enter at <paramref name="caretOffset"/>, or null to let the editor handle it.</summary>
    public static ContinuationEdit? OnEnter(string text, int caretOffset)
    {
        caretOffset = Math.Clamp(caretOffset, 0, text.Length);

        var lineStart = TextLines.LineStart(text, caretOffset);
        var lineEnd = TextLines.LineEnd(text, caretOffset, lineStart);

        var beforeCaret = text[lineStart..caretOffset];
        var match = MarkerRegex().Match(beforeCaret);
        if (!match.Success)
            return null;

        // Empty item + caret at end of line -> remove the marker and exit the list.
        var wholeLine = text[lineStart..lineEnd];
        if (caretOffset >= lineEnd && wholeLine[match.Length..].Trim().Length == 0)
            return new ContinuationEdit(lineStart, lineEnd - lineStart, "", lineStart);

        var indent = match.Groups["indent"].Value;
        string next;
        if (match.Groups["task"].Success)
            next = $"{indent}{match.Groups["taskchar"].Value} [ ] ";
        else if (match.Groups["bullet"].Success)
            next = $"{indent}{match.Groups["bulletchar"].Value} ";
        else if (match.Groups["ordered"].Success)
        {
            // Absurdly large numbers fall back to the editor's default Enter.
            if (!long.TryParse(match.Groups["num"].Value, out var number) || number == long.MaxValue)
                return null;
            next = $"{indent}{number + 1}{match.Groups["numsep"].Value} ";
        }
        else
            next = $"{indent}> ";

        var insert = TextLines.DetectNewline(text) + next;
        return new ContinuationEdit(caretOffset, 0, insert, caretOffset + insert.Length);
    }
}
