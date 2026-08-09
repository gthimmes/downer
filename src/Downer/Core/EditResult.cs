namespace Downer.Core;

/// <summary>Result of a pure text transform: the new document text plus the new selection.</summary>
public sealed record EditResult(string Text, int SelectionStart, int SelectionLength)
{
    public int CaretOffset => SelectionStart + SelectionLength;
}

public enum LinePrefixKind
{
    Bullet,
    Ordered,
    Task,
    Quote,
}
