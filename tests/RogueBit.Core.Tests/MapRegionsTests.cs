using RogueBit.Core;
using RogueBit.Core.Map;
using Xunit;

namespace RogueBit.Core.Tests;

public class MapRegionsTests
{
    [Fact]
    public void FindsOneRegionWhenEveryFloorConnects()
    {
        DungeonMap map = MapBuilder.From(
            "#####",
            "#...#",
            "#...#",
            "#####");

        Assert.Single(MapRegions.Find(map));
    }

    [Fact]
    public void FindsTwoRegionsWhenAWallSplitsTheFloor()
    {
        DungeonMap map = MapBuilder.From(
            "#####",
            "#.#.#",
            "#.#.#",
            "#####");

        List<List<Position>> regions = MapRegions.Find(map);

        Assert.Equal(2, regions.Count);
        Assert.All(regions, region => Assert.Equal(2, region.Count));
    }

    [Fact]
    public void DoesNotJoinTwoFloorsThatOnlyTouchAtACorner()
    {
        // A player moving in four directions cannot cross a corner, so these
        // are two regions and not one.
        DungeonMap map = MapBuilder.From(
            "####",
            "#.##",
            "##.#",
            "####");

        Assert.Equal(2, MapRegions.Find(map).Count);
    }

    [Fact]
    public void FindsNoRegionOnASolidMap()
    {
        DungeonMap map = MapBuilder.From(
            "###",
            "###");

        Assert.Empty(MapRegions.Find(map));
    }

    [Fact]
    public void ConnectAllLeavesExactlyOneRegion()
    {
        DungeonMap map = MapBuilder.From(
            "#########",
            "#.#...#.#",
            "#.#...#.#",
            "#.#####.#",
            "#########");

        Assert.Equal(3, MapRegions.Find(map).Count);

        MapRegions.ConnectAll(map);

        Assert.Single(MapRegions.Find(map));
    }

    [Fact]
    public void ConnectAllKeepsEveryCellThatWasAlreadyFloor()
    {
        DungeonMap map = MapBuilder.From(
            "#######",
            "#.#...#",
            "#.#...#",
            "#######");

        List<Position> before = [.. map.WalkableCells()];

        MapRegions.ConnectAll(map);

        // Connecting may add floor. It must never take any away.
        Assert.All(before, cell => Assert.True(map.IsWalkable(cell)));
    }

    [Fact]
    public void ConnectAllDoesNothingToAMapThatIsAlreadyOneRegion()
    {
        DungeonMap map = MapBuilder.From(
            "#####",
            "#...#",
            "#####");

        string[] before = MapBuilder.Render(map);

        MapRegions.ConnectAll(map);

        Assert.Equal(before, MapBuilder.Render(map));
    }

    [Fact]
    public void CarveCorridorJoinsItsTwoEnds()
    {
        DungeonMap map = MapBuilder.From(
            "#######",
            "#.....#",
            "#.....#",
            "#.....#",
            "#######");

        // Wall the middle off, then carve a corridor back through it.
        for (int y = 1; y <= 3; y++) map[new Position(3, y)] = TileKind.Wall;
        Assert.Equal(2, MapRegions.Find(map).Count);

        MapRegions.CarveCorridor(map, new Position(1, 1), new Position(5, 3));

        Assert.Single(MapRegions.Find(map));
    }
}
