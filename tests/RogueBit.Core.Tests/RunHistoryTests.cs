using System.Text;
using RogueBit.Core.Saves;
using Xunit;

namespace RogueBit.Core.Tests;

/// <summary>
/// The record of finished runs.
///
/// Each test writes into a directory of its own and removes it afterwards, so
/// the suite never reads or writes the player's real record. The history is
/// built fresh on every use rather than held, because a record that only works
/// while one object is alive is not a record.
/// </summary>
public sealed class RunHistoryTests : IDisposable
{
    private readonly string directory =
        Path.Combine(Path.GetTempPath(), "roguebit-tests", Guid.NewGuid().ToString("n"));

    public void Dispose()
    {
        if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
    }

    private string Path_ => Path.Combine(directory, "runs.json");

    private RunHistory History => new(Path_);

    private static FinishedRun Run(int seed, int score, int depth = 3, int turns = 200, bool won = false) =>
        new(seed, score, depth, turns, won);

    [Fact]
    public void AMachineThatHasPlayedNothingRemembersNothing()
    {
        Assert.Empty(History.All());
        Assert.Equal(0, History.Best());
        Assert.Equal(0, History.BestOn(7));
        Assert.Null(History.LastOn(7));
    }

    [Fact]
    public void AFinishedRunComesBackAfterTheGameHasBeenClosed()
    {
        History.Add(Run(seed: 7, score: 120, depth: 4, turns: 310, won: true));

        FinishedRun kept = Assert.Single(History.All());

        Assert.Equal(7, kept.Seed);
        Assert.Equal(120, kept.Score);
        Assert.Equal(4, kept.DeepestDepth);
        Assert.Equal(310, kept.Turns);
        Assert.True(kept.Won);
    }

    [Fact]
    public void RunsComeBackInTheOrderTheyWerePlayed()
    {
        History.Add(Run(seed: 1, score: 10));
        History.Add(Run(seed: 2, score: 20));
        History.Add(Run(seed: 3, score: 30));

        Assert.Equal([1, 2, 3], History.All().Select(run => run.Seed));
    }

    [Fact]
    public void TheBestOnASeedIgnoresEveryOtherSeed()
    {
        History.Add(Run(seed: 1, score: 500));
        History.Add(Run(seed: 2, score: 30));
        History.Add(Run(seed: 2, score: 90));

        Assert.Equal(90, History.BestOn(2));
        Assert.Equal(500, History.Best());
    }

    [Fact]
    public void TheLastOnASeedIsTheLastOneAndNotTheBestOne()
    {
        // Last time is what happened last time, however badly it went. A
        // player who wants their best told to them has the other number.
        History.Add(Run(seed: 4, score: 300));
        History.Add(Run(seed: 4, score: 12));

        Assert.Equal(12, History.LastOn(4)?.Score);
        Assert.Equal(300, History.BestOn(4));
    }

    [Fact]
    public void AddingSaysWhetherTheRunBeatTheSeed()
    {
        Assert.True(History.Add(Run(seed: 5, score: 100)));
        Assert.False(History.Add(Run(seed: 5, score: 40)));
        Assert.False(History.Add(Run(seed: 5, score: 100)));
        Assert.True(History.Add(Run(seed: 5, score: 101)));
    }

    [Fact]
    public void BeatingOneSeedIsNotBeatingAnother()
    {
        History.Add(Run(seed: 6, score: 900));

        Assert.True(History.Add(Run(seed: 7, score: 5)));
    }

    [Fact]
    public void TheOldestRunsAreDroppedRatherThanKeptForEver()
    {
        for (int i = 0; i < RunHistory.Capacity + 20; i++) History.Add(Run(seed: 1, score: i));

        IReadOnlyList<FinishedRun> kept = History.All();

        Assert.Equal(RunHistory.Capacity, kept.Count);
        Assert.Equal(20, kept[0].Score);
        Assert.Equal(RunHistory.Capacity + 19, kept[^1].Score);
    }

    [Fact]
    public void ARecordFromAVersionThisOneDoesNotKnowIsLeftAlone()
    {
        Directory.CreateDirectory(directory);
        File.WriteAllText(
            Path_,
            """{"Version":99,"Runs":[{"Seed":1,"Score":5,"DeepestDepth":1,"Turns":1,"Won":false}]}""",
            Encoding.UTF8);

        Assert.Empty(History.All());
        Assert.Equal(0, History.Best());
    }

    [Fact]
    public void ADamagedRecordIsNotAllowedToEndTheRun()
    {
        Directory.CreateDirectory(directory);
        File.WriteAllText(Path_, "this is not json at all", Encoding.UTF8);

        Assert.Empty(History.All());
        Assert.True(History.Add(Run(seed: 1, score: 10)));
        Assert.Single(History.All());
    }

    [Fact]
    public void NothingIsLeftBehindWhenARunIsWritten()
    {
        History.Add(Run(seed: 1, score: 10));

        // The file is written beside the target and moved over it. A leftover
        // half written file is how a record is lost rather than kept.
        Assert.Equal(["runs.json"], Directory.GetFiles(directory).Select(Path.GetFileName));
    }
}
