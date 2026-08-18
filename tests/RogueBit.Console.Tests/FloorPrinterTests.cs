using RogueBit.Console;
using RogueBit.Core;
using Xunit;

namespace RogueBit.Console.Tests;

/// <summary>
/// Printing a floor without playing it. The point of the command is that a bad
/// map can be reported by seed and depth, so the same pair printing the same
/// floor is the whole promise.
/// </summary>
public class FloorPrinterTests
{
    [Fact]
    public void TheSameSeedAndDepthPrintTheSameFloor()
    {
        Assert.Equal(FloorPrinter.Render(4242, 3), FloorPrinter.Render(4242, 3));
    }

    [Fact]
    public void ADifferentSeedPrintsADifferentFloor()
    {
        Assert.NotEqual(FloorPrinter.Render(4242, 1), FloorPrinter.Render(4243, 1));
    }

    [Fact]
    public void ADifferentDepthPrintsADifferentFloor()
    {
        Assert.NotEqual(FloorPrinter.Render(4242, 1), FloorPrinter.Render(4242, 2));
    }

    [Fact]
    public void TheFirstFloorHasNoWayUpAndDeeperOnesDo()
    {
        Assert.DoesNotContain(FloorPrinter.Render(4242, 1), row => row.Contains('<', StringComparison.Ordinal));
        Assert.Contains(FloorPrinter.Render(4242, 2), row => row.Contains('<', StringComparison.Ordinal));
    }

    [Fact]
    public void EveryFloorButTheLastHasAWayDown()
    {
        for (int depth = 1; depth < GameRules.FinalDepth; depth++)
        {
            Assert.Contains(FloorPrinter.Render(4242, depth), row => row.Contains('>', StringComparison.Ordinal));
        }
    }

    [Fact]
    public void TheBottomFloorHasNoWayDown()
    {
        Assert.DoesNotContain(
            FloorPrinter.Render(4242, GameRules.FinalDepth),
            row => row.Contains('>', StringComparison.Ordinal));
    }

    [Fact]
    public void ThePrintedFloorIsTheSizeTheGamePlaysOn()
    {
        string[] rows = FloorPrinter.Render(4242, 1);

        Assert.Equal(Run.MapHeight, rows.Length);
        Assert.All(rows, row => Assert.Equal(Run.MapWidth, row.Length));
    }
}
