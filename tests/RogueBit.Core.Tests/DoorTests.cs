using RogueBit.Core;
using RogueBit.Core.Entities;
using RogueBit.Core.Map;
using RogueBit.Core.Vision;
using Xunit;

namespace RogueBit.Core.Tests;

/// <summary>
/// The door is the one tile a player can walk through and cannot see through.
/// These tests are about that difference, and about a floor having doors on it
/// at all.
/// </summary>
public class DoorTests
{
    [Fact]
    public void APlayerCanWalkOntoADoor()
    {
        DungeonMap map = MapBuilder.From(
            "#####",
            "#.+.#",
            "#####");
        map.Entrance = new Position(1, 1);
        map.StairsDown = new Position(3, 1);
        map[map.StairsDown] = TileKind.StairsDown;

        Run run = Run.Resume(
            random: new SeededRandom(1),
            player: new Player(new Position(1, 1)),
            floors: [new Floor { Depth = 1, Map = map }],
            depth: 1,
            deepestDepth: 1,
            turns: 0,
            log: []);

        Assert.Equal(ActionResult.Took, run.Move(Directions.East));
        Assert.Equal(new Position(2, 1), run.Player.Position);
        Assert.Equal(TileKind.Door, run.Map[run.Player.Position]);
    }

    [Fact]
    public void SightStopsAtADoor()
    {
        DungeonMap map = MapBuilder.From(
            "#######",
            "#.+...#",
            "#######");
        Position origin = new(1, 1);

        FieldOfView.Compute(map, origin, radius: 9);

        Assert.True(map.IsVisible(new Position(2, 1)), "the door itself is seen");
        Assert.False(map.IsVisible(new Position(3, 1)), "the cell behind the door is not");
        Assert.False(map.IsVisible(new Position(4, 1)));
    }

    [Fact]
    public void SightWouldNotStopThereWithoutTheDoor()
    {
        // The same room with ordinary ground where the door was. Without this,
        // the test above would pass on a map that was dark for another reason.
        DungeonMap map = MapBuilder.From(
            "#######",
            "#.....#",
            "#######");

        FieldOfView.Compute(map, new Position(1, 1), radius: 9);

        Assert.True(map.IsVisible(new Position(3, 1)));
        Assert.True(map.IsVisible(new Position(4, 1)));
    }

    [Fact]
    public void StandingInADoorwayDoesNotBlindThePlayer()
    {
        // The origin tile is opaque, which must not stop the scan leaving it.
        DungeonMap map = MapBuilder.From(
            "#######",
            "#..+..#",
            "#######");

        FieldOfView.Compute(map, new Position(3, 1), radius: 9);

        Assert.True(map.IsVisible(new Position(2, 1)));
        Assert.True(map.IsVisible(new Position(4, 1)));
        Assert.True(map.IsVisible(new Position(1, 1)));
    }

    [Fact]
    public void EveryFloorOfRoomsGetsDoors()
    {
        BspDungeonGenerator generator = new();

        for (int seed = 0; seed < 40; seed++)
        {
            DungeonMap map = generator.Generate(78, 34, new SeededRandom(seed));

            Assert.Contains(map.WalkableCells(), c => map[c] == TileKind.Door);
        }
    }

    [Fact]
    public void ADoorNeverSitsOnTheEntranceOrTheStairs()
    {
        BspDungeonGenerator generator = new();

        for (int seed = 0; seed < 40; seed++)
        {
            DungeonMap map = generator.Generate(78, 34, new SeededRandom(seed));

            Assert.NotEqual(TileKind.Door, map[map.Entrance]);
            Assert.NotEqual(TileKind.Door, map[map.StairsDown]);
        }
    }

    [Fact]
    public void ADoorNeverStandsInsideARoom()
    {
        // A door in the middle of a room would wall off an open space from
        // itself. Doors belong in the throat of a corridor and nowhere else.
        BspDungeonGenerator generator = new();

        for (int seed = 0; seed < 40; seed++)
        {
            DungeonMap map = generator.Generate(78, 34, new SeededRandom(seed));

            foreach (Position cell in map.WalkableCells().Where(c => map[c] == TileKind.Door))
            {
                Assert.DoesNotContain(map.Rooms, room => room.Contains(cell));
            }
        }
    }

    [Fact]
    public void DoorsDoNotStrandAnyPartOfTheFloor()
    {
        // Doors are walkable, so they must not change the one connected region
        // the generators promise.
        BspDungeonGenerator generator = new();

        for (int seed = 0; seed < 40; seed++)
        {
            DungeonMap map = generator.Generate(78, 34, new SeededRandom(seed));

            Assert.Single(MapRegions.Find(map));
        }
    }
}
