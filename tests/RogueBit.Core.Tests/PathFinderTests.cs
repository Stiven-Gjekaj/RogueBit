using RogueBit.Core;
using RogueBit.Core.Map;
using RogueBit.Core.Pathing;
using Xunit;

namespace RogueBit.Core.Tests;

public class PathFinderTests
{
    [Fact]
    public void WalksStraightDownAnEmptyCorridor()
    {
        DungeonMap map = MapBuilder.From(
            "#######",
            "#.....#",
            "#######");

        IReadOnlyList<Position> path = PathFinder.Find(map, new Position(1, 1), new Position(5, 1));

        Assert.Equal(4, path.Count);
        Assert.Equal(new Position(5, 1), path[^1]);
    }

    [Fact]
    public void DoesNotIncludeTheCellItStartsOn()
    {
        DungeonMap map = MapBuilder.From(
            "#####",
            "#...#",
            "#####");

        IReadOnlyList<Position> path = PathFinder.Find(map, new Position(1, 1), new Position(3, 1));

        Assert.DoesNotContain(new Position(1, 1), path);
    }

    [Fact]
    public void ReturnsNothingWhenItIsAlreadyThere()
    {
        DungeonMap map = MapBuilder.From("###", "#.#", "###");

        Assert.Empty(PathFinder.Find(map, new Position(1, 1), new Position(1, 1)));
    }

    [Fact]
    public void GoesRoundABarrierRatherThanThroughIt()
    {
        //  A wall across the middle with one gap at the bottom.
        DungeonMap map = MapBuilder.From(
            "#########",
            "#...#...#",
            "#...#...#",
            "#...#...#",
            "#.......#",
            "#########");

        IReadOnlyList<Position> path = PathFinder.Find(map, new Position(1, 1), new Position(7, 1));

        Assert.NotEmpty(path);
        Assert.All(path, cell => Assert.True(map.IsWalkable(cell), $"{cell} is a wall"));

        // Straight across would be six steps. Round the barrier is three down,
        // six across and three back up.
        Assert.Equal(12, path.Count);
    }

    [Fact]
    public void ReturnsNothingWhenTheGoalIsWalledOff()
    {
        DungeonMap map = MapBuilder.From(
            "#######",
            "#..#..#",
            "#..#..#",
            "#######");

        Assert.Empty(PathFinder.Find(map, new Position(1, 1), new Position(5, 1)));
    }

    [Fact]
    public void ReturnsNothingWhenTheGoalIsAWall()
    {
        DungeonMap map = MapBuilder.From(
            "#####",
            "#...#",
            "#####");

        Assert.Empty(PathFinder.Find(map, new Position(1, 1), new Position(2, 0)));
    }

    [Fact]
    public void EveryStepIsNextToTheOneBeforeIt()
    {
        DungeonMap map = new BspDungeonGenerator().Generate(50, 25, new SeededRandom(3));

        IReadOnlyList<Position> path = PathFinder.Find(map, map.Entrance, map.StairsDown);

        Assert.NotEmpty(path);

        Position previous = map.Entrance;
        foreach (Position step in path)
        {
            Assert.Equal(1, previous.ManhattanDistanceTo(step));
            previous = step;
        }
    }

    [Fact]
    public void FindsTheStairsFromTheEntranceOnEveryFloorItGenerates()
    {
        foreach (IDungeonGenerator generator in new IDungeonGenerator[]
                 { new BspDungeonGenerator(), new DrunkardWalkGenerator() })
        {
            for (int seed = 0; seed < 15; seed++)
            {
                DungeonMap map = generator.Generate(50, 25, new SeededRandom(seed));

                Assert.True(
                    PathFinder.Find(map, map.Entrance, map.StairsDown).Count > 0 || map.Entrance == map.StairsDown,
                    $"{generator.Name} seed {seed}: the stairs cannot be reached from the entrance");
            }
        }
    }

    [Fact]
    public void StepsRoundABlockedCell()
    {
        DungeonMap map = MapBuilder.From(
            "#####",
            "#...#",
            "#...#",
            "#####");
        Position blocker = new(2, 1);

        IReadOnlyList<Position> path = PathFinder.Find(
            map, new Position(1, 1), new Position(3, 1), cell => cell == blocker);

        Assert.NotEmpty(path);
        Assert.DoesNotContain(blocker, path);
    }

    [Fact]
    public void StillReachesAGoalThatSomethingIsStandingOn()
    {
        // An enemy must be able to path onto the player to attack, even though
        // the player blocks the cell.
        DungeonMap map = MapBuilder.From(
            "#####",
            "#...#",
            "#####");
        Position player = new(3, 1);

        IReadOnlyList<Position> path = PathFinder.Find(
            map, new Position(1, 1), player, cell => cell == player);

        Assert.Equal(player, path[^1]);
    }

    [Fact]
    public void ReturnsNothingWhenBlockersSealTheOnlyWayThrough()
    {
        DungeonMap map = MapBuilder.From(
            "#####",
            "#...#",
            "#####");
        Position blocker = new(2, 1);

        Assert.Empty(PathFinder.Find(map, new Position(1, 1), new Position(3, 1), cell => cell == blocker));
    }

    [Fact]
    public void NextStepGivesTheFirstCellOfTheWalk()
    {
        DungeonMap map = MapBuilder.From(
            "#####",
            "#...#",
            "#####");

        Assert.Equal(new Position(2, 1), PathFinder.NextStep(map, new Position(1, 1), new Position(3, 1)));
    }

    [Fact]
    public void NextStepGivesNothingWhenThereIsNoWalk()
    {
        DungeonMap map = MapBuilder.From(
            "######",
            "#.##.#",
            "######");

        Assert.Null(PathFinder.NextStep(map, new Position(1, 1), new Position(4, 1)));
    }

    [Fact]
    public void FindsAShortestWalkAndNotMerelyAShortOne()
    {
        // An open room. The shortest walk is the Manhattan distance, and any
        // detour would make the count larger.
        DungeonMap map = new(12, 12);
        foreach (Position cell in new Rectangle(1, 1, 10, 10).Cells()) map[cell] = TileKind.Floor;

        Position start = new(1, 1);
        Position goal = new(9, 7);

        Assert.Equal(start.ManhattanDistanceTo(goal), PathFinder.Find(map, start, goal).Count);
    }
}
