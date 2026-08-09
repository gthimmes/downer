namespace Downer.Core;

/// <summary>Pure MRU-list operations for the recent files menu.</summary>
public static class RecentFiles
{
    public const int DefaultMax = 10;

    /// <summary>Returns a new list with <paramref name="path"/> at the front, deduplicated, capped at <paramref name="max"/>.</summary>
    public static List<string> Add(IEnumerable<string> existing, string path, int max = DefaultMax)
    {
        var result = new List<string> { path };
        foreach (var entry in existing)
        {
            if (result.Count >= max)
                break;
            if (!string.Equals(entry, path, StringComparison.OrdinalIgnoreCase))
                result.Add(entry);
        }
        return result;
    }

    public static List<string> Remove(IEnumerable<string> existing, string path) =>
        existing.Where(e => !string.Equals(e, path, StringComparison.OrdinalIgnoreCase)).ToList();
}
