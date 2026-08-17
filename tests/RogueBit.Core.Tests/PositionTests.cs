using RogueBit.Core;
using Xunit;

namespace RogueBit.Core.Tests;

public class PositionTests
{
    [Fact]
    public void AddsAndSubtracts()
    {
        Position start = new(3, 4);

        Assert.Equal(new Position(3, 3), start + Directions.North);
        Assert.Equal(new Position(4, 4), start + Directions.East);
        Assert.Equal(new Position(1, 2), start - new Position(2, 2));
    }

    [Fact]
    public void MeasuresManhattanDistanceAlongTheAxes()
    {
        Assert.Equal(7, new Position(0, 0).ManhattanDistanceTo(new Position(3, 4)));
    }

    [Fact]
    public void MeasuresChebyshevDistanceByTheLongerAxis()
    {
        Assert.Equal(4, new Position(0, 0).ChebyshevDistanceTo(new Position(3, 4)));
    }

    [Fact]
    public void OffersFourCardinalStepsAndEightInTotal()
    {
        Assert.Equal(4, Directions.Cardinal.Count);
        Assert.Equal(8, Directions.All.Count);
        Assert.Equal(8, Directions.All.Distinct().Count());
    }

    [Fact]
    public void NoDirectionStandsStill()
    {
        Assert.DoesNotContain(new Position(0, 0), Directions.All);
    }
}
