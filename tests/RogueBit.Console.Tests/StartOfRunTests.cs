using RogueBit.Console;
using RogueBit.Core;
using RogueBit.Core.Saves;
using Xunit;

namespace RogueBit.Console.Tests;

/// <summary>
/// The line at the top of a run about the last one on the same seed.
/// </summary>
public sealed class StartOfRunTests : IDisposable
{
    private readonly string directory =
        Path.Combine(Path.GetTempPath(), "roguebit-tests", Guid.NewGuid().ToString("n"));

    public void Dispose()
    {
        if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
    }

    private RunHistory History => new(Path.Combine(directory, "runs.json"));

    [Fact]
    public void ALostRunIsSaidToHaveGotAsFarAsItGot()
    {
        string line = StartOfRun.LastTime(new FinishedRun(9, 120, 4, 300, Won: false));

        Assert.Equal("Last time on this seed you got to floor 4, with 120 points.", line);
    }

    [Fact]
    public void AWonRunIsSaidToHaveReachedTheBottom()
    {
        string line = StartOfRun.LastTime(new FinishedRun(9, 640, 10, 900, Won: true));

        Assert.Equal("Last time on this seed you reached the bottom, with 640 points.", line);
    }

    [Fact]
    public void ASeedPlayedBeforeIsToldAboutInTheLog()
    {
        History.Add(new FinishedRun(9, 120, 4, 300, Won: false));

        Run run = new(9);
        StartOfRun.Announce(run, History);

        Assert.Contains(run.Log.Messages, message => message.Text.StartsWith("Last time on this seed"));
    }

    [Fact]
    public void ASeedNobodyHasFinishedIsNotMentionedAtAll()
    {
        History.Add(new FinishedRun(9, 120, 4, 300, Won: false));

        Run run = new(10);
        int before = run.Log.Count;
        StartOfRun.Announce(run, History);

        Assert.Equal(before, run.Log.Count);
    }

    [Fact]
    public void ItIsTheLastRunOnTheSeedAndNotTheBestOne()
    {
        History.Add(new FinishedRun(9, 900, 8, 800, Won: false));
        History.Add(new FinishedRun(9, 30, 2, 40, Won: false));

        Run run = new(9);
        StartOfRun.Announce(run, History);

        Assert.Contains(
            run.Log.Messages,
            message => message.Text == "Last time on this seed you got to floor 2, with 30 points.");
    }
}
