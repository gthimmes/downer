namespace Downer.Core;

/// <summary>Proportional scroll mapping between two panes of different heights.</summary>
public static class ScrollSync
{
    /// <summary>
    /// Maps an offset within [0, sourceScrollable] onto the same fraction of
    /// [0, targetScrollable]. Returns null when the source cannot scroll (nothing
    /// meaningful to mirror) so callers can skip the sync entirely.
    /// </summary>
    public static double? MapOffset(double sourceOffset, double sourceScrollable, double targetScrollable)
    {
        if (sourceScrollable <= 0)
            return null;

        var fraction = Math.Clamp(sourceOffset / sourceScrollable, 0, 1);
        return fraction * Math.Max(0, targetScrollable);
    }
}
