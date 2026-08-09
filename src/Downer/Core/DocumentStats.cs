using System.Text.RegularExpressions;

namespace Downer.Core;

public sealed partial record DocumentStats(int Words, int Characters, int Lines)
{
    [GeneratedRegex(@"[\p{L}\p{N}]+(?:[-'’_][\p{L}\p{N}]+)*")]
    private static partial Regex WordRegex();

    public static DocumentStats Count(string text)
    {
        if (string.IsNullOrEmpty(text))
            return new DocumentStats(0, 0, 1);

        var words = WordRegex().Matches(text).Count;
        var lines = 1;
        foreach (var c in text)
        {
            if (c == '\n')
                lines++;
        }

        return new DocumentStats(words, text.Length, lines);
    }
}
