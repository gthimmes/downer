using Downer.Core;

namespace Downer.Tests;

public class DocumentStatsTests
{
    [Fact]
    public void Empty_text_has_zero_words_and_one_line()
    {
        var stats = DocumentStats.Count("");

        Assert.Equal(0, stats.Words);
        Assert.Equal(0, stats.Characters);
        Assert.Equal(1, stats.Lines);
    }

    [Fact]
    public void Counts_simple_sentence()
    {
        var stats = DocumentStats.Count("The quick brown fox");

        Assert.Equal(4, stats.Words);
        Assert.Equal(19, stats.Characters);
        Assert.Equal(1, stats.Lines);
    }

    [Fact]
    public void Punctuation_is_not_a_word()
    {
        Assert.Equal(2, DocumentStats.Count("Hello, world!").Words);
    }

    [Theory]
    [InlineData("well-known", 1)]
    [InlineData("don't", 1)]
    [InlineData("it’s fine", 2)]
    [InlineData("snake_case_name", 1)]
    public void Compound_words_count_once(string text, int expected)
    {
        Assert.Equal(expected, DocumentStats.Count(text).Words);
    }

    [Fact]
    public void Numbers_count_as_words()
    {
        Assert.Equal(3, DocumentStats.Count("version 2 beta").Words);
    }

    [Fact]
    public void Accented_words_count()
    {
        Assert.Equal(2, DocumentStats.Count("café déjà").Words);
    }

    [Fact]
    public void Counts_lines_across_newlines()
    {
        var stats = DocumentStats.Count("a\nb\nc");

        Assert.Equal(3, stats.Lines);
        Assert.Equal(3, stats.Words);
    }

    [Fact]
    public void Crlf_counts_as_one_line_break()
    {
        Assert.Equal(2, DocumentStats.Count("a\r\nb").Lines);
    }

    [Fact]
    public void Markdown_syntax_characters_are_not_words()
    {
        Assert.Equal(2, DocumentStats.Count("## **bold** _text_ ---").Words);
    }
}
