using Avalonia.Collections;
using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
using Downer.Core;

namespace Downer.Views;

/// <summary>Dotted red underlines beneath misspelled prose words, in both editing modes.</summary>
public sealed class SpellCheckTransformer : DocumentColorizingTransformer
{
    public bool Enabled { get; set; }
    public Func<string, bool> IsCorrect { get; set; } = _ => true;
    public FenceLineState[] FenceStates { get; set; } = Array.Empty<FenceLineState>();
    public IBrush SquiggleBrush { get; set; } = new SolidColorBrush(Color.Parse("#E03131"));

    protected override void ColorizeLine(DocumentLine line)
    {
        if (!Enabled)
            return;

        var text = CurrentContext.Document.GetText(line);
        if (text.Length == 0)
            return;

        var index = line.LineNumber - 1;
        var fenceState = index < FenceStates.Length ? FenceStates[index] : FenceLineState.Outside;

        foreach (var word in SpellWords.Extract(text, fenceState))
        {
            if (IsCorrect(word.Word))
                continue;

            ChangeLinePart(line.Offset + word.Start, line.Offset + word.Start + word.Length, e =>
            {
                var decorations = new TextDecorationCollection
                {
                    new TextDecoration
                    {
                        Location = TextDecorationLocation.Underline,
                        Stroke = SquiggleBrush,
                        StrokeThickness = 2,
                        StrokeThicknessUnit = TextDecorationUnit.Pixel,
                        StrokeDashArray = new AvaloniaList<double> { 1, 2 },
                    },
                };

                // Keep any decoration another transformer already applied (strikethrough).
                if (e.TextRunProperties.TextDecorations is { } existing)
                {
                    foreach (var decoration in existing)
                        decorations.Add(decoration);
                }

                e.TextRunProperties.SetTextDecorations(decorations);
            });
        }
    }
}
