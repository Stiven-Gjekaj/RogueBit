namespace RogueBit.Core;

/// <summary>
/// The one source of chance in a run.
///
/// Every generator, spawn table and wandering enemy draws from here. A run is
/// therefore fully described by its seed, and the same seed replays the same
/// game. The old code kept one generator alive across restarts, so a restart on
/// the same seed produced a different dungeon. <see cref="Restart"/> exists to
/// make that mistake impossible: it returns a fresh source on the same seed.
/// </summary>
public sealed class SeededRandom
{
    private readonly Random random;

    public int Seed { get; }

    public SeededRandom(int seed)
    {
        Seed = seed;
        random = new Random(seed);
    }

    /// <summary>A source seeded from the clock, for a run the player did not pin.</summary>
    public static SeededRandom FromClock() => new(Environment.TickCount);

    /// <summary>A new source on the same seed, which replays the run exactly.</summary>
    public SeededRandom Restart() => new(Seed);

    /// <summary>A number from 0 up to but not including <paramref name="exclusiveUpperBound"/>.</summary>
    public int Next(int exclusiveUpperBound) => random.Next(exclusiveUpperBound);

    /// <summary>A number from <paramref name="inclusiveLower"/> up to but not including <paramref name="exclusiveUpper"/>.</summary>
    public int Next(int inclusiveLower, int exclusiveUpper) => random.Next(inclusiveLower, exclusiveUpper);

    /// <summary>A number from <paramref name="inclusiveLower"/> to <paramref name="inclusiveUpper"/>.</summary>
    public int Between(int inclusiveLower, int inclusiveUpper) => random.Next(inclusiveLower, inclusiveUpper + 1);

    /// <summary>True with the given probability, which must be between 0 and 1.</summary>
    public bool Chance(double probability) => random.NextDouble() < probability;

    /// <summary>One item, chosen with equal probability.</summary>
    public T Pick<T>(IReadOnlyList<T> items)
    {
        if (items.Count == 0) throw new ArgumentException("Cannot pick from an empty list.", nameof(items));
        return items[random.Next(items.Count)];
    }

    /// <summary>Reorders a list in place.</summary>
    public void Shuffle<T>(IList<T> items)
    {
        for (int i = items.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (items[i], items[j]) = (items[j], items[i]);
        }
    }
}
