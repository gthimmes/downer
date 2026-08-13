using System.Text;
using Downer.Core;

namespace Downer.Tests;

public class PdfExporterTests
{
    private static string ExportText(string markdown, string title = "Doc") =>
        Encoding.Latin1.GetString(PdfExporter.Export(markdown, title));

    [Fact]
    public void Produces_a_wellformed_pdf_shell()
    {
        var pdf = ExportText("# Hello\n\nSome text.");

        Assert.StartsWith("%PDF-1.4", pdf);
        Assert.EndsWith("%%EOF\n", pdf);
        Assert.Contains("/Type /Catalog", pdf);
        Assert.Contains("/Type /Pages", pdf);
        Assert.Contains("/Count 1", pdf);
        Assert.Contains("startxref", pdf);
    }

    [Fact]
    public void Xref_offsets_point_at_their_objects()
    {
        var pdf = ExportText("Just one line.");

        var xref = pdf.IndexOf("xref\n0 ", StringComparison.Ordinal);
        Assert.True(xref > 0);

        var entries = pdf[xref..].Split('\n').Skip(3).TakeWhile(l => l.EndsWith("n ")).ToArray();
        Assert.NotEmpty(entries);
        for (var i = 0; i < entries.Length; i++)
        {
            var offset = int.Parse(entries[i][..10]);
            Assert.Equal($"{i + 1} 0 obj", pdf.Substring(offset, $"{i + 1} 0 obj".Length));
        }
    }

    [Fact]
    public void Text_content_lands_in_the_stream_with_markers_concealed()
    {
        var pdf = ExportText("# My Heading\n\nplain **bolded** words");

        Assert.Contains("(My) Tj", pdf);
        Assert.Contains("(Heading) Tj", pdf);
        Assert.Contains("(bolded) Tj", pdf);
        Assert.DoesNotContain("**", pdf.Substring(pdf.IndexOf("stream", StringComparison.Ordinal)));
        Assert.DoesNotContain("(#", pdf);
    }

    [Fact]
    public void Styles_select_the_right_base14_fonts()
    {
        var pdf = ExportText("normal **bold** _italic_ `mono`");

        Assert.Contains("/F1 11", pdf);   // Helvetica
        Assert.Contains("/F2 11", pdf);   // Helvetica-Bold
        Assert.Contains("/F3 11", pdf);   // Helvetica-Oblique
        Assert.Contains("/F5 11", pdf);   // Courier for inline code
        Assert.Contains("/BaseFont /Helvetica-Bold", pdf);
        Assert.Contains("/BaseFont /Courier", pdf);
    }

    [Fact]
    public void Long_documents_break_onto_multiple_pages()
    {
        var body = string.Join("\n\n", Enumerable.Range(1, 120).Select(i => $"Paragraph number {i} right here."));
        var pdf = ExportText(body);

        var countStart = pdf.IndexOf("/Count ", StringComparison.Ordinal) + "/Count ".Length;
        var count = int.Parse(pdf[countStart..pdf.IndexOf(' ', countStart)]);

        Assert.True(count >= 2, $"expected multiple pages, got {count}");
        Assert.Equal(count, CountOccurrences(pdf, "/Type /Page /Parent"));
    }

    [Fact]
    public void Special_characters_are_escaped_or_mapped()
    {
        var pdf = ExportText("parens (here) and a backslash \\ and an em—dash");

        Assert.Contains("\\(here\\)", pdf);
        Assert.Contains("\\\\", pdf);
        Assert.Contains("\\227", pdf);    // — in WinAnsi octal
    }

    [Fact]
    public void Unmappable_characters_degrade_to_question_marks()
    {
        var pdf = ExportText("checkmark ✓ char");

        Assert.Contains("(?) Tj", pdf);
    }

    [Fact]
    public void Fence_delimiters_vanish_but_code_lines_render_mono()
    {
        var pdf = ExportText("```csharp\nvar x = 1;\n```\nafter");

        Assert.DoesNotContain("(```", pdf);
        Assert.DoesNotContain("csharp", pdf[pdf.IndexOf("stream", StringComparison.Ordinal)..]);
        Assert.Contains("var x = 1;", pdf);
        Assert.Contains("/F5 9.5", pdf);
    }

    [Fact]
    public void Lists_and_tasks_get_print_glyphs()
    {
        var pdf = ExportText("- bullet item\n- [x] done thing\n1. ordered thing");

        Assert.Contains("(\\225 ) Tj", pdf);  // • bullet in WinAnsi
        Assert.Contains("([x] ) Tj", pdf);
        Assert.Contains("(1. ) Tj", pdf);
    }

    [Fact]
    public void Document_title_reaches_the_info_dictionary()
    {
        var pdf = ExportText("hello", "My Notes");

        Assert.Contains("/Title (My Notes)", pdf);
    }

    [Fact]
    public void Empty_document_still_produces_a_valid_single_page()
    {
        var pdf = ExportText("");

        Assert.StartsWith("%PDF-1.4", pdf);
        Assert.Contains("/Count 1", pdf);
    }

    [Fact]
    public void Sample_document_exports_to_disk_for_inspection()
    {
        var markdown =
            "# PDF Export Sample\n\n" +
            "This paragraph has **bold**, _italic_, `inline code`, and a [link](https://example.com).\n\n" +
            "## Lists\n\n- first bullet\n- second bullet\n- [x] a finished task\n- [ ] an open task\n\n" +
            "1. ordered one\n2. ordered two\n\n" +
            "> A quoted line with some wisdom in it.\n\n" +
            "```csharp\nvar answer = 42;\nConsole.WriteLine(answer);\n```\n\n" +
            "| Fruit | Count |\n| ----- | ----- |\n| apple | 3     |\n| pear  | 12    |\n\n" +
            "---\n\nText after a horizontal rule — with an em dash and “curly quotes”.\n";

        var bytes = PdfExporter.Export(markdown, "Sample");
        var dir = Path.Combine(AppContext.BaseDirectory, "pdf-shots");
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "sample.pdf");
        File.WriteAllBytes(path, bytes);

        Assert.True(new FileInfo(path).Length > 500);
    }

    private static int CountOccurrences(string haystack, string needle)
    {
        var count = 0;
        var index = 0;
        while ((index = haystack.IndexOf(needle, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += needle.Length;
        }
        return count;
    }
}
