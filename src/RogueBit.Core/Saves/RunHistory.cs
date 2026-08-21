namespace RogueBit.Core.Saves;

using System.Text;
using System.Text.Json;

/// <summary>One finished run, written down.</summary>
/// <param name="Seed">The dungeon it was played on.</param>
/// <param name="Score">What it was worth.</param>
/// <param name="DeepestDepth">The deepest floor it reached.</param>
/// <param name="Turns">How long it lasted.</param>
/// <param name="Won">True when the deep warden died and the run was won.</param>
public sealed record FinishedRun(int Seed, int Score, int DeepestDepth, int Turns, bool Won);

/// <summary>
/// What the file of finished runs holds.
///
/// It carries a version for the same reason a save does. A file whose shape
/// this build does not know is left alone rather than read into new meanings,
/// and a player who upgrades keeps whatever was there instead of finding it
/// quietly rewritten.
/// </summary>
public sealed record RunRecord
{
    public const int CurrentVersion = 1;

    public int Version { get; init; } = CurrentVersion;

    public required IReadOnlyList<FinishedRun> Runs { get; init; }
}

/// <summary>
/// Every run this machine has finished, oldest first.
///
/// It replaces a file that held one integer: the best score across every seed
/// ever played. That was enough to print one number and nothing else. A run
/// that is worth replaying is worth comparing against, and comparing needs the
/// seed it was played on.
///
/// There is no clock in it. Entries are in the order they were added, which is
/// all that last time needs, and a timestamp would put the wall clock into
/// every test that reads one back.
///
/// Only a finished run is recorded. Leaving with Escape writes a save and the
/// run carries on later, so it has not finished and nothing is written for it.
/// </summary>
public sealed class RunHistory
{
    /// <summary>
    /// How many entries are kept. A player who has been at this for a year
    /// should not be waiting on a file that grows without end, and nothing
    /// here reads further back than the last run on a seed.
    /// </summary>
    public const int Capacity = 500;

    private static readonly JsonSerializerOptions Format = new()
    {
        WriteIndented = true,
    };

    private readonly string path;

    public RunHistory(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        this.path = path;
    }

    /// <summary>
    /// Every finished run, oldest first, or nothing when there is no file,
    /// when it is damaged, or when it was written by a version this one does
    /// not understand.
    /// </summary>
    public IReadOnlyList<FinishedRun> All()
    {
        try
        {
            if (!File.Exists(path)) return [];

            RunRecord? record = JsonSerializer.Deserialize<RunRecord>(File.ReadAllText(path, Encoding.UTF8), Format);

            if (record is null) return [];
            if (record.Version != RunRecord.CurrentVersion) return [];

            return record.Runs;
        }
        catch (Exception exception) when (exception is JsonException or IOException or UnauthorizedAccessException)
        {
            return [];
        }
    }

    /// <summary>
    /// Adds a finished run, and reports whether it beat everything before it
    /// on that seed.
    ///
    /// The file is written beside the target and then moved over it, so a
    /// crash midway leaves the previous record whole rather than half of a new
    /// one. The save has been written that way from the start. This one was
    /// not, and the record of a year of play is worth at least as much care as
    /// the run in progress.
    /// </summary>
    public bool Add(FinishedRun run)
    {
        ArgumentNullException.ThrowIfNull(run);

        bool best = run.Score > BestOn(run.Seed);
        List<FinishedRun> kept = [.. All(), run];

        if (kept.Count > Capacity) kept.RemoveRange(0, kept.Count - Capacity);

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);

            string temporary = path + ".writing";
            File.WriteAllText(
                temporary,
                JsonSerializer.Serialize(new RunRecord { Runs = kept }, Format),
                Encoding.UTF8);
            File.Move(temporary, path, overwrite: true);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // A record that will not be written is not worth ending a run for.
        }

        return best;
    }

    /// <summary>The best score on one seed, or zero when it has not been played.</summary>
    public int BestOn(int seed)
    {
        int best = 0;

        foreach (FinishedRun run in All())
        {
            if (run.Seed == seed && run.Score > best) best = run.Score;
        }

        return best;
    }

    /// <summary>The last run finished on one seed, or nothing when there is none.</summary>
    public FinishedRun? LastOn(int seed)
    {
        FinishedRun? last = null;

        foreach (FinishedRun run in All())
        {
            if (run.Seed == seed) last = run;
        }

        return last;
    }

    /// <summary>The best score on any seed, which is what the panel has always shown.</summary>
    public int Best()
    {
        int best = 0;

        foreach (FinishedRun run in All())
        {
            if (run.Score > best) best = run.Score;
        }

        return best;
    }
}
