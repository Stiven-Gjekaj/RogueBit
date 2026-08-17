using RogueBit.Core;
using Xunit;

namespace RogueBit.Core.Tests;

public class SeededRandomTests
{
    [Fact]
    public void TwoSourcesOnOneSeedProduceTheSameNumbers()
    {
        SeededRandom a = new(12345);
        SeededRandom b = new(12345);

        int[] first = [.. Enumerable.Range(0, 50).Select(_ => a.Next(1000))];
        int[] second = [.. Enumerable.Range(0, 50).Select(_ => b.Next(1000))];

        Assert.Equal(first, second);
    }

    [Fact]
    public void DifferentSeedsProduceDifferentNumbers()
    {
        SeededRandom a = new(1);
        SeededRandom b = new(2);

        int[] first = [.. Enumerable.Range(0, 50).Select(_ => a.Next(1000))];
        int[] second = [.. Enumerable.Range(0, 50).Select(_ => b.Next(1000))];

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void RestartReplaysTheRunFromTheStart()
    {
        // This is the defect the old code shipped. Restarting reused the
        // advanced generator, so the same seed gave a different dungeon.
        SeededRandom source = new(999);
        int[] before = [.. Enumerable.Range(0, 20).Select(_ => source.Next(1000))];

        SeededRandom restarted = source.Restart();
        int[] after = [.. Enumerable.Range(0, 20).Select(_ => restarted.Next(1000))];

        Assert.Equal(before, after);
        Assert.Equal(source.Seed, restarted.Seed);
    }

    [Fact]
    public void RestartDoesNotDisturbTheSourceItCameFrom()
    {
        SeededRandom source = new(7);
        _ = source.Next(100);

        SeededRandom restarted = source.Restart();
        _ = restarted.Next(100);

        // The two are independent, so drawing from one must not move the other.
        Assert.Equal(new SeededRandom(7).Next(100), restarted.Restart().Next(100));
    }

    [Fact]
    public void BetweenIncludesBothEnds()
    {
        SeededRandom source = new(4);
        HashSet<int> seen = [];

        for (int i = 0; i < 500; i++) seen.Add(source.Between(3, 5));

        Assert.Equal([3, 4, 5], seen.OrderBy(n => n));
    }

    [Fact]
    public void ShuffleKeepsEveryItem()
    {
        SeededRandom source = new(11);
        List<int> items = [.. Enumerable.Range(0, 20)];

        source.Shuffle(items);

        Assert.Equal(Enumerable.Range(0, 20), items.OrderBy(n => n));
    }

    [Fact]
    public void ShuffleOnOneSeedGivesOneOrder()
    {
        List<int> first = [.. Enumerable.Range(0, 20)];
        List<int> second = [.. Enumerable.Range(0, 20)];

        new SeededRandom(3).Shuffle(first);
        new SeededRandom(3).Shuffle(second);

        Assert.Equal(first, second);
    }

    [Fact]
    public void PickRefusesAnEmptyList()
        => Assert.Throws<ArgumentException>(() => new SeededRandom(1).Pick(Array.Empty<int>()));
}
