using Downer.Core;

namespace Downer.Tests;

public class InlineToggleTests
{
    [Fact]
    public void Wraps_selected_word_in_bold()
    {
        var r = MarkdownFormatter.ToggleInline("hello world", 0, 5, "**");

        Assert.Equal("**hello** world", r.Text);
        Assert.Equal(2, r.SelectionStart);
        Assert.Equal(5, r.SelectionLength);
    }

    [Fact]
    public void Unwraps_when_selection_includes_markers()
    {
        var r = MarkdownFormatter.ToggleInline("**hello** world", 0, 9, "**");

        Assert.Equal("hello world", r.Text);
        Assert.Equal(0, r.SelectionStart);
        Assert.Equal(5, r.SelectionLength);
    }

    [Fact]
    public void Unwraps_when_markers_surround_selection()
    {
        var r = MarkdownFormatter.ToggleInline("**hello** world", 2, 5, "**");

        Assert.Equal("hello world", r.Text);
        Assert.Equal(0, r.SelectionStart);
        Assert.Equal(5, r.SelectionLength);
    }

    [Fact]
    public void Empty_selection_inserts_placeholder_and_selects_it()
    {
        var r = MarkdownFormatter.ToggleInline("", 0, 0, "**", "bold");

        Assert.Equal("**bold**", r.Text);
        Assert.Equal(2, r.SelectionStart);
        Assert.Equal(4, r.SelectionLength);
    }

    [Fact]
    public void Trims_whitespace_from_selection_edges()
    {
        var r = MarkdownFormatter.ToggleInline("hello world", 5, 6, "**"); // " world"

        Assert.Equal("hello **world**", r.Text);
        Assert.Equal(8, r.SelectionStart);
        Assert.Equal(5, r.SelectionLength);
    }

    [Fact]
    public void Italic_marker_wraps_bold_text_without_stealing_asterisks()
    {
        // "**x**" toggled with "*" must nest (-> ***x***), not unwrap.
        var r = MarkdownFormatter.ToggleInline("**x**", 0, 5, "*");

        Assert.Equal("***x***", r.Text);
        Assert.Equal(1, r.SelectionStart);
        Assert.Equal(5, r.SelectionLength);
    }

    [Fact]
    public void Single_star_italic_unwraps_normally()
    {
        var r = MarkdownFormatter.ToggleInline("*x*", 0, 3, "*");

        Assert.Equal("x", r.Text);
    }

    [Theory]
    [InlineData("~~")]
    [InlineData("`")]
    [InlineData("_")]
    public void Toggle_is_its_own_inverse(string marker)
    {
        var wrapped = MarkdownFormatter.ToggleInline("some text here", 5, 4, marker);
        var unwrapped = MarkdownFormatter.ToggleInline(wrapped.Text, wrapped.SelectionStart, wrapped.SelectionLength, marker);

        Assert.Equal("some text here", unwrapped.Text);
        Assert.Equal(5, unwrapped.SelectionStart);
        Assert.Equal(4, unwrapped.SelectionLength);
    }

    [Fact]
    public void Snake_case_underscores_are_not_treated_as_italic_markers()
    {
        // Selecting "var" inside my_var_name must wrap, not delete the literal underscores.
        var r = MarkdownFormatter.ToggleInline("my_var_name", 3, 3, "_");

        Assert.Equal("my__var__name", r.Text);
        Assert.Equal(4, r.SelectionStart);
        Assert.Equal(3, r.SelectionLength);
    }

    [Fact]
    public void Standalone_underscore_italics_still_unwrap()
    {
        var r = MarkdownFormatter.ToggleInline("_hello_ world", 1, 5, "_");

        Assert.Equal("hello world", r.Text);
    }

    [Fact]
    public void Clamps_out_of_range_selection()
    {
        var r = MarkdownFormatter.ToggleInline("hi", 0, 99, "**");

        Assert.Equal("**hi**", r.Text);
    }
}
