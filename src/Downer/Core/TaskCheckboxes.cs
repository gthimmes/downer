namespace Downer.Core;

/// <summary>A click on a task checkbox: the offset of its state character and the state to write.</summary>
public sealed record CheckboxHit(int StateCharOffset, char NewState);

/// <summary>
/// Pure hit-testing for click-to-toggle task checkboxes. An offset anywhere on the
/// rendered checkbox region (list marker through "[x]") maps to a single-character
/// flip of the state char, keeping the document edit minimal and undo-friendly.
/// </summary>
public static class TaskCheckboxes
{
    public static CheckboxHit? HitTest(string text, int offset, FenceLineState fenceState = FenceLineState.Outside)
    {
        if (text.Length == 0 || offset < 0 || offset > text.Length)
            return null;
        if (fenceState != FenceLineState.Outside)
            return null;

        var lineStart = TextLines.LineStart(text, offset);
        var lineEnd = TextLines.LineEnd(text, offset, lineStart);
        var line = text[lineStart..lineEnd];

        var info = MarkdownSpanParser.ParseLine(line);
        var task = info.Spans.FirstOrDefault(s => s.Kind == SpanKind.TaskCheckbox);
        if (task is null)
            return null;

        var marker = info.Spans.FirstOrDefault(s => s.Kind == SpanKind.ListMarker);
        var hitStart = marker?.Start ?? task.Start;

        var relative = offset - lineStart;
        if (relative < hitStart || relative >= task.End)
            return null;

        var stateCharOffset = lineStart + task.Start + 1;
        var current = text[stateCharOffset];
        return new CheckboxHit(stateCharOffset, current is 'x' or 'X' ? ' ' : 'x');
    }
}
