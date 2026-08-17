using RogueBit.Core;
using RogueBit.Core.Map;
using Xunit;

namespace RogueBit.Core.Tests;

public class RectangleTests
{
    [Fact]
    public void ReportsItsOwnEdges()
    {
        Rectangle r = new(2, 3, 4, 5);

        Assert.Equal(2, r.Left);
        Assert.Equal(3, r.Top);
        Assert.Equal(5, r.Right);
        Assert.Equal(7, r.Bottom);
        Assert.Equal(20, r.Area);
    }

    [Fact]
    public void ListsExactlyAsManyCellsAsItsArea()
    {
        Rectangle r = new(2, 3, 4, 5);

        Assert.Equal(r.Area, r.Cells().Count());
        Assert.All(r.Cells(), cell => Assert.True(r.Contains(cell)));
    }

    [Fact]
    public void TouchingRectanglesOverlapAndSeparatedOnesDoNot()
    {
        Rectangle a = new(0, 0, 3, 3);

        Assert.True(a.Overlaps(new Rectangle(2, 2, 3, 3)));
        Assert.False(a.Overlaps(new Rectangle(3, 0, 3, 3)));
        Assert.False(a.Overlaps(new Rectangle(0, 3, 3, 3)));
    }

    [Fact]
    public void ExpandGrowsOnEverySide()
    {
        Rectangle r = new Rectangle(5, 5, 2, 2).Expand(1);

        Assert.Equal(new Rectangle(4, 4, 4, 4), r);
    }

    [Fact]
    public void CentreLiesInside()
    {
        Rectangle r = new(10, 20, 7, 3);

        Assert.True(r.Contains(r.Centre));
    }
}
