using Downer.Core;

namespace Downer.Tests;

public class TableFormatterTests
{
    [Fact]
    public void Aligns_ragged_columns()
    {
        var r = TableFormatter.FormatTableAt("|a|bb|\n|-|-|\n|ccc|d|", 0);

        Assert.Equal(
            "| a   | bb  |\n" +
            "| --- | --- |\n" +
            "| ccc | d   |",
            r!.Text);
    }

    [Fact]
    public void Preserves_alignment_colons()
    {
        var r = TableFormatter.FormatTableAt("|a|b|c|\n|:-|-:|:-:|\n|1|2|3|", 0);

        Assert.Equal(
            "| a   |   b |  c  |\n" +
            "|:--- | ---:|:---:|\n" +
            "| 1   |   2 |  3  |",
            r!.Text);
    }

    [Fact]
    public void Non_table_offset_returns_null()
    {
        Assert.Null(TableFormatter.FormatTableAt("just text", 3));
    }

    [Fact]
    public void Only_the_contiguous_block_is_touched()
    {
        var text = "before\n|a|b|\n|-|-|\nafter";
        var r = TableFormatter.FormatTableAt(text, 8);

        Assert.StartsWith("before\n", r!.Text);
        Assert.EndsWith("\nafter", r.Text);
        Assert.Contains("| a   | b   |", r.Text);
    }

    [Fact]
    public void Rows_with_missing_cells_are_padded()
    {
        var r = TableFormatter.FormatTableAt("|a|b|\n|-|-|\n|only|", 0);

        Assert.Contains("| only |     |", r!.Text);
    }

    [Fact]
    public void Crlf_documents_keep_crlf()
    {
        var r = TableFormatter.FormatTableAt("|a|b|\r\n|-|-|", 0);

        Assert.Contains("\r\n", r!.Text);
        Assert.DoesNotContain("\r\r", r.Text);
    }

    [Fact]
    public void Format_all_tables_handles_multiple_blocks()
    {
        var text = "|a|\n|-|\n\ntext\n\n|xx|\n|-|";
        var result = TableFormatter.FormatAllTables(text);

        Assert.Contains("| a   |", result);
        Assert.Contains("| xx  |", result);
        Assert.Contains("\ntext\n", result);
    }

    [Fact]
    public void Format_is_idempotent()
    {
        var once = TableFormatter.FormatTableAt("|a|bb|\n|-|-|\n|ccc|d|", 0)!.Text;
        var twice = TableFormatter.FormatTableAt(once, 0)!.Text;

        Assert.Equal(once, twice);
    }
}
