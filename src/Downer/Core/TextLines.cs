namespace Downer.Core;

/// <summary>Single source of CRLF-aware line-boundary math shared by all Core transforms.</summary>
internal static class TextLines
{
    /// <summary>Offset of the first character of the line containing <paramref name="offset"/>.</summary>
    public static int LineStart(string text, int offset)
    {
        if (offset <= 0)
            return 0;
        var newline = text.LastIndexOf('\n', offset - 1);
        return newline < 0 ? 0 : newline + 1;
    }

    /// <summary>Offset just past the last content character (excluding a trailing '\r') of the line containing <paramref name="offset"/>.</summary>
    public static int LineEnd(string text, int offset, int lineStart)
    {
        var end = offset < text.Length ? text.IndexOf('\n', offset) : -1;
        end = end < 0 ? text.Length : end;
        if (end > lineStart && text[end - 1] == '\r')
            end--;
        return end;
    }

    /// <summary>"\r\n" when the document uses CRLF anywhere, otherwise "\n".</summary>
    public static string DetectNewline(string text) =>
        text.Contains("\r\n", StringComparison.Ordinal) ? "\r\n" : "\n";
}
