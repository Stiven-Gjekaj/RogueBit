using RogueBit.Core;
using Xunit;

namespace RogueBit.Core.Tests;

public class GridTests
{
    [Fact]
    public void FillsEveryCellOnConstruction()
    {
        Grid<TileKind> grid = new(4, 3, TileKind.Wall);

        for (int y = 0; y < grid.Height; y++)
        {
            for (int x = 0; x < grid.Width; x++)
            {
                Assert.Equal(TileKind.Wall, grid[x, y]);
            }
        }
    }

    [Fact]
    public void ReadsBackWhatItWrote()
    {
        Grid<TileKind> grid = new(4, 3, TileKind.Wall);

        grid[2, 1] = TileKind.Floor;

        Assert.Equal(TileKind.Floor, grid[2, 1]);
        Assert.Equal(TileKind.Wall, grid[1, 1]);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(0, -1)]
    [InlineData(4, 0)]
    [InlineData(0, 3)]
    public void RefusesACellOffTheGrid(int x, int y)
    {
        Grid<TileKind> grid = new(4, 3, TileKind.Wall);

        Assert.False(grid.Contains(x, y));
        Assert.Throws<ArgumentOutOfRangeException>(() => grid[x, y]);
    }

    [Fact]
    public void ReturnsTheFallbackOffTheGrid()
    {
        Grid<TileKind> grid = new(4, 3, TileKind.Floor);

        Assert.Equal(TileKind.Wall, grid.GetOrDefault(-1, 0, TileKind.Wall));
        Assert.Equal(TileKind.Floor, grid.GetOrDefault(0, 0, TileKind.Wall));
    }

    [Theory]
    [InlineData(0, 3)]
    [InlineData(3, 0)]
    public void RefusesADimensionThatIsNotPositive(int width, int height)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Grid<TileKind>(width, height, TileKind.Wall));
    }
}
