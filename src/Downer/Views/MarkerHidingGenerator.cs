using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
using Downer.Core;

namespace Downer.Views;

/// <summary>
/// Typora-style syntax concealment for the formatted editing mode. On every line
/// except the one the caret is on, markers vanish: heading hashes, emphasis
/// markers, backticks, link plumbing, and fence delimiters collapse to
/// zero-width; bullets render as "•", task boxes as "☑"/"☐", quotes as "▏".
/// Table lines are left alone — their pipes are the grid.
/// The document text itself is never touched; only the rendering changes.
/// </summary>
public sealed class MarkerHidingGenerator : VisualLineElementGenerator
{
    private const string ZeroWidth = "​";

    public Func<int> RevealedLine { get; set; } = () => -1;
    public Func<int, FenceLineState> FenceStateFor { get; set; } = _ => FenceLineState.Outside;

    private readonly List<(int Start, int Length, string Replacement)> _replacements = new();
    private DocumentLine? _line;

    public override void StartGeneration(ITextRunConstructionContext context)
    {
        base.StartGeneration(context);
        _replacements.Clear();
        _line = context.VisualLine.FirstDocumentLine;

        if (_line.LineNumber == RevealedLine())
            return;

        var text = context.Document.GetText(_line);
        var fenceState = FenceStateFor(_line.LineNumber);

        if (fenceState == FenceLineState.Inside)
            return;

        if (fenceState == FenceLineState.Delimiter)
        {
            var fenceLength = CodeFences.FenceMarkerLength(text);
            if (fenceLength > 0)
                _replacements.Add((0, fenceLength, ZeroWidth));
            return;
        }

        var info = MarkdownSpanParser.ParseLine(text, fenceState);
        if (info.Spans.Any(s => s.Kind is SpanKind.TablePipe or SpanKind.TableSeparator))
            return;

        var hasTaskCheckbox = info.Spans.Any(s => s.Kind == SpanKind.TaskCheckbox);

        foreach (var span in info.Spans)
        {
            if (!span.IsMarker)
                continue;

            switch (span.Kind)
            {
                case SpanKind.HeadingMarker:
                case SpanKind.LinkPunctuation:
                case SpanKind.LinkUrl:
                case SpanKind.Bold:
                case SpanKind.Italic:
                case SpanKind.Strikethrough:
                case SpanKind.Code:
                    _replacements.Add((span.Start, span.Length, ZeroWidth));
                    break;

                case SpanKind.ListMarker:
                    if (char.IsDigit(text[span.Start]))
                        break; // ordered-list numbers stay visible
                    _replacements.Add((span.Start, span.Length, hasTaskCheckbox ? ZeroWidth : "• "));
                    break;

                case SpanKind.TaskCheckbox:
                    var box = text.Substring(span.Start, span.Length);
                    _replacements.Add((span.Start, span.Length, box.Contains('x') || box.Contains('X') ? "☑ " : "☐ "));
                    break;

                case SpanKind.QuoteMarker:
                    var depth = text.AsSpan(span.Start, span.Length).Count('>');
                    _replacements.Add((span.Start, span.Length, new string('▏', Math.Max(1, depth)) + " "));
                    break;
            }
        }

        _replacements.Sort((a, b) => a.Start.CompareTo(b.Start));
    }

    public override int GetFirstInterestedOffset(int startOffset)
    {
        if (_line is null)
            return -1;

        var relative = startOffset - _line.Offset;
        foreach (var replacement in _replacements)
        {
            if (replacement.Start >= relative)
                return _line.Offset + replacement.Start;
        }
        return -1;
    }

    public override VisualLineElement? ConstructElement(int offset)
    {
        if (_line is null)
            return null;

        var relative = offset - _line.Offset;
        foreach (var replacement in _replacements)
        {
            if (replacement.Start == relative)
                return new FormattedTextElement(replacement.Replacement, replacement.Length);
        }
        return null;
    }
}
