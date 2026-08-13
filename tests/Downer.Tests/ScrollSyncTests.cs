using Downer.Core;

namespace Downer.Tests;

public class ScrollSyncTests
{
    [Fact]
    public void Maps_proportionally()
    {
        Assert.Equal(50, ScrollSync.MapOffset(100, 200, 100));
        Assert.Equal(0, ScrollSync.MapOffset(0, 200, 100));
        Assert.Equal(100, ScrollSync.MapOffset(200, 200, 100));
    }

    [Fact]
    public void Overscroll_clamps_to_the_target_end()
    {
        Assert.Equal(100, ScrollSync.MapOffset(500, 200, 100));
    }

    [Fact]
    public void Negative_offset_clamps_to_zero()
    {
        Assert.Equal(0, ScrollSync.MapOffset(-25, 200, 100));
    }

    [Fact]
    public void Unscrollable_source_returns_null()
    {
        Assert.Null(ScrollSync.MapOffset(10, 0, 100));
        Assert.Null(ScrollSync.MapOffset(10, -5, 100));
    }

    [Fact]
    public void Unscrollable_target_maps_to_zero()
    {
        Assert.Equal(0, ScrollSync.MapOffset(100, 200, 0));
        Assert.Equal(0, ScrollSync.MapOffset(100, 200, -3));
    }
}
