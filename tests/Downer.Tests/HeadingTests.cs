using Downer.Core;

namespace Downer.Tests;

public class HeadingTests
{
    [Fact]
    public void Sets_h1_on_plain_line()
    {
        var r = MarkdownFormatter.SetHeading("title", 0, 0, 1);

        Assert.Equal("# title", r.Text);
    }

    [Fact]
    public void Changes_existing_heading_level()
    {
        var r = MarkdownFormatter.SetHeading("# title", 0, 0, 3);

        Assert.Equal("### title", r.Text);
    }

    [Fact]
    public void Reapplying_same_level_toggles_heading_off()
    {
        var r = MarkdownFormatter.SetHeading("## title", 0, 0, 2);

        Assert.Equal("title", r.Text);
    }

    [Fact]
    public void Level_zero_clears_heading()
    {
        var r = MarkdownFormatter.SetHeading("### deep", 0, 0, 0);

        Assert.Equal("deep", r.Text);
    }

    [Fact]
    public void Applies_to_every_selected_line()
    {
        var r = MarkdownFormatter.SetHeading("one\ntwo", 0, 7, 2);

        Assert.Equal("## one\n## two", r.Text);
    }

    [Fact]
    public void Skips_blank_lines()
    {
        var r = MarkdownFormatter.SetHeading("one\n\ntwo", 0, 8, 1);

        Assert.Equal("# one\n\n# two", r.Text);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(7)]
    public void Rejects_invalid_levels(int level)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => MarkdownFormatter.SetHeading("x", 0, 0, level));
    }

    [Fact]
    public void Empty_document_gets_heading_marker()
    {
        var r = MarkdownFormatter.SetHeading("", 0, 0, 1);

        Assert.Equal("# ", r.Text);
    }

    [Fact]
    public void Only_touches_lines_in_selection()
    {
        var r = MarkdownFormatter.SetHeading("intro\ntitle\noutro", 6, 5, 1);

        Assert.Equal("intro\n# title\noutro", r.Text);
    }
}
