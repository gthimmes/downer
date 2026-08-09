using Downer.Core;

namespace Downer.Tests;

public class AutoListContinuationTests
{
    [Fact]
    public void Continues_bullet_list()
    {
        var edit = AutoListContinuation.OnEnter("- item", 6);

        Assert.NotNull(edit);
        Assert.Equal(6, edit.ReplaceStart);
        Assert.Equal(0, edit.ReplaceLength);
        Assert.Equal("\n- ", edit.InsertText);
        Assert.Equal(9, edit.CaretOffset);
    }

    [Theory]
    [InlineData("* item", "\n* ")]
    [InlineData("+ item", "\n+ ")]
    public void Preserves_bullet_character(string line, string expectedInsert)
    {
        var edit = AutoListContinuation.OnEnter(line, line.Length);

        Assert.Equal(expectedInsert, edit!.InsertText);
    }

    [Fact]
    public void Increments_ordered_list_number()
    {
        var edit = AutoListContinuation.OnEnter("3. gamma", 8);

        Assert.Equal("\n4. ", edit!.InsertText);
    }

    [Fact]
    public void Preserves_paren_number_separator()
    {
        var edit = AutoListContinuation.OnEnter("1) alpha", 8);

        Assert.Equal("\n2) ", edit!.InsertText);
    }

    [Fact]
    public void Continues_task_list_with_unchecked_box()
    {
        var edit = AutoListContinuation.OnEnter("- [x] done", 10);

        Assert.Equal("\n- [ ] ", edit!.InsertText);
    }

    [Fact]
    public void Continues_blockquote()
    {
        var edit = AutoListContinuation.OnEnter("> quoted", 8);

        Assert.Equal("\n> ", edit!.InsertText);
    }

    [Fact]
    public void Preserves_indentation()
    {
        var edit = AutoListContinuation.OnEnter("  - nested", 10);

        Assert.Equal("\n  - ", edit!.InsertText);
    }

    [Fact]
    public void Empty_bullet_item_exits_the_list()
    {
        // "- a\n- " with caret at the end: Enter should remove the dangling marker.
        var edit = AutoListContinuation.OnEnter("- a\n- ", 6);

        Assert.NotNull(edit);
        Assert.Equal(4, edit.ReplaceStart);
        Assert.Equal(2, edit.ReplaceLength);
        Assert.Equal("", edit.InsertText);
        Assert.Equal(4, edit.CaretOffset);
    }

    [Fact]
    public void Empty_ordered_item_exits_the_list()
    {
        var edit = AutoListContinuation.OnEnter("1. ", 3);

        Assert.Equal(0, edit!.ReplaceStart);
        Assert.Equal(3, edit.ReplaceLength);
        Assert.Equal("", edit.InsertText);
    }

    [Fact]
    public void Empty_item_with_crlf_terminator_exits_cleanly()
    {
        var text = "- a\r\n- \r\nx";
        var edit = AutoListContinuation.OnEnter(text, 7);

        Assert.Equal(5, edit!.ReplaceStart);
        Assert.Equal(2, edit.ReplaceLength);
    }

    [Fact]
    public void Splitting_an_item_mid_line_continues_the_list()
    {
        var edit = AutoListContinuation.OnEnter("- hello", 3);

        Assert.Equal(3, edit!.ReplaceStart);
        Assert.Equal("\n- ", edit.InsertText);
    }

    [Fact]
    public void Plain_text_returns_null()
    {
        Assert.Null(AutoListContinuation.OnEnter("plain text", 10));
    }

    [Fact]
    public void Empty_document_returns_null()
    {
        Assert.Null(AutoListContinuation.OnEnter("", 0));
    }

    [Fact]
    public void Caret_before_marker_completes_returns_null()
    {
        Assert.Null(AutoListContinuation.OnEnter("- item", 1));
    }
}
