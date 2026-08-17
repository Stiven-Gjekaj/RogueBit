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

    [Fact]
    public void TheSequenceForASeedIsPinned()
    {
        // This is the promise the whole game rests on. A saved run, a shared
        // seed and every screenshot in the documentation all assume that seed
        // 12345 means one particular dungeon and always will.
        //
        // If this test fails, the generator has changed and every one of those
        // has quietly come to mean something else. That is a breaking change to
        // the game, not a detail, and it needs a new major version rather than a
        // new set of expected numbers here.
        SeededRandom source = new(12345);

        int[] drawn = [.. Enumerable.Range(0, 10).Select(_ => source.Next(1000))];

        Assert.Equal([124, 104, 3, 176, 654, 690, 840, 798, 115, 834], drawn);
    }

    [Fact]
    public void ASnapshotResumesExactlyWhereItWasTaken()
    {
        SeededRandom source = new(555);
        for (int i = 0; i < 37; i++) _ = source.Next(100);

        (int seed, ulong state, ulong increment) = source.Snapshot();
        SeededRandom resumed = SeededRandom.Restore(seed, state, increment);

        int[] fromOriginal = [.. Enumerable.Range(0, 20).Select(_ => source.Next(1000))];
        int[] fromResumed = [.. Enumerable.Range(0, 20).Select(_ => resumed.Next(1000))];

        Assert.Equal(fromOriginal, fromResumed);
        Assert.Equal(555, resumed.Seed);
    }

    [Fact]
    public void ASnapshotTakenLaterIsNotTheSameAsOneTakenEarlier()
    {
        SeededRandom source = new(555);
        (_, ulong first, _) = source.Snapshot();
        _ = source.Next(100);
        (_, ulong second, _) = source.Snapshot();

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void RestoreRefusesAnEvenIncrement()
    {
        // An even increment gives the generator a much shorter period, so a
        // damaged save must be refused rather than played.
        Assert.Throws<ArgumentException>(() => SeededRandom.Restore(1, 42UL, 2UL));
    }

    [Fact]
    public void EveryValueInARangeComesUpAboutEquallyOften()
    {
        // Taking the remainder of a random number biases the low values. This
        // checks the rejection sampling that avoids it.
        SeededRandom source = new(31337);
        int[] counts = new int[6];

        for (int i = 0; i < 60_000; i++) counts[source.Next(6)]++;

        Assert.All(counts, count => Assert.InRange(count, 9_400, 10_600));
    }

    [Fact]
    public void ADoubleStaysInsideItsRange()
    {
        SeededRandom source = new(8);

        for (int i = 0; i < 5_000; i++)
        {
            double value = source.NextDouble();
            Assert.InRange(value, 0.0, 0.9999999999);
        }
    }

    [Fact]
    public void RefusesABoundThatIsNotPositive()
    {
        SeededRandom source = new(1);

        Assert.Throws<ArgumentOutOfRangeException>(() => source.Next(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => source.Next(-3));
        Assert.Throws<ArgumentOutOfRangeException>(() => source.Next(5, 5));
        Assert.Throws<ArgumentOutOfRangeException>(() => source.Between(5, 4));
    }
}
