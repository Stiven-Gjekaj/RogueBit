using RogueBit.Core;
using Xunit;

namespace RogueBit.Core.Tests;

public class TileTests
{
    [Theory]
    [InlineData(TileKind.Floor, true)]
    [InlineData(TileKind.StairsDown, true)]
    [InlineData(TileKind.Wall, false)]
    public void SaysWhichTilesCanBeWalkedOn(TileKind kind, bool expected)
        => Assert.Equal(expected, kind.IsWalkable());

    [Theory]
    [InlineData(TileKind.Floor, true)]
    [InlineData(TileKind.StairsDown, true)]
    [InlineData(TileKind.Wall, false)]
    public void SaysWhichTilesLightPassesThrough(TileKind kind, bool expected)
        => Assert.Equal(expected, kind.IsTransparent());

    [Fact]
    public void KeepsWalkabilityAndTransparencyInStep()
    {
        // A tile that can be stood on but blocks sight would need its own kind.
        // Until one exists the two properties must agree, and this test says so.
        foreach (TileKind kind in Enum.GetValues<TileKind>())
        {
            Assert.Equal(kind.IsWalkable(), kind.IsTransparent());
        }
    }
}
