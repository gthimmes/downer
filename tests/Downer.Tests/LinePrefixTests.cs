using Downer.Core;

namespace Downer.Tests;

public class LinePrefixTests
{
    [Fact]
    public void Adds_bullet_to_current_line_with_caret_only()
    {
        var r = MarkdownFormatter.ToggleLinePrefix("alpha", 3, 0, LinePrefixKind.Bullet);

        Assert.Equal("- alpha", r.Text);
        Assert.Equal(0, r.SelectionStart);
        Assert.Equal(7, r.SelectionLength);
    }

    [Fact]
    public void Adds_bullets_to_all_selected_lines()
    {
        var r = MarkdownFormatter.ToggleLinePrefix("a\nb\nc", 0, 5, LinePrefixKind.Bullet);

        Assert.Equal("- a\n- b\n- c", r.Text);
    }

    [Fact]
    public void Removes_bullets_when_all_lines_have_them()
    {
        var r = MarkdownFormatter.ToggleLinePrefix("- a\n- b", 0, 7, LinePrefixKind.Bullet);

        Assert.Equal("a\nb", r.Text);
    }

    [Fact]
    public void Numbers_ordered_list_sequentially()
    {
        var r = MarkdownFormatter.ToggleLinePrefix("a\nb\nc", 0, 5, LinePrefixKind.Ordered);

        Assert.Equal("1. a\n2. b\n3. c", r.Text);
    }

    [Fact]
    public void Converts_bullet_list_to_ordered_list()
    {
        var r = MarkdownFormatter.ToggleLinePrefix("- a\n- b", 0, 7, LinePrefixKind.Ordered);

        Assert.Equal("1. a\n2. b", r.Text);
    }

    [Fact]
    public void Converts_ordered_list_to_task_list()
    {
        var r = MarkdownFormatter.ToggleLinePrefix("1. a\n2. b", 0, 9, LinePrefixKind.Task);

        Assert.Equal("- [ ] a\n- [ ] b", r.Text);
    }

    [Fact]
    public void Removes_task_prefix_including_checked_boxes()
    {
        var r = MarkdownFormatter.ToggleLinePrefix("- [x] done\n- [ ] todo", 0, 21, LinePrefixKind.Task);

        Assert.Equal("done\ntodo", r.Text);
    }

    [Fact]
    public void Quote_adds_and_removes()
    {
        var quoted = MarkdownFormatter.ToggleLinePrefix("a\nb", 0, 3, LinePrefixKind.Quote);
        Assert.Equal("> a\n> b", quoted.Text);

        var unquoted = MarkdownFormatter.ToggleLinePrefix(quoted.Text, quoted.SelectionStart, quoted.SelectionLength, LinePrefixKind.Quote);
        Assert.Equal("a\nb", unquoted.Text);
    }

    [Fact]
    public void Quote_does_not_strip_list_markers()
    {
        var r = MarkdownFormatter.ToggleLinePrefix("- item", 0, 6, LinePrefixKind.Quote);

        Assert.Equal("> - item", r.Text);
    }

    [Fact]
    public void Blank_lines_are_left_alone()
    {
        var r = MarkdownFormatter.ToggleLinePrefix("a\n\nb", 0, 4, LinePrefixKind.Bullet);

        Assert.Equal("- a\n\n- b", r.Text);
    }

    [Fact]
    public void Preserves_crlf_line_endings()
    {
        var r = MarkdownFormatter.ToggleLinePrefix("a\r\nb", 0, 4, LinePrefixKind.Bullet);

        Assert.Equal("- a\r\n- b", r.Text);
    }

    [Fact]
    public void Partial_selection_expands_to_whole_lines()
    {
        var r = MarkdownFormatter.ToggleLinePrefix("hello\nworld", 2, 6, LinePrefixKind.Bullet);

        Assert.Equal("- hello\n- world", r.Text);
    }

    [Fact]
    public void Preserves_indentation()
    {
        var r = MarkdownFormatter.ToggleLinePrefix("  nested", 0, 8, LinePrefixKind.Bullet);

        Assert.Equal("  - nested", r.Text);
    }

    [Fact]
    public void Does_not_touch_surrounding_lines()
    {
        var r = MarkdownFormatter.ToggleLinePrefix("before\ntarget\nafter", 7, 6, LinePrefixKind.Bullet);

        Assert.Equal("before\n- target\nafter", r.Text);
    }
}
