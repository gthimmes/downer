using Downer.Core;

namespace Downer.Tests;

public class RecentFilesTests
{
    [Fact]
    public void Adds_new_path_to_front()
    {
        var result = RecentFiles.Add(new[] { "b.md", "c.md" }, "a.md");

        Assert.Equal(new[] { "a.md", "b.md", "c.md" }, result);
    }

    [Fact]
    public void Existing_path_moves_to_front_without_duplicate()
    {
        var result = RecentFiles.Add(new[] { "a.md", "b.md", "c.md" }, "b.md");

        Assert.Equal(new[] { "b.md", "a.md", "c.md" }, result);
    }

    [Fact]
    public void Dedupe_is_case_insensitive()
    {
        var result = RecentFiles.Add(new[] { "C:\\Docs\\Note.md" }, "c:\\docs\\note.md");

        Assert.Single(result);
    }

    [Fact]
    public void List_is_capped_at_max()
    {
        var existing = Enumerable.Range(1, 10).Select(i => $"{i}.md");

        var result = RecentFiles.Add(existing, "new.md", max: 10);

        Assert.Equal(10, result.Count);
        Assert.Equal("new.md", result[0]);
        Assert.DoesNotContain("10.md", result);
    }

    [Fact]
    public void Remove_deletes_matching_entry()
    {
        var result = RecentFiles.Remove(new[] { "a.md", "b.md" }, "A.MD");

        Assert.Equal(new[] { "b.md" }, result);
    }

    [Fact]
    public void Add_to_empty_list()
    {
        Assert.Equal(new[] { "only.md" }, RecentFiles.Add(Array.Empty<string>(), "only.md"));
    }
}
