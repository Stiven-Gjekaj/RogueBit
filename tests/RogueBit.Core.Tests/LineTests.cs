using RogueBit.Core;
using RogueBit.Core.Map;
using RogueBit.Core.Vision;
using Xunit;

namespace RogueBit.Core.Tests;

public class LineTests
{
    [Fact]
    public void IncludesBothEnds()
    {
        List<Position> cells = [.. Line.Between(new Position(0, 0), new Position(3, 0))];

        Assert.Equal(new Position(0, 0), cells[0]);
        Assert.Equal(new Position(3, 0), cells[^1]);
    }

    [Fact]
    public void EveryStepTouchesTheOneBeforeIt()
    {
        List<Position> cells = [.. Line.Between(new Position(0, 0), new Position(7, 4))];

        for (int i = 1; i < cells.Count; i++)
        {
            Assert.Equal(1, cells[i - 1].ChebyshevDistanceTo(cells[i]));
        }
    }

    [Fact]
    public void ALineToItselfIsOneCell()
    {
        Assert.Single(Line.Between(new Position(2, 2), new Position(2, 2)));
    }

    [Fact]
    public void RunsInEitherDirection()
    {
        List<Position> forward = [.. Line.Between(new Position(0, 0), new Position(5, 3))];
        List<Position> backward = [.. Line.Between(new Position(5, 3), new Position(0, 0))];

        Assert.Equal(forward.Count, backward.Count);
        Assert.Equal(forward[0], backward[^1]);
    }

    [Fact]
    public void SeesStraightDownAnOpenCorridor()
    {
        DungeonMap map = MapBuilder.From(
            "#######",
            "#.....#",
            "#######");

        Assert.True(Line.IsClear(map, new Position(1, 1), new Position(5, 1)));
    }

    [Fact]
    public void AWallBetweenTheEndsBlocksTheLine()
    {
        DungeonMap map = MapBuilder.From(
            "#######",
            "#..#..#",
            "#######");

        Assert.False(Line.IsClear(map, new Position(1, 1), new Position(5, 1)));
    }

    [Fact]
    public void ATargetStandingAgainstAWallCanStillBeHit()
    {
        // The ends are not tested, so a shot at something in a doorway lands.
        DungeonMap map = MapBuilder.From(
            "######",
            "#....#",
            "######");

        Assert.True(Line.IsClear(map, new Position(1, 1), new Position(4, 1)));
    }
}
