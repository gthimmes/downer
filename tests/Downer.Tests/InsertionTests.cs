using Downer.Core;

namespace Downer.Tests;

public class InsertionTests
{
    // ---- Links and images ----

    [Fact]
    public void Link_with_no_selection_selects_text_placeholder()
    {
        var r = MarkdownFormatter.InsertLink("", 0, 0);

        Assert.Equal("[text](url)", r.Text);
        Assert.Equal(1, r.SelectionStart);
        Assert.Equal(4, r.SelectionLength);
    }

    [Fact]
    public void Link_with_text_selection_uses_it_as_label_and_selects_url()
    {
        var r = MarkdownFormatter.InsertLink("click here", 0, 10);

        Assert.Equal("[click here](url)", r.Text);
        Assert.Equal(13, r.SelectionStart);
        Assert.Equal(3, r.SelectionLength);
    }

    [Fact]
    public void Link_with_url_selection_uses_it_as_target_and_selects_label()
    {
        var r = MarkdownFormatter.InsertLink("https://example.com", 0, 19);

        Assert.Equal("[text](https://example.com)", r.Text);
        Assert.Equal(1, r.SelectionStart);
        Assert.Equal(4, r.SelectionLength);
    }

    [Fact]
    public void Image_uses_bang_and_alt_placeholder()
    {
        var r = MarkdownFormatter.InsertLink("", 0, 0, image: true);

        Assert.Equal("![alt text](url)", r.Text);
        Assert.Equal(2, r.SelectionStart);
        Assert.Equal(8, r.SelectionLength);
    }

    // ---- Horizontal rule ----

    [Fact]
    public void Rule_in_empty_document()
    {
        var r = MarkdownFormatter.InsertHorizontalRule("", 0, 0);

        Assert.Equal("---\n", r.Text);
        Assert.Equal(3, r.SelectionStart);
        Assert.Equal(0, r.SelectionLength);
    }

    [Fact]
    public void Rule_after_text_gets_blank_line_padding()
    {
        var r = MarkdownFormatter.InsertHorizontalRule("abc", 3, 0);

        Assert.Equal("abc\n\n---\n", r.Text);
    }

    [Fact]
    public void Rule_after_existing_blank_line_adds_no_extra_padding()
    {
        var r = MarkdownFormatter.InsertHorizontalRule("abc\n\n", 5, 0);

        Assert.Equal("abc\n\n---\n", r.Text);
    }

    [Fact]
    public void Rule_between_paragraphs()
    {
        var r = MarkdownFormatter.InsertHorizontalRule("one\n\n\n\ntwo", 5, 0);

        Assert.Equal("one\n\n---\n\ntwo", r.Text);
    }

    // ---- Code block ----

    [Fact]
    public void Code_block_with_no_selection_leaves_caret_at_language_slot()
    {
        var r = MarkdownFormatter.InsertCodeBlock("", 0, 0);

        Assert.Equal("```\n\n```\n", r.Text);
        Assert.Equal(3, r.SelectionStart);
        Assert.Equal(0, r.SelectionLength);
    }

    [Fact]
    public void Code_block_wraps_selection()
    {
        var r = MarkdownFormatter.InsertCodeBlock("var x = 1;", 0, 10);

        Assert.Equal("```\nvar x = 1;\n```\n", r.Text);
        Assert.Equal(3, r.SelectionStart);
    }

    [Fact]
    public void Code_block_after_paragraph_gets_padding()
    {
        var r = MarkdownFormatter.InsertCodeBlock("intro", 5, 0);

        Assert.Equal("intro\n\n```\n\n```\n", r.Text);
    }

    // ---- Table ----

    [Fact]
    public void Table_generates_header_separator_and_rows()
    {
        var r = MarkdownFormatter.InsertTable("", 0, 0, rows: 2, columns: 2);

        Assert.Equal(
            "| Header 1 | Header 2 |\n" +
            "| --- | --- |\n" +
            "|   |   |\n" +
            "|   |   |\n",
            r.Text);
        Assert.Equal(2, r.SelectionStart);
        Assert.Equal("Header 1".Length, r.SelectionLength);
    }

    [Fact]
    public void Table_defaults_to_three_by_three()
    {
        var r = MarkdownFormatter.InsertTable("", 0, 0);

        Assert.Equal(5, r.Text.TrimEnd('\n').Split('\n').Length); // header + separator + 3 rows
        Assert.Equal(4, r.Text.Split('\n')[0].Count(c => c == '|')); // 3 columns
    }

    [Theory]
    [InlineData(0, 3)]
    [InlineData(3, 0)]
    public void Table_rejects_degenerate_sizes(int rows, int columns)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => MarkdownFormatter.InsertTable("", 0, 0, rows, columns));
    }
}
