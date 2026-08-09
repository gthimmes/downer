using System.Net;
using Markdig;

namespace Downer.Core;

/// <summary>Converts markdown to standalone HTML using Markdig's advanced pipeline.</summary>
public static class HtmlExporter
{
    private static readonly MarkdownPipeline Pipeline = new MarkdownPipelineBuilder()
        .UseAdvancedExtensions()
        .UseYamlFrontMatter()
        .Build();

    public static string ToHtmlFragment(string markdown) =>
        Markdig.Markdown.ToHtml(markdown ?? "", Pipeline);

    public static string ToHtmlDocument(string markdown, string title)
    {
        var body = ToHtmlFragment(markdown);
        var safeTitle = WebUtility.HtmlEncode(title);

        return $"""
            <!DOCTYPE html>
            <html lang="en">
            <head>
            <meta charset="utf-8">
            <meta name="viewport" content="width=device-width, initial-scale=1">
            <title>{safeTitle}</title>
            <style>
            {Stylesheet}
            </style>
            </head>
            <body>
            <main>
            {body}
            </main>
            </body>
            </html>
            """;
    }

    private const string Stylesheet = """
        :root {
          color-scheme: light dark;
          --fg: #1f2328;
          --bg: #ffffff;
          --muted: #59636e;
          --border: #d1d9e0;
          --code-bg: #f6f8fa;
          --accent: #0969da;
        }
        @media (prefers-color-scheme: dark) {
          :root {
            --fg: #f0f6fc;
            --bg: #0d1117;
            --muted: #9198a1;
            --border: #3d444d;
            --code-bg: #151b23;
            --accent: #4493f8;
          }
        }
        * { box-sizing: border-box; }
        body {
          margin: 0;
          color: var(--fg);
          background: var(--bg);
          font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", "Noto Sans", Helvetica, Arial, sans-serif;
          font-size: 16px;
          line-height: 1.6;
        }
        main { max-width: 52rem; margin: 0 auto; padding: 2rem 1.5rem 4rem; }
        h1, h2, h3, h4, h5, h6 { margin: 1.5em 0 0.5em; line-height: 1.25; }
        h1 { font-size: 2em; border-bottom: 1px solid var(--border); padding-bottom: 0.3em; }
        h2 { font-size: 1.5em; border-bottom: 1px solid var(--border); padding-bottom: 0.3em; }
        a { color: var(--accent); }
        code, pre {
          font-family: ui-monospace, SFMono-Regular, "Cascadia Code", Consolas, "Liberation Mono", Menlo, monospace;
          font-size: 0.875em;
        }
        code { background: var(--code-bg); padding: 0.2em 0.4em; border-radius: 6px; }
        pre { background: var(--code-bg); padding: 1rem; border-radius: 6px; overflow-x: auto; }
        pre code { background: none; padding: 0; }
        blockquote {
          margin: 0 0 1em;
          padding: 0 1em;
          color: var(--muted);
          border-left: 0.25em solid var(--border);
        }
        table { border-collapse: collapse; display: block; overflow-x: auto; margin-bottom: 1em; }
        th, td { border: 1px solid var(--border); padding: 0.4em 0.8em; }
        th { background: var(--code-bg); }
        img { max-width: 100%; }
        hr { border: 0; border-top: 1px solid var(--border); margin: 2em 0; }
        input[type="checkbox"] { margin-right: 0.4em; }
        """;
}
