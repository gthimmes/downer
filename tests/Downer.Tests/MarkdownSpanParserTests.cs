using Downer.Core;

namespace Downer.Tests;

public class MarkdownSpanParserTests
{
    private static StyledSpan Single(LineSpans result, SpanKind kind, bool isMarker) =>
        Assert.Single(result.Spans, s => s.Kind == kind && s.IsMarker == isMarker);

    // ---- Headings ----

    [Fact]
    public void Heading_reports_level_and_marker_span()
    {
        var r = MarkdownSpanParser.ParseLine("## Title");

        Assert.Equal(2, r.HeadingLevel);
        var marker = Single(r, SpanKind.HeadingMarker, isMarker: true);
        Assert.Equal(0, marker.Start);
        Assert.Equal(3, marker.Length); // "## "
    }

    [Fact]
    public void Plain_line_has_no_heading()
    {
        Assert.Equal(0, MarkdownSpanParser.ParseLine("just text").HeadingLevel);
    }

    [Fact]
    public void Seven_hashes_is_not_a_heading()
    {
        Assert.Equal(0, MarkdownSpanParser.ParseLine("####### nope").HeadingLevel);
    }

    // ---- Bold / italic / strike ----

    [Fact]
    public void Bold_produces_markers_and_content()
    {
        var r = MarkdownSpanParser.ParseLine("a **bold** b");

        var content = Single(r, SpanKind.Bold, isMarker: false);
        Assert.Equal(4, content.Start);
        Assert.Equal(4, content.Length);
        Assert.Equal(2, r.Spans.Count(s => s.Kind == SpanKind.Bold && s.IsMarker));
    }

    [Fact]
    public void Italic_star_and_underscore_both_parse()
    {
        Assert.Single(MarkdownSpanParser.ParseLine("x *it* y").Spans, s => s.Kind == SpanKind.Italic && !s.IsMarker);
        Assert.Single(MarkdownSpanParser.ParseLine("x _it_ y").Spans, s => s.Kind == SpanKind.Italic && !s.IsMarker);
    }

    [Fact]
    public void Intraword_underscore_is_not_italic()
    {
        Assert.DoesNotContain(MarkdownSpanParser.ParseLine("snake_case_name").Spans, s => s.Kind == SpanKind.Italic);
    }

    [Fact]
    public void Bold_italic_triple_star_yields_both_styles()
    {
        var r = MarkdownSpanParser.ParseLine("***both***");

        Assert.Contains(r.Spans, s => s.Kind == SpanKind.Bold && !s.IsMarker);
        Assert.Contains(r.Spans, s => s.Kind == SpanKind.Italic && !s.IsMarker);
    }

    [Fact]
    public void Italic_nests_inside_bold_content()
    {
        var r = MarkdownSpanParser.ParseLine("**a _b_ c**");

        Assert.Contains(r.Spans, s => s.Kind == SpanKind.Bold && !s.IsMarker);
        var italic = Single(r, SpanKind.Italic, isMarker: false);
        Assert.Equal(5, italic.Start); // "b"
    }

    [Fact]
    public void Strikethrough_parses()
    {
        var content = Single(MarkdownSpanParser.ParseLine("~~gone~~"), SpanKind.Strikethrough, isMarker: false);
        Assert.Equal(2, content.Start);
        Assert.Equal(4, content.Length);
    }

    [Fact]
    public void Loose_star_between_spaces_is_not_italic()
    {
        Assert.DoesNotContain(MarkdownSpanParser.ParseLine("2 * 3 * 4").Spans, s => s.Kind == SpanKind.Italic);
    }

    // ---- Inline code ----

    [Fact]
    public void Inline_code_suppresses_emphasis_inside()
    {
        var r = MarkdownSpanParser.ParseLine("`**not bold**`");

        Assert.Contains(r.Spans, s => s.Kind == SpanKind.Code && !s.IsMarker);
        Assert.DoesNotContain(r.Spans, s => s.Kind == SpanKind.Bold);
    }

    [Fact]
    public void Bold_still_applies_outside_code()
    {
        var r = MarkdownSpanParser.ParseLine("**b** and `c`");

        Assert.Contains(r.Spans, s => s.Kind == SpanKind.Bold && !s.IsMarker);
        Assert.Contains(r.Spans, s => s.Kind == SpanKind.Code && !s.IsMarker);
    }

    [Fact]
    public void Double_backtick_code_parses()
    {
        var content = Single(MarkdownSpanParser.ParseLine("``a ` b``"), SpanKind.Code, isMarker: false);
        Assert.Equal(2, content.Start);
        Assert.Equal(5, content.Length);
    }

    // ---- Links and images ----

    [Fact]
    public void Link_splits_into_text_url_and_punctuation()
    {
        var r = MarkdownSpanParser.ParseLine("[click](https://x.com)");

        var text = Single(r, SpanKind.LinkText, isMarker: false);
        Assert.Equal(1, text.Start);
        Assert.Equal(5, text.Length);
        var url = Single(r, SpanKind.LinkUrl, isMarker: true);
        Assert.Equal(8, url.Start);
        Assert.Equal(13, url.Length);
        Assert.Equal(3, r.Spans.Count(s => s.Kind == SpanKind.LinkPunctuation));
    }

    [Fact]
    public void Image_includes_bang_in_opening_punctuation()
    {
        var r = MarkdownSpanParser.ParseLine("![alt](img.png)");

        var open = r.Spans.First(s => s.Kind == SpanKind.LinkPunctuation);
        Assert.Equal(0, open.Start);
        Assert.Equal(2, open.Length); // "!["
    }

    [Fact]
    public void Emphasis_does_not_fire_inside_urls()
    {
        var r = MarkdownSpanParser.ParseLine("[t](https://x.com/a_b_c)");

        Assert.DoesNotContain(r.Spans, s => s.Kind == SpanKind.Italic);
    }

    // ---- Quotes and lists ----

    [Fact]
    public void Quote_marker_is_claimed()
    {
        var r = MarkdownSpanParser.ParseLine("> quoted **bold**");

        var quote = Single(r, SpanKind.QuoteMarker, isMarker: true);
        Assert.Equal(0, quote.Start);
        Assert.Contains(r.Spans, s => s.Kind == SpanKind.Bold);
    }

    [Fact]
    public void Nested_quote_markers_form_one_span()
    {
        var quote = Single(MarkdownSpanParser.ParseLine("> > deep"), SpanKind.QuoteMarker, isMarker: true);
        Assert.Equal(4, quote.Length); // "> > "
    }

    [Fact]
    public void Bullet_marker_is_not_mistaken_for_italic()
    {
        var r = MarkdownSpanParser.ParseLine("* item");

        Assert.Contains(r.Spans, s => s.Kind == SpanKind.ListMarker);
        Assert.DoesNotContain(r.Spans, s => s.Kind == SpanKind.Italic);
    }

    [Fact]
    public void Ordered_marker_parses()
    {
        var marker = Single(MarkdownSpanParser.ParseLine("12. item"), SpanKind.ListMarker, isMarker: true);
        Assert.Equal(4, marker.Length); // "12. "
    }

    [Fact]
    public void Task_checkbox_is_a_separate_span()
    {
        var r = MarkdownSpanParser.ParseLine("- [x] done");

        Assert.Contains(r.Spans, s => s.Kind == SpanKind.ListMarker);
        var task = Single(r, SpanKind.TaskCheckbox, isMarker: true);
        Assert.Equal(2, task.Start);
    }

    [Fact]
    public void Heading_content_still_gets_inline_styles()
    {
        var r = MarkdownSpanParser.ParseLine("# Big **bold** title");

        Assert.Equal(1, r.HeadingLevel);
        Assert.Contains(r.Spans, s => s.Kind == SpanKind.Bold && !s.IsMarker);
    }

    [Fact]
    public void Quoted_heading_reports_both()
    {
        var r = MarkdownSpanParser.ParseLine("> ## Quoted heading");

        Assert.Equal(2, r.HeadingLevel);
        Assert.Contains(r.Spans, s => s.Kind == SpanKind.QuoteMarker);
    }

    // ---- Tables ----

    [Fact]
    public void Table_row_pipes_are_grid_markers()
    {
        var r = MarkdownSpanParser.ParseLine("| a | b |");

        Assert.Equal(3, r.Spans.Count(s => s.Kind == SpanKind.TablePipe && s.IsMarker));
    }

    [Fact]
    public void Table_separator_row_is_one_span()
    {
        var r = MarkdownSpanParser.ParseLine("| --- | :-: |");

        var sep = Single(r, SpanKind.TableSeparator, isMarker: true);
        Assert.Equal(0, sep.Start);
        Assert.Equal(13, sep.Length);
    }

    [Fact]
    public void Emphasis_still_works_inside_table_cells()
    {
        var r = MarkdownSpanParser.ParseLine("| **bold** | `code` |");

        Assert.Contains(r.Spans, s => s.Kind == SpanKind.Bold && !s.IsMarker);
        Assert.Contains(r.Spans, s => s.Kind == SpanKind.Code && !s.IsMarker);
    }

    [Fact]
    public void Table_row_is_not_mistaken_for_other_structures()
    {
        var r = MarkdownSpanParser.ParseLine("| - item | > quote |");

        Assert.DoesNotContain(r.Spans, s => s.Kind == SpanKind.ListMarker);
        Assert.DoesNotContain(r.Spans, s => s.Kind == SpanKind.QuoteMarker);
    }

    // ---- Fence markers ----

    [Theory]
    [InlineData("```csharp", 3)]
    [InlineData("  ~~~", 5)]
    [InlineData("plain", 0)]
    public void Fence_marker_length_detects_delimiters(string line, int expected)
    {
        Assert.Equal(expected, CodeFences.FenceMarkerLength(line));
    }

    // ---- Fence states ----

    [Fact]
    public void Lines_inside_fences_get_no_spans()
    {
        var r = MarkdownSpanParser.ParseLine("**not bold**", FenceLineState.Inside);

        Assert.Empty(r.Spans);
        Assert.Equal(FenceLineState.Inside, r.FenceState);
    }

    [Fact]
    public void Fence_analysis_tracks_regions()
    {
        var states = CodeFences.Analyze("a\n```cs\ncode\n```\nb");

        Assert.Equal(FenceLineState.Outside, states[0]);
        Assert.Equal(FenceLineState.Delimiter, states[1]);
        Assert.Equal(FenceLineState.Inside, states[2]);
        Assert.Equal(FenceLineState.Delimiter, states[3]);
        Assert.Equal(FenceLineState.Outside, states[4]);
    }

    [Fact]
    public void Unclosed_fence_runs_to_end()
    {
        var states = CodeFences.Analyze("```\nstill code");

        Assert.Equal(FenceLineState.Inside, states[1]);
    }

    [Fact]
    public void Tilde_fence_is_not_closed_by_backticks()
    {
        var states = CodeFences.Analyze("~~~\n```\n~~~");

        Assert.Equal(FenceLineState.Inside, states[1]);
        Assert.Equal(FenceLineState.Delimiter, states[2]);
    }

    [Fact]
    public void Empty_line_parses_cleanly()
    {
        Assert.Empty(MarkdownSpanParser.ParseLine("").Spans);
    }
}
