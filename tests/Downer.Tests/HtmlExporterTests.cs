using Downer.Core;

namespace Downer.Tests;

public class HtmlExporterTests
{
    [Fact]
    public void Renders_headings()
    {
        Assert.Contains("<h1", HtmlExporter.ToHtmlFragment("# Hello"));
    }

    [Fact]
    public void Renders_pipe_tables()
    {
        var html = HtmlExporter.ToHtmlFragment("| A | B |\n| --- | --- |\n| 1 | 2 |");

        Assert.Contains("<table", html);
        Assert.Contains("<td>1</td>", html);
    }

    [Fact]
    public void Renders_task_lists_as_checkboxes()
    {
        var html = HtmlExporter.ToHtmlFragment("- [x] done\n- [ ] todo");

        Assert.Contains("checkbox", html);
        Assert.Contains("checked", html);
    }

    [Fact]
    public void Renders_strikethrough()
    {
        Assert.Contains("<del>gone</del>", HtmlExporter.ToHtmlFragment("~~gone~~"));
    }

    [Fact]
    public void Renders_fenced_code_with_language_class()
    {
        var html = HtmlExporter.ToHtmlFragment("```csharp\nvar x = 1;\n```");

        Assert.Contains("language-csharp", html);
        Assert.Contains("<pre>", html);
    }

    [Fact]
    public void Autolinks_bare_urls()
    {
        Assert.Contains("<a href=\"https://example.com\"", HtmlExporter.ToHtmlFragment("Visit https://example.com today"));
    }

    [Fact]
    public void Strips_yaml_front_matter()
    {
        var html = HtmlExporter.ToHtmlFragment("---\ntitle: secret\n---\n\n# Real Content");

        Assert.DoesNotContain("secret", html);
        Assert.Contains("<h1", html);
    }

    [Fact]
    public void Document_is_standalone_html()
    {
        var html = HtmlExporter.ToHtmlDocument("# Hi", "My Doc");

        Assert.StartsWith("<!DOCTYPE html>", html);
        Assert.Contains("<title>My Doc</title>", html);
        Assert.Contains("<style>", html);
        Assert.Contains("<h1", html);
        Assert.Contains("prefers-color-scheme", html);
    }

    [Fact]
    public void Document_title_is_html_encoded()
    {
        var html = HtmlExporter.ToHtmlDocument("x", "<script>alert(1)</script>");

        Assert.DoesNotContain("<script>", html);
        Assert.Contains("&lt;script&gt;", html);
    }

    [Fact]
    public void Empty_markdown_still_produces_a_document()
    {
        var html = HtmlExporter.ToHtmlDocument("", "Empty");

        Assert.StartsWith("<!DOCTYPE html>", html);
    }
}
