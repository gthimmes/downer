using Downer.Core;

namespace Downer.Tests;

public class SpellWordsTests
{
    private static string[] WordsOf(string line, FenceLineState state = FenceLineState.Outside) =>
        SpellWords.Extract(line, state).Select(w => w.Word).ToArray();

    [Fact]
    public void Plain_prose_yields_each_word_with_position()
    {
        var words = SpellWords.Extract("Hello wonderful world");

        Assert.Equal(3, words.Count);
        Assert.Equal(new WordSpan(0, 5, "Hello"), words[0]);
        Assert.Equal(new WordSpan(6, 9, "wonderful"), words[1]);
        Assert.Equal(new WordSpan(16, 5, "world"), words[2]);
    }

    [Fact]
    public void Inline_code_is_skipped()
    {
        Assert.Equal(new[] { "call", "here" }, WordsOf("call `fooBarBaz qux` here"));
    }

    [Fact]
    public void Link_text_is_checked_but_url_is_not()
    {
        Assert.Equal(new[] { "see", "docs", "here" }, WordsOf("see [docs here](https://exmple.com/pth)"));
    }

    [Fact]
    public void Bare_urls_and_emails_are_skipped()
    {
        Assert.Equal(new[] { "visit", "or", "mail" },
            WordsOf("visit https://exampl.com or mail someone@exampl.com"));
        Assert.Equal(new[] { "at" }, WordsOf("at www.exampl.com"));
    }

    [Fact]
    public void Markers_are_not_words()
    {
        Assert.Equal(new[] { "Heading", "text" }, WordsOf("## Heading text"));
        Assert.Equal(new[] { "quoted" }, WordsOf("> quoted"));
        Assert.Equal(new[] { "item" }, WordsOf("- [x] item"));
    }

    [Fact]
    public void Emphasis_content_is_still_checked()
    {
        Assert.Equal(new[] { "realy", "important" }, WordsOf("**realy** _important_"));
    }

    [Fact]
    public void Fenced_lines_yield_nothing()
    {
        Assert.Empty(WordsOf("var x = misspeled;", FenceLineState.Inside));
        Assert.Empty(WordsOf("```csharp", FenceLineState.Delimiter));
    }

    [Fact]
    public void Identifier_like_tokens_are_skipped()
    {
        Assert.Empty(WordsOf("NASA HTTP"));                       // ALL-CAPS
        Assert.Empty(WordsOf("someVariable CamelCase"));          // mixed case
        Assert.Equal(new[] { "and" }, WordsOf("XmlHttpRequest and iOS"));
    }

    [Fact]
    public void Short_and_non_ascii_words_are_skipped()
    {
        Assert.Empty(WordsOf("a I x"));
        Assert.Empty(WordsOf("café naïve"));
    }

    [Fact]
    public void Apostrophes_stay_inside_words_but_quotes_do_not()
    {
        var words = SpellWords.Extract("don't 'quoted'");

        Assert.Equal("don't", words[0].Word);
        Assert.Equal("quoted", words[1].Word);
    }

    [Fact]
    public void Table_cells_are_checked_but_pipes_and_separators_are_not()
    {
        Assert.Equal(new[] { "aple", "banan" }, WordsOf("| aple | banan |"));
        Assert.Empty(WordsOf("| --- | --- |"));
    }

    [Fact]
    public void Empty_line_yields_nothing()
    {
        Assert.Empty(WordsOf(""));
    }
}
