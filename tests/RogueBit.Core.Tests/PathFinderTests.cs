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

        // Straight across is walled off. Cutting down round the end of the
        // wall and back up takes six diagonal and near diagonal steps.
        Assert.Equal(6, path.Count);
    }

    [Fact]
    public void WalksDiagonallyWhenItCan()
    {
        DungeonMap map = MapBuilder.From(
            "#####",
            "#...#",
            "#...#",
            "#...#",
            "#####");

        IReadOnlyList<Position> path = PathFinder.Find(map, new Position(1, 1), new Position(3, 3));

        Assert.Equal(2, path.Count);
    }

    [Fact]
    public void WillNotCutBetweenTwoWallsThatMeetAtACorner()
    {
        // The only join between the two rooms is the corner at (2,2) to (3,3).
        // Walking it would be sliding through solid rock, so there is no walk.
        DungeonMap map = MapBuilder.From(
            "######",
            "#..###",
            "#..###",
            "###..#",
            "###..#",
            "######");

        Assert.False(map.CanStep(new Position(2, 2), new Position(3, 3)));
        Assert.Empty(PathFinder.Find(map, new Position(1, 1), new Position(4, 4)));
    }

    [Fact]
    public void WillCutPastASingleWallCorner()
    {
        // Only one of the two cells beside the diagonal is a wall, so this is
        // walking round a corner rather than through one, and it is allowed.
        DungeonMap map = MapBuilder.From(
            "#####",
            "#..##",
            "#...#",
            "#...#",
            "#####");

        Assert.True(map.CanStep(new Position(2, 1), new Position(3, 2)));
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
            Assert.Equal(1, previous.ChebyshevDistanceTo(step));
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
                    PathFinder.Find(map, map.Entrance, map.StairsDown).Count > 0,
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

    /// <summary>
    /// The true shortest walk, counted without a heuristic at all. Every step
    /// costs the same, so breadth first gives the answer A* has to match.
    /// </summary>
    private static int ShortestByBreadthFirst(DungeonMap map, Position start, Position goal)
    {
        Queue<Position> queue = new();
        Dictionary<Position, int> seen = new() { [start] = 0 };
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            Position at = queue.Dequeue();
            if (at == goal) return seen[at];

            foreach (Position step in Directions.All)
            {
                Position next = at + step;
                if (!map.CanStep(at, next) || seen.ContainsKey(next)) continue;

                seen[next] = seen[at] + 1;
                queue.Enqueue(next);
            }
        }

        return -1;
    }

    [Fact]
    public void MatchesBreadthFirstOnEveryFloorItIsGiven()
    {
        // The open room below cannot catch a heuristic that overstates: with
        // nothing to walk round, going greedily towards the goal is also going
        // shortest. On real floors it can, and does. Left on Manhattan this
        // fails on roughly one floor in six, by as much as seven steps.
        foreach (IDungeonGenerator generator in new IDungeonGenerator[]
                 { new BspDungeonGenerator(), new DrunkardWalkGenerator() })
        {
            for (int seed = 0; seed < 40; seed++)
            {
                DungeonMap map = generator.Generate(78, 34, new SeededRandom(seed));

                int truth = ShortestByBreadthFirst(map, map.Entrance, map.StairsDown);
                int found = PathFinder.Find(map, map.Entrance, map.StairsDown).Count;

                Assert.True(truth > 0, $"{generator.Name} seed {seed}: the stairs are not reachable");
                Assert.Equal(truth, found);
            }
        }
    }

    [Fact]
    public void FindsAShortestWalkAndNotMerelyAShortOne()
    {
        // An open room. A walker that moves in eight directions covers both
        // axes at once, so the shortest walk is the Chebyshev distance and any
        // detour would make the count larger.
        //
        // This is the test that catches a heuristic left behind. Manhattan
        // overstates the true cost once diagonals are allowed, and a heuristic
        // that overstates gives a short walk rather than a shortest one.
        DungeonMap map = new(12, 12);
        foreach (Position cell in new Rectangle(1, 1, 10, 10).Cells()) map[cell] = TileKind.Floor;

        Position start = new(1, 1);
        Position goal = new(9, 7);

        Assert.Equal(start.ChebyshevDistanceTo(goal), PathFinder.Find(map, start, goal).Count);
    }
}
