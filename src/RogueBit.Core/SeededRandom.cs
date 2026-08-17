namespace RogueBit.Core;

using System.Numerics;

/// <summary>
/// The one source of chance in a run.
///
/// Every generator, spawn table and wandering monster draws from here. A run is
/// therefore fully described by its seed, and the same seed replays the same
/// game.
///
/// This is PCG-XSH-RR, written out rather than taken from
/// <see cref="System.Random"/>, for two reasons that both matter to this game.
///
/// 1. <see cref="System.Random"/> does not promise the same sequence from the
///    same seed across runtime versions, and its algorithm has already changed
///    once. A saved seed that replays a different dungeon next year is not a
///    seed. This generator is written out here, so the promise is the code.
/// 2. Its state cannot be read out, so a run in progress could not be saved and
///    resumed without changing what the dice would do next. The two words below
///    are the whole state, and <see cref="Snapshot"/> hands them over.
///
/// <see cref="SeededRandomTests.TheSequenceForASeedIsPinned"/> holds the output
/// to fixed values. If that test ever fails, every saved run and every shared
/// seed has silently changed meaning.
/// </summary>
public sealed class SeededRandom
{
    private const ulong Multiplier = 6364136223846793005UL;

    private ulong state;
    private readonly ulong increment;

    public int Seed { get; }

    public SeededRandom(int seed)
        : this(seed, stream: 1)
    {
    }

    private SeededRandom(int seed, ulong stream)
    {
        Seed = seed;

        // The increment selects the stream and has to be odd.
        increment = (stream << 1) | 1UL;
        state = 0UL;

        Advance();
        state += (ulong)(long)seed;
        Advance();
    }

    private SeededRandom(int seed, ulong state, ulong increment)
    {
        Seed = seed;
        this.state = state;
        this.increment = increment;
    }

    /// <summary>A source seeded from the clock, for a run the player did not pin.</summary>
    public static SeededRandom FromClock() => new(Environment.TickCount);

    /// <summary>A new source on the same seed, which replays the run exactly.</summary>
    public SeededRandom Restart() => new(Seed);

    /// <summary>
    /// The whole state of this source. Saving these three numbers and handing
    /// them back to <see cref="Restore"/> resumes the run without changing what
    /// the dice do next.
    /// </summary>
    public (int Seed, ulong State, ulong Increment) Snapshot() => (Seed, state, increment);

    /// <summary>Rebuilds a source from a snapshot taken earlier.</summary>
    public static SeededRandom Restore(int seed, ulong state, ulong increment)
    {
        if ((increment & 1UL) == 0UL)
        {
            throw new ArgumentException("The increment of a PCG stream must be odd.", nameof(increment));
        }

        return new SeededRandom(seed, state, increment);
    }

    /// <summary>A number from 0 up to but not including <paramref name="exclusiveUpperBound"/>.</summary>
    public int Next(int exclusiveUpperBound)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(exclusiveUpperBound);
        return (int)NextUInt((uint)exclusiveUpperBound);
    }

    /// <summary>A number from <paramref name="inclusiveLower"/> up to but not including <paramref name="exclusiveUpper"/>.</summary>
    public int Next(int inclusiveLower, int exclusiveUpper)
    {
        if (exclusiveUpper <= inclusiveLower)
        {
            throw new ArgumentOutOfRangeException(nameof(exclusiveUpper), "The upper bound must be above the lower one.");
        }

        return inclusiveLower + (int)NextUInt((uint)(exclusiveUpper - inclusiveLower));
    }

    /// <summary>A number from <paramref name="inclusiveLower"/> to <paramref name="inclusiveUpper"/>.</summary>
    public int Between(int inclusiveLower, int inclusiveUpper)
    {
        if (inclusiveUpper < inclusiveLower)
        {
            throw new ArgumentOutOfRangeException(nameof(inclusiveUpper), "The upper bound must not be below the lower one.");
        }

        return inclusiveLower + (int)NextUInt((uint)(inclusiveUpper - inclusiveLower + 1));
    }

    /// <summary>A number from 0 up to but not including 1.</summary>
    public double NextDouble()
    {
        // Fifty three bits, which is every value a double can hold in that range.
        ulong high = NextUInt() >> 5;
        ulong low = NextUInt() >> 6;
        return ((high << 26) | low) * (1.0 / 9007199254740992.0);
    }

    /// <summary>True with the given probability, which must be between 0 and 1.</summary>
    public bool Chance(double probability) => NextDouble() < probability;

    /// <summary>One item, chosen with equal probability.</summary>
    public T Pick<T>(IReadOnlyList<T> items)
    {
        ArgumentNullException.ThrowIfNull(items);
        if (items.Count == 0) throw new ArgumentException("Cannot pick from an empty list.", nameof(items));

        return items[(int)NextUInt((uint)items.Count)];
    }

    /// <summary>Reorders a list in place.</summary>
    public void Shuffle<T>(IList<T> items)
    {
        ArgumentNullException.ThrowIfNull(items);

        for (int i = items.Count - 1; i > 0; i--)
        {
            int j = (int)NextUInt((uint)(i + 1));
            (items[i], items[j]) = (items[j], items[i]);
        }
    }

    /// <summary>One step of the generator, returning the next thirty two bits.</summary>
    private uint NextUInt()
    {
        ulong previous = state;
        Advance();

        // Xorshift the high bits down, then rotate by a count taken from the
        // top five bits. The rotation is what removes the weak low order bits a
        // plain linear congruential generator leaves behind.
        uint xorshifted = (uint)(((previous >> 18) ^ previous) >> 27);
        int rotation = (int)(previous >> 59);

        return BitOperations.RotateRight(xorshifted, rotation);
    }

    /// <summary>
    /// A number below <paramref name="bound"/>, with no modulo bias. The low
    /// values would come up slightly more often if the remainder were simply
    /// taken, so the range that would cause that is rejected and redrawn.
    /// </summary>
    private uint NextUInt(uint bound)
    {
        uint threshold = (uint)((0x1_0000_0000UL - bound) % bound);

        while (true)
        {
            uint drawn = NextUInt();
            if (drawn >= threshold) return drawn % bound;
        }
    }

    private void Advance() => state = unchecked((state * Multiplier) + increment);
}
