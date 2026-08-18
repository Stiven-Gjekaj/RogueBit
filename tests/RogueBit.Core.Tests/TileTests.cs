using RogueBit.Core;
using Xunit;

namespace RogueBit.Core.Tests;

public class TileTests
{
    [Theory]
    [InlineData(TileKind.Floor, true)]
    [InlineData(TileKind.StairsDown, true)]
    [InlineData(TileKind.StairsUp, true)]
    [InlineData(TileKind.Door, true)]
    [InlineData(TileKind.Wall, false)]
    public void SaysWhichTilesCanBeWalkedOn(TileKind kind, bool expected)
        => Assert.Equal(expected, kind.IsWalkable());

    [Theory]
    [InlineData(TileKind.Floor, true)]
    [InlineData(TileKind.StairsDown, true)]
    [InlineData(TileKind.StairsUp, true)]
    [InlineData(TileKind.Door, false)]
    [InlineData(TileKind.Wall, false)]
    public void SaysWhichTilesLightPassesThrough(TileKind kind, bool expected)
        => Assert.Equal(expected, kind.IsTransparent());

    [Fact]
    public void TheDoorIsTheOnlyTileWhereTheTwoDisagree()
    {
        // Every other tile is open to both or closed to both. The door is the
        // one that can be stood on and cannot be seen through, and a second
        // such tile should have to say so here rather than arrive quietly.
        foreach (TileKind kind in Enum.GetValues<TileKind>())
        {
            if (kind == TileKind.Door) continue;

            Assert.Equal(kind.IsWalkable(), kind.IsTransparent());
        }

        Assert.True(TileKind.Door.IsWalkable());
        Assert.False(TileKind.Door.IsTransparent());
    }
}
