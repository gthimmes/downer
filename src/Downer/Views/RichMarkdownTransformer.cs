using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
using Downer.Core;

namespace Downer.Views;

/// <summary>Brushes for in-place rendering; swapped wholesale on theme change.</summary>
public sealed record RichPalette(
    IBrush Dim,
    IBrush Accent,
    IBrush CodeForeground,
    IBrush CodeBackground,
    IBrush Link)
{
    public static RichPalette Light { get; } = new(
        Dim: new SolidColorBrush(Color.Parse("#ADB5BD")),
        Accent: new SolidColorBrush(Color.Parse("#7048E8")),
        CodeForeground: new SolidColorBrush(Color.Parse("#C2255C")),
        CodeBackground: new SolidColorBrush(Color.Parse("#F1F3F5")),
        Link: new SolidColorBrush(Color.Parse("#1971C2")));

    public static RichPalette Dark { get; } = new(
        Dim: new SolidColorBrush(Color.Parse("#5C6470")),
        Accent: new SolidColorBrush(Color.Parse("#9775FA")),
        CodeForeground: new SolidColorBrush(Color.Parse("#FAA2C1")),
        CodeBackground: new SolidColorBrush(Color.Parse("#2B3035")),
        Link: new SolidColorBrush(Color.Parse("#4DABF7")));
}

/// <summary>
/// Styles markdown in place on the editing surface: real heading sizes, true
/// bold/italic/strike, mono code runs, dimmed syntax markers. This is what makes
/// the default editing mode feel WYSIWYG while the text stays plain markdown.
/// </summary>
public sealed class RichMarkdownTransformer : DocumentColorizingTransformer
{
    public RichPalette Palette { get; set; } = RichPalette.Light;
    public FontFamily MonoFont { get; set; } = new("Cascadia Code,Consolas,Menlo,DejaVu Sans Mono,monospace");
    public double BaseFontSize { get; set; } = 15;
    public FenceLineState[] FenceStates { get; set; } = Array.Empty<FenceLineState>();

    protected override void ColorizeLine(DocumentLine line)
    {
        var lineText = CurrentContext.Document.GetText(line);
        if (lineText.Length == 0)
            return;

        var index = line.LineNumber - 1;
        var fenceState = index < FenceStates.Length ? FenceStates[index] : FenceLineState.Outside;
        var info = MarkdownSpanParser.ParseLine(lineText, fenceState);

        if (fenceState != FenceLineState.Outside)
        {
            ChangeLinePart(line.Offset, line.EndOffset, e =>
            {
                SetMono(e);
                e.TextRunProperties.SetForegroundBrush(
                    fenceState == FenceLineState.Delimiter ? Palette.Dim : Palette.CodeForeground);
                e.TextRunProperties.SetBackgroundBrush(Palette.CodeBackground);
            });
            return;
        }

        // Table lines render whole-line mono so padded columns align into a grid.
        if (info.Spans.Any(s => s.Kind is SpanKind.TablePipe or SpanKind.TableSeparator))
            ChangeLinePart(line.Offset, line.EndOffset, SetMono);

        if (info.HeadingLevel > 0)
        {
            var scale = info.HeadingLevel switch
            {
                1 => 1.7,
                2 => 1.45,
                3 => 1.25,
                4 => 1.1,
                _ => 1.0,
            };
            ChangeLinePart(line.Offset, line.EndOffset, e =>
            {
                e.TextRunProperties.SetFontRenderingEmSize(BaseFontSize * scale);
                SetWeight(e, FontWeight.Bold);
            });
        }

        foreach (var span in info.Spans)
        {
            var start = line.Offset + span.Start;
            var end = Math.Min(line.Offset + span.End, line.EndOffset);
            if (start >= end)
                continue;

            ChangeLinePart(start, end, e => ApplySpan(e, span));
        }
    }

    private void ApplySpan(VisualLineElement e, StyledSpan span)
    {
        if (span.IsMarker)
        {
            switch (span.Kind)
            {
                case SpanKind.ListMarker:
                case SpanKind.QuoteMarker:
                case SpanKind.TaskCheckbox:
                    e.TextRunProperties.SetForegroundBrush(Palette.Accent);
                    SetWeight(e, FontWeight.SemiBold);
                    break;
                case SpanKind.Code:
                    SetMono(e);
                    e.TextRunProperties.SetForegroundBrush(Palette.Dim);
                    break;
                default:
                    e.TextRunProperties.SetForegroundBrush(Palette.Dim);
                    break;
            }
            return;
        }

        switch (span.Kind)
        {
            case SpanKind.Bold:
                SetWeight(e, FontWeight.Bold);
                break;
            case SpanKind.Italic:
                SetStyle(e, FontStyle.Italic);
                break;
            case SpanKind.Strikethrough:
                e.TextRunProperties.SetTextDecorations(TextDecorations.Strikethrough);
                break;
            case SpanKind.Code:
                SetMono(e);
                e.TextRunProperties.SetForegroundBrush(Palette.CodeForeground);
                e.TextRunProperties.SetBackgroundBrush(Palette.CodeBackground);
                break;
            case SpanKind.LinkText:
                e.TextRunProperties.SetForegroundBrush(Palette.Link);
                e.TextRunProperties.SetTextDecorations(TextDecorations.Underline);
                break;
        }
    }

    private void SetMono(VisualLineElement e)
    {
        var tf = e.TextRunProperties.Typeface;
        e.TextRunProperties.SetTypeface(new Typeface(MonoFont, tf.Style, tf.Weight));
        e.TextRunProperties.SetFontRenderingEmSize(BaseFontSize * 0.92);
    }

    private static void SetWeight(VisualLineElement e, FontWeight weight)
    {
        var tf = e.TextRunProperties.Typeface;
        e.TextRunProperties.SetTypeface(new Typeface(tf.FontFamily, tf.Style, weight));
    }

    private static void SetStyle(VisualLineElement e, FontStyle style)
    {
        var tf = e.TextRunProperties.Typeface;
        e.TextRunProperties.SetTypeface(new Typeface(tf.FontFamily, style, tf.Weight));
    }
}
