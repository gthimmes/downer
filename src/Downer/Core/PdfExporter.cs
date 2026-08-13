using System.Globalization;
using System.Text;

namespace Downer.Core;

/// <summary>
/// Dependency-free markdown-to-PDF exporter built on the PDF base-14 fonts
/// (Helvetica + Courier, WinAnsi encoding). Layout mirrors the WYSIWYG surface:
/// markers are concealed, headings are sized, bullets/checkboxes get glyphs,
/// quotes get bars, code blocks sit on a tinted background, and tables stay
/// monospace so their grids line up. Character widths are class-based
/// approximations, so wrapping is conservative rather than typographically exact.
/// </summary>
public static class PdfExporter
{
    private const double PageWidth = 595.28;   // A4 portrait, in points
    private const double PageHeight = 841.89;
    private const double Margin = 56;
    private const double BaseSize = 11;
    private const double CodeSize = 9.5;
    private const double LineSpacing = 1.45;
    private const double UsableWidth = PageWidth - 2 * Margin;

    private readonly record struct Style(bool Bold, bool Italic, bool Code);

    public static byte[] Export(string markdown, string title)
    {
        var pages = new List<string>();
        var layout = new Layout(pages);

        var fenceStates = CodeFences.Analyze(markdown);
        var lines = markdown.Split('\n');

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i].TrimEnd('\r');
            var state = i < fenceStates.Length ? fenceStates[i] : FenceLineState.Outside;

            if (state == FenceLineState.Delimiter)
                continue; // fence syntax has no place on paper

            if (state == FenceLineState.Inside)
            {
                layout.EmitCodeLine(line);
                continue;
            }

            if (line.Trim().Length == 0)
            {
                layout.EmitParagraphGap();
                continue;
            }

            var info = MarkdownSpanParser.ParseLine(line);

            if (info.Spans.Any(s => s.Kind == SpanKind.HorizontalRule))
            {
                layout.EmitRule();
                continue;
            }

            if (info.Spans.Any(s => s.Kind is SpanKind.TablePipe or SpanKind.TableSeparator))
            {
                layout.EmitTableLine(line);
                continue;
            }

            layout.EmitProseLine(line, info);
        }

        layout.FinishPage();
        return Assemble(pages, title);
    }

    // ---- Layout engine: flows styled words down the page, breaking pages as needed ----

    private sealed class Layout(List<string> pages)
    {
        private StringBuilder _sb = new();
        private double _y = PageHeight - Margin;
        private bool _pageHasContent;

        public void FinishPage()
        {
            pages.Add(_sb.ToString());
        }

        private void NewPage()
        {
            FinishPage();
            _sb = new StringBuilder();
            _y = PageHeight - Margin;
            _pageHasContent = false;
        }

        private void Advance(double lineHeight)
        {
            _y -= lineHeight;
            if (_y < Margin)
                NewPage();
        }

        public void EmitParagraphGap()
        {
            if (_pageHasContent)
                Advance(BaseSize * 0.8);
        }

        public void EmitRule()
        {
            Advance(BaseSize * LineSpacing);
            _sb.Append(F("0.78 0.78 0.80 RG 1 w {0} {1} m {2} {1} l S\n",
                Margin, _y + BaseSize * 0.4, PageWidth - Margin));
            _pageHasContent = true;
        }

        public void EmitCodeLine(string text)
        {
            var lineHeight = CodeSize * LineSpacing;
            Advance(lineHeight);
            _sb.Append(F("0.955 0.958 0.965 rg {0} {1} {2} {3} re f 0 0 0 rg\n",
                Margin - 4, _y - CodeSize * 0.28, UsableWidth + 8, lineHeight));
            ShowText(text, new Style(false, false, true), CodeSize, Margin);
            _pageHasContent = true;
        }

        public void EmitTableLine(string text)
        {
            Advance(CodeSize * LineSpacing);
            ShowText(text, new Style(false, false, true), CodeSize, Margin);
            _pageHasContent = true;
        }

        public void EmitProseLine(string line, LineSpans info)
        {
            var size = info.HeadingLevel switch
            {
                1 => 22.0,
                2 => 18.0,
                3 => 15.0,
                4 => 13.0,
                > 0 => BaseSize,
                _ => BaseSize,
            };
            var headingBold = info.HeadingLevel is > 0 and <= 4;
            var lineHeight = size * LineSpacing;

            // Char-level style map; marker spans are concealed like the editor does.
            var skip = new bool[line.Length];
            var bold = new bool[line.Length];
            var italic = new bool[line.Length];
            var code = new bool[line.Length];

            var indent = 0.0;
            var quoteDepth = 0;
            string? prefix = null;
            var prefixStyle = new Style(false, false, false);

            foreach (var span in info.Spans)
            {
                if (span.IsMarker)
                {
                    for (var i = span.Start; i < span.End && i < line.Length; i++)
                        skip[i] = true;

                    switch (span.Kind)
                    {
                        case SpanKind.QuoteMarker:
                            quoteDepth = line.AsSpan(span.Start, span.Length).Count('>');
                            indent += 14 * quoteDepth;
                            break;
                        case SpanKind.ListMarker:
                            prefix ??= char.IsDigit(line[span.Start])
                                ? line.Substring(span.Start, span.Length).TrimEnd() + " "
                                : "• ";
                            break;
                        case SpanKind.TaskCheckbox:
                            var box = line.Substring(span.Start, span.Length);
                            prefix = box.Contains('x') || box.Contains('X') ? "[x] " : "[ ] ";
                            prefixStyle = new Style(false, false, true);
                            break;
                    }
                    continue;
                }

                for (var i = span.Start; i < span.End && i < line.Length; i++)
                {
                    if (span.Kind == SpanKind.Bold) bold[i] = true;
                    if (span.Kind == SpanKind.Italic) italic[i] = true;
                    if (span.Kind == SpanKind.Code) code[i] = true;
                }
            }

            // Tokenize the visible characters into styled words. A style change
            // mid-token ("**bold**,") splits the word but glues the halves so no
            // phantom space appears between them.
            var words = new List<(string Text, Style Style, bool Glue)>();
            var buffer = new StringBuilder();
            var current = new Style(false, false, false);
            var glueNext = false;

            void Flush(bool glueFollowing)
            {
                if (buffer.Length > 0)
                {
                    words.Add((buffer.ToString(), current, glueNext));
                    buffer.Clear();
                    glueNext = glueFollowing;
                }
            }

            for (var i = 0; i < line.Length; i++)
            {
                if (skip[i])
                    continue;
                if (line[i] == ' ' || line[i] == '\t')
                {
                    Flush(glueFollowing: false);
                    glueNext = false;
                    continue;
                }

                var style = new Style(headingBold || bold[i], italic[i], code[i]);
                if (buffer.Length > 0 && style != current)
                    Flush(glueFollowing: true);
                current = style;
                buffer.Append(line[i]);
            }
            Flush(glueFollowing: false);

            // The list/task prefix hangs left of the text, so the indent must fit it.
            if (prefix is not null)
                indent += Math.Max(16, Measure(prefix, prefixStyle, size) + 3);

            // Flow the words with wrapping; continuation lines keep the indent.
            Advance(lineHeight);
            if (quoteDepth > 0)
                DrawQuoteBars(quoteDepth);

            if (prefix is not null)
                ShowText(prefix, prefixStyle, size, Margin + indent - Math.Max(16, Measure(prefix, prefixStyle, size) + 3));

            var x = Margin + indent;
            var first = true;
            foreach (var (text, style, glue) in words)
            {
                if (!first && !glue)
                    x += Measure(" ", style, size);

                var width = Measure(text, style, size);
                if (x + width > Margin + UsableWidth && x > Margin + indent)
                {
                    Advance(lineHeight);
                    if (quoteDepth > 0)
                        DrawQuoteBars(quoteDepth);
                    x = Margin + indent;
                }

                ShowText(text, style, size, x);
                x += width;
                first = false;
            }

            if (info.HeadingLevel > 0)
                Advance(size * 0.35); // breathing room under headings

            _pageHasContent = true;
        }

        private void DrawQuoteBars(int depth)
        {
            for (var level = 1; level <= depth; level++)
            {
                var x = Margin + 14 * level - 10;
                _sb.Append(F("0.62 0.60 0.85 RG 1.5 w {0} {1} m {0} {2} l S\n",
                    x, _y - 2, _y + BaseSize * 0.9));
            }
        }

        private void ShowText(string text, Style style, double size, double x)
        {
            if (text.Length == 0)
                return;

            _sb.Append(F("BT /{0} {1} Tf {2} {3} Td (", FontKey(style), size, x, _y));
            AppendEscaped(_sb, text);
            _sb.Append(") Tj ET\n");
        }
    }

    private static string FontKey(Style style) => style switch
    {
        { Code: true } => "F5",
        { Bold: true, Italic: true } => "F4",
        { Bold: true } => "F2",
        { Italic: true } => "F3",
        _ => "F1",
    };

    // ---- Measurement: class-based width approximation, deliberately conservative ----

    private static double Measure(string text, Style style, double size)
    {
        var em = 0.0;
        foreach (var c in text)
            em += CharEm(c, style);
        return em * size * 1.02;
    }

    private static double CharEm(char c, Style style)
    {
        if (style.Code)
            return 0.60; // Courier is fixed-pitch

        var em = c switch
        {
            'i' or 'j' or 'l' or '\'' or '.' or ',' or ';' or ':' or '!' or '|' => 0.30,
            'f' or 't' or 'r' or 'I' or '(' or ')' or '[' or ']' or '-' => 0.37,
            'm' or 'w' or 'M' or 'W' or '@' => 0.92,
            '—' => 1.00,
            '–' or '•' => 0.65,
            ' ' => 0.30,
            >= 'A' and <= 'Z' => 0.71,
            >= '0' and <= '9' => 0.56,
            _ => 0.54,
        };
        return style.Bold ? em * 1.06 : em;
    }

    // ---- PDF plumbing ----

    private static string F(string format, params object[] args) =>
        string.Format(CultureInfo.InvariantCulture, format,
            args.Select(a => a is double d ? (object)Math.Round(d, 2) : a).ToArray());

    private static byte ToWinAnsi(char c) => c switch
    {
        >= ' ' and <= '~' => (byte)c,
        '‘' => 0x91, // ‘
        '’' => 0x92, // ’
        '“' => 0x93, // “
        '”' => 0x94, // ”
        '–' => 0x96, // –
        '—' => 0x97, // —
        '…' => 0x85, // …
        '•' => 0x95, // •
        '€' => 0x80, // €
        '™' => 0x99, // ™
        >= ' ' and <= 'ÿ' => (byte)c,
        _ => (byte)'?',
    };

    private static void AppendEscaped(StringBuilder sb, string text)
    {
        foreach (var c in text)
        {
            var b = ToWinAnsi(c);
            switch (b)
            {
                case (byte)'(': sb.Append("\\("); break;
                case (byte)')': sb.Append("\\)"); break;
                case (byte)'\\': sb.Append("\\\\"); break;
                default:
                    if (b is >= 32 and <= 126)
                        sb.Append((char)b);
                    else
                        sb.Append('\\').Append(Convert.ToString(b, 8).PadLeft(3, '0'));
                    break;
            }
        }
    }

    private static readonly string[] FontNames =
    {
        "Helvetica", "Helvetica-Bold", "Helvetica-Oblique", "Helvetica-BoldOblique", "Courier",
    };

    private static byte[] Assemble(List<string> pages, string title)
    {
        // Object plan: 1 catalog, 2 pages root, 3..7 fonts,
        // then per page (content stream, page) pairs, then info.
        var objects = new List<string>();

        objects.Add("<< /Type /Catalog /Pages 2 0 R >>");

        var firstPageId = 8;
        var kids = string.Join(" ", pages.Select((_, i) => $"{firstPageId + i * 2 + 1} 0 R"));
        objects.Add($"<< /Type /Pages /Kids [{kids}] /Count {pages.Count} >>");

        for (var i = 0; i < FontNames.Length; i++)
        {
            objects.Add(
                $"<< /Type /Font /Subtype /Type1 /BaseFont /{FontNames[i]} /Encoding /WinAnsiEncoding >>");
        }

        var fontDict = "<< " + string.Join(" ", FontNames.Select((_, i) => $"/F{i + 1} {i + 3} 0 R")) + " >>";

        foreach (var content in pages)
        {
            objects.Add($"<< /Length {content.Length} >>\nstream\n{content}endstream");
            objects.Add(
                $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 {PageWidth.ToString(CultureInfo.InvariantCulture)} " +
                $"{PageHeight.ToString(CultureInfo.InvariantCulture)}] " +
                $"/Resources << /Font {fontDict} >> /Contents {objects.Count} 0 R >>"); // the stream just added
        }

        var titleSb = new StringBuilder();
        AppendEscaped(titleSb, title);
        objects.Add($"<< /Title ({titleSb}) /Producer (Downer) >>");
        var infoId = objects.Count;

        // Serialize with byte offsets for the xref table.
        var sb = new StringBuilder();
        sb.Append("%PDF-1.4\n%âãÏÓ\n");

        var offsets = new List<int>();
        for (var i = 0; i < objects.Count; i++)
        {
            offsets.Add(sb.Length);
            sb.Append(F("{0} 0 obj\n", i + 1)).Append(objects[i]).Append("\nendobj\n");
        }

        var xrefStart = sb.Length;
        sb.Append(F("xref\n0 {0}\n", objects.Count + 1));
        sb.Append("0000000000 65535 f \n");
        foreach (var offset in offsets)
            sb.Append(offset.ToString("D10", CultureInfo.InvariantCulture)).Append(" 00000 n \n");

        sb.Append(F("trailer\n<< /Size {0} /Root 1 0 R /Info {1} 0 R >>\nstartxref\n{2}\n%%EOF\n",
            objects.Count + 1, infoId, xrefStart));

        return Encoding.Latin1.GetBytes(sb.ToString());
    }
}
