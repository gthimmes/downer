using System.Text.RegularExpressions;

namespace Downer.Core;

public enum SpanKind
{
    HeadingMarker,
    QuoteMarker,
    ListMarker,
    TaskCheckbox,
    Bold,
    Italic,
    Code,
    Strikethrough,
    LinkText,
    LinkUrl,
    LinkPunctuation,
}

/// <summary>A styled region of a single line. Marker spans carry syntax; content spans carry text to style.</summary>
public sealed record StyledSpan(int Start, int Length, SpanKind Kind, bool IsMarker)
{
    public int End => Start + Length;
}

public sealed record LineSpans(int HeadingLevel, FenceLineState FenceState, IReadOnlyList<StyledSpan> Spans);

public enum FenceLineState
{
    Outside,
    Delimiter,
    Inside,
}

/// <summary>Tracks which lines sit inside fenced code blocks.</summary>
public static partial class CodeFences
{
    [GeneratedRegex(@"^\s{0,3}(`{3,}|~{3,})")]
    private static partial Regex FenceRegex();

    public static FenceLineState[] Analyze(string text)
    {
        var lines = text.Split('\n');
        var result = new FenceLineState[lines.Length];
        var openChar = '\0';

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].TrimEnd('\r');
            var match = FenceRegex().Match(line);

            if (openChar == '\0')
            {
                if (match.Success)
                {
                    openChar = match.Groups[1].Value[0];
                    result[i] = FenceLineState.Delimiter;
                }
            }
            else if (match.Success && match.Groups[1].Value[0] == openChar)
            {
                openChar = '\0';
                result[i] = FenceLineState.Delimiter;
            }
            else
            {
                result[i] = FenceLineState.Inside;
            }
        }

        return result;
    }
}

/// <summary>
/// Pure per-line markdown span parser powering the in-place (WYSIWYG-style) editor rendering.
/// Inline code and link URLs claim their full range so no other styling applies inside them;
/// emphasis claims only its marker characters so styles nest (bold containing italic, etc.).
/// </summary>
public static partial class MarkdownSpanParser
{
    [GeneratedRegex(@"^(\s*)((?:>[ \t]?)+)")]
    private static partial Regex QuotePrefixRegex();

    [GeneratedRegex(@"^(#{1,6})[ \t]+")]
    private static partial Regex HeadingPrefixRegex();

    [GeneratedRegex(@"^(\s*)(?:(?<marker>[-*+][ \t]+)(?<task>\[[ xX]\][ \t]?)?|(?<marker2>\d{1,9}[.)][ \t]+))")]
    private static partial Regex ListPrefixRegex();

    [GeneratedRegex(@"(?<!`)(`+)(?!`)(.+?)(?<!`)\1(?!`)")]
    private static partial Regex CodeRegex();

    [GeneratedRegex(@"(!?)\[([^\]]*)\]\(([^)\s]*)\)")]
    private static partial Regex LinkRegex();

    [GeneratedRegex(@"\*\*\*(?!\s)(.+?)(?<!\s)\*\*\*")]
    private static partial Regex BoldItalicRegex();

    [GeneratedRegex(@"\*\*(?!\s)(.+?)(?<!\s)\*\*")]
    private static partial Regex BoldStarRegex();

    [GeneratedRegex(@"(?<![\w])__(?!\s)(.+?)(?<!\s)__(?![\w])")]
    private static partial Regex BoldUnderscoreRegex();

    [GeneratedRegex(@"~~(?!\s)(.+?)(?<!\s)~~")]
    private static partial Regex StrikeRegex();

    [GeneratedRegex(@"\*(?![\s*])([^*]+?)(?<!\s)\*(?!\*)")]
    private static partial Regex ItalicStarRegex();

    [GeneratedRegex(@"(?<![\w])_(?![\s_])([^_]+?)(?<!\s)_(?![\w])")]
    private static partial Regex ItalicUnderscoreRegex();

    public static LineSpans ParseLine(string line, FenceLineState fenceState = FenceLineState.Outside)
    {
        var spans = new List<StyledSpan>();
        if (fenceState != FenceLineState.Outside)
            return new LineSpans(0, fenceState, spans);

        var claimed = new bool[line.Length];
        var headingLevel = 0;
        var contentStart = 0;

        var quote = QuotePrefixRegex().Match(line);
        if (quote.Success)
        {
            var markers = quote.Groups[2];
            AddClaimed(spans, claimed, markers.Index, markers.Length, SpanKind.QuoteMarker, isMarker: true);
            contentStart = quote.Length;
        }

        var heading = HeadingPrefixRegex().Match(line[contentStart..]);
        if (heading.Success)
        {
            headingLevel = heading.Groups[1].Length;
            AddClaimed(spans, claimed, contentStart, heading.Length, SpanKind.HeadingMarker, isMarker: true);
        }
        else
        {
            var list = ListPrefixRegex().Match(line[contentStart..]);
            if (list.Success)
            {
                var marker = list.Groups["marker"].Success ? list.Groups["marker"] : list.Groups["marker2"];
                if (marker.Success)
                    AddClaimed(spans, claimed, contentStart + marker.Index, marker.Length, SpanKind.ListMarker, isMarker: true);
                if (list.Groups["task"].Success)
                {
                    var task = list.Groups["task"];
                    AddClaimed(spans, claimed, contentStart + task.Index, task.Length, SpanKind.TaskCheckbox, isMarker: true);
                }
            }
        }

        ParseInline(line, claimed, spans);

        spans.Sort((a, b) => a.Start.CompareTo(b.Start));
        return new LineSpans(headingLevel, fenceState, spans);
    }

    private static void ParseInline(string line, bool[] claimed, List<StyledSpan> spans)
    {
        // Inline code first: it suppresses everything inside.
        foreach (Match m in CodeRegex().Matches(line))
        {
            if (AnyClaimed(claimed, m.Index, m.Length))
                continue;
            var ticks = m.Groups[1].Length;
            AddClaimed(spans, claimed, m.Index, ticks, SpanKind.Code, isMarker: true);
            AddClaimed(spans, claimed, m.Index + ticks, m.Length - 2 * ticks, SpanKind.Code, isMarker: false);
            AddClaimed(spans, claimed, m.Index + m.Length - ticks, ticks, SpanKind.Code, isMarker: true);
        }

        // Links and images: URL is claimed so emphasis never fires inside it.
        foreach (Match m in LinkRegex().Matches(line))
        {
            if (AnyClaimed(claimed, m.Index, m.Length))
                continue;
            var text = m.Groups[2];
            var url = m.Groups[3];
            var openPunct = m.Index;
            var openLen = m.Groups[1].Length + 1;             // "![" or "["
            AddClaimed(spans, claimed, openPunct, openLen, SpanKind.LinkPunctuation, isMarker: true);
            spans.Add(new StyledSpan(text.Index, text.Length, SpanKind.LinkText, IsMarker: false));
            AddClaimed(spans, claimed, text.Index + text.Length, 2, SpanKind.LinkPunctuation, isMarker: true); // "]("
            AddClaimed(spans, claimed, url.Index, url.Length, SpanKind.LinkUrl, isMarker: true);
            AddClaimed(spans, claimed, url.Index + url.Length, 1, SpanKind.LinkPunctuation, isMarker: true);   // ")"
        }

        // Emphasis: claim marker characters only so styles can nest.
        EmphasisPass(line, claimed, spans, BoldItalicRegex(), 3, SpanKind.Bold, alsoItalic: true);
        EmphasisPass(line, claimed, spans, BoldStarRegex(), 2, SpanKind.Bold);
        EmphasisPass(line, claimed, spans, BoldUnderscoreRegex(), 2, SpanKind.Bold);
        EmphasisPass(line, claimed, spans, StrikeRegex(), 2, SpanKind.Strikethrough);
        EmphasisPass(line, claimed, spans, ItalicStarRegex(), 1, SpanKind.Italic);
        EmphasisPass(line, claimed, spans, ItalicUnderscoreRegex(), 1, SpanKind.Italic);
    }

    private static void EmphasisPass(string line, bool[] claimed, List<StyledSpan> spans, Regex regex, int markerLength, SpanKind kind, bool alsoItalic = false)
    {
        foreach (Match m in regex.Matches(line))
        {
            if (AnyClaimed(claimed, m.Index, markerLength)
                || AnyClaimed(claimed, m.Index + m.Length - markerLength, markerLength))
                continue;

            AddClaimed(spans, claimed, m.Index, markerLength, kind, isMarker: true);
            AddClaimed(spans, claimed, m.Index + m.Length - markerLength, markerLength, kind, isMarker: true);

            var contentStart = m.Index + markerLength;
            var contentLength = m.Length - 2 * markerLength;
            spans.Add(new StyledSpan(contentStart, contentLength, kind, IsMarker: false));
            if (alsoItalic)
                spans.Add(new StyledSpan(contentStart, contentLength, SpanKind.Italic, IsMarker: false));
        }
    }

    private static void AddClaimed(List<StyledSpan> spans, bool[] claimed, int start, int length, SpanKind kind, bool isMarker)
    {
        if (length <= 0)
            return;
        spans.Add(new StyledSpan(start, length, kind, isMarker));
        for (var i = start; i < start + length && i < claimed.Length; i++)
            claimed[i] = true;
    }

    private static bool AnyClaimed(bool[] claimed, int start, int length)
    {
        for (var i = start; i < start + length && i < claimed.Length; i++)
        {
            if (claimed[i])
                return true;
        }
        return false;
    }
}
