namespace Downer.Core;

/// <summary>A candidate word for spell checking, positioned within its line.</summary>
public readonly record struct WordSpan(int Start, int Length, string Word);

/// <summary>
/// Pure extraction of spell-checkable words from a markdown line. Skips
/// everything that is not prose: syntax markers, inline code, link URLs,
/// bare URLs and emails, fenced code, numbers, and identifier-like tokens
/// (ALL-CAPS, camelCase). Only ASCII words are offered so non-English prose
/// is never flagged by the English dictionary.
/// </summary>
public static class SpellWords
{
    public static IReadOnlyList<WordSpan> Extract(string line, FenceLineState fenceState = FenceLineState.Outside)
    {
        var words = new List<WordSpan>();
        if (line.Length == 0 || fenceState != FenceLineState.Outside)
            return words;

        var excluded = new bool[line.Length];

        foreach (var span in MarkdownSpanParser.ParseLine(line, fenceState).Spans)
        {
            if (!span.IsMarker && span.Kind is not (SpanKind.Code or SpanKind.LinkUrl))
                continue;
            for (var i = span.Start; i < span.End && i < line.Length; i++)
                excluded[i] = true;
        }

        ExcludeUrlLikeRuns(line, excluded);

        for (var i = 0; i < line.Length; i++)
        {
            if (excluded[i] || !char.IsLetter(line[i]))
                continue;

            var start = i;
            while (i < line.Length && !excluded[i] && (char.IsLetter(line[i]) || line[i] == '\''))
                i++;

            var end = i;
            while (end > start && line[end - 1] == '\'')
                end--; // trailing apostrophes (quotes) are not part of the word

            var word = line[start..end];
            if (IsCheckable(word))
                words.Add(new WordSpan(start, end - start, word));
        }

        return words;
    }

    /// <summary>Whole whitespace-delimited tokens that look like URLs or emails are skipped.</summary>
    private static void ExcludeUrlLikeRuns(string line, bool[] excluded)
    {
        // Already-excluded characters (markers, code, link URLs) break tokens too,
        // so prose right next to markdown link syntax is not dragged down with it.
        var i = 0;
        while (i < line.Length)
        {
            if (char.IsWhiteSpace(line[i]) || excluded[i])
            {
                i++;
                continue;
            }

            var start = i;
            while (i < line.Length && !char.IsWhiteSpace(line[i]) && !excluded[i])
                i++;

            var token = line[start..i];
            if (token.Contains("://") || token.Contains('@') || token.StartsWith("www.", StringComparison.OrdinalIgnoreCase))
            {
                for (var j = start; j < i; j++)
                    excluded[j] = true;
            }
        }
    }

    private static bool IsCheckable(string word)
    {
        if (word.Length < 2)
            return false;

        var upperAfterFirst = false;
        foreach (var (c, index) in word.Select((c, index) => (c, index)))
        {
            if (!char.IsAscii(c) && c != '\'')
                return false; // non-English prose is not ours to judge
            if (index > 0 && char.IsUpper(c))
                upperAfterFirst = true;
        }

        // ALL-CAPS and camelCase are identifiers/acronyms, not prose.
        return !upperAfterFirst;
    }
}
