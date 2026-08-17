using RogueBit.Core;
using RogueBit.Core.Map;
using RogueBit.Core.Vision;
using Xunit;

namespace RogueBit.Core.Tests;

public class FieldOfViewTests
{
    /// <summary>Draws what the viewer can see, so a failure is readable.</summary>
    private static string[] RenderVisible(DungeonMap map, Position origin)
    {
        string[] rows = new string[map.Height];

        for (int y = 0; y < map.Height; y++)
        {
            char[] row = new char[map.Width];
            for (int x = 0; x < map.Width; x++)
            {
                Position p = new(x, y);
                row[x] = p == origin ? '@'
                    : !map.IsVisible(p) ? ' '
                    : map.IsWalkable(p) ? '.' : '#';
            }

            rows[y] = new string(row);
        }

        return rows;
    }

    [Fact]
    public void TheViewerAlwaysSeesItsOwnCell()
    {
        DungeonMap map = MapBuilder.From(
            "###",
            "#.#",
            "###");
        Position origin = new(1, 1);

        FieldOfView.Compute(map, origin, 5);

        Assert.True(map.IsVisible(origin));
    }

    [Fact]
    public void SeesTheWholeOfAnOpenRoomWithinRange()
    {
        DungeonMap map = MapBuilder.From(
            "#######",
            "#.....#",
            "#.....#",
            "#.....#",
            "#######");
        Position origin = new(3, 2);

        FieldOfView.Compute(map, origin, 8);

        foreach (Position cell in map.WalkableCells())
        {
            Assert.True(map.IsVisible(cell), $"{cell} should be lit:\n{string.Join('\n', RenderVisible(map, origin))}");
        }
    }

    [Fact]
    public void AWallCastsAShadowBehindItself()
    {
        //          x: 0123456
        DungeonMap map = MapBuilder.From(
            "#########",
            "#.......#",
            "#.......#",
            "#...#...#",
            "#.......#",
            "#########");
        Position origin = new(4, 1);

        FieldOfView.Compute(map, origin, 10);

        // The pillar itself is lit, the cell directly behind it is not.
        Assert.True(map.IsVisible(new Position(4, 3)));
        Assert.False(
            map.IsVisible(new Position(4, 4)),
            $"the cell behind the pillar should be dark:\n{string.Join('\n', RenderVisible(map, origin))}");
    }

    [Fact]
    public void SeesTheWallsOfASealedRoomButNothingBeyondThem()
    {
        DungeonMap map = MapBuilder.From(
            "#########",
            "#...#...#",
            "#...#...#",
            "#########");
        Position origin = new(2, 1);

        FieldOfView.Compute(map, origin, 10);

        Assert.True(map.IsVisible(new Position(4, 1)), "the dividing wall should be lit");
        Assert.False(map.IsVisible(new Position(5, 1)), "the room beyond the wall should be dark");
        Assert.False(map.IsVisible(new Position(6, 2)), "the room beyond the wall should be dark");
    }

    [Fact]
    public void RoundsTheFieldOffIntoADiscRatherThanASquare()
    {
        DungeonMap map = new(21, 21);
        foreach (Position cell in new Rectangle(1, 1, 19, 19).Cells()) map[cell] = TileKind.Floor;
        Position origin = new(10, 10);

        FieldOfView.Compute(map, origin, 5);

        // Straight out is inside the radius; the corner of that square is not.
        Assert.True(map.IsVisible(new Position(15, 10)));
        Assert.False(map.IsVisible(new Position(14, 14)));
    }

    [Fact]
    public void SeesNothingBeyondTheRadius()
    {
        DungeonMap map = new(31, 31);
        foreach (Position cell in new Rectangle(1, 1, 29, 29).Cells()) map[cell] = TileKind.Floor;
        Position origin = new(15, 15);
        const int radius = 6;

        FieldOfView.Compute(map, origin, radius);

        foreach (Position cell in map.WalkableCells())
        {
            if (!map.IsVisible(cell)) continue;

            int dx = cell.X - origin.X;
            int dy = cell.Y - origin.Y;
            Assert.True(
                (dx * dx) + (dy * dy) <= radius * radius,
                $"{cell} is lit but lies outside the radius");
        }
    }

    [Fact]
    public void ARadiusOfZeroShowsOnlyTheViewer()
    {
        DungeonMap map = MapBuilder.From(
            "#####",
            "#...#",
            "#...#",
            "#####");
        Position origin = new(2, 1);

        FieldOfView.Compute(map, origin, 0);

        Assert.True(map.IsVisible(origin));
        Assert.False(map.IsVisible(new Position(1, 1)));
        Assert.False(map.IsVisible(new Position(3, 1)));
    }

    [Fact]
    public void RememberedGroundStaysExploredAfterTheLightMovesOn()
    {
        DungeonMap map = MapBuilder.From(
            "##########",
            "#........#",
            "##########");
        Position first = new(1, 1);
        Position second = new(8, 1);

        FieldOfView.Compute(map, first, 2);
        Assert.True(map.IsVisible(new Position(2, 1)));

        FieldOfView.Compute(map, second, 2);

        Assert.False(map.IsVisible(new Position(2, 1)));
        Assert.True(map.IsExplored(new Position(2, 1)), "ground once seen must stay remembered");
    }

    [Fact]
    public void LooksTheSameFromEitherSideOfADoorway()
    {
        // A corridor through a wall. What one end can see of the other should
        // not depend on which end the viewer stands at.
        DungeonMap map = MapBuilder.From(
            "#######",
            "#..#..#",
            "#.....#",
            "#..#..#",
            "#######");

        FieldOfView.Compute(map, new Position(1, 2), 6);
        bool leftSeesRight = map.IsVisible(new Position(5, 2));

        FieldOfView.Compute(map, new Position(5, 2), 6);
        bool rightSeesLeft = map.IsVisible(new Position(1, 2));

        Assert.Equal(leftSeesRight, rightSeesLeft);
    }

    [Fact]
    public void RefusesANegativeRadius()
    {
        DungeonMap map = MapBuilder.From("###", "#.#", "###");

        Assert.Throws<ArgumentOutOfRangeException>(() => FieldOfView.Compute(map, new Position(1, 1), -1));
    }
}
