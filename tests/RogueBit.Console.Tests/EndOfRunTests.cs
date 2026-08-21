using RogueBit.Console;
using RogueBit.Core;
using Xunit;

namespace RogueBit.Console.Tests;

/// <summary>
/// What the player is told when a run is over. It is the last thing they read,
/// and until now nothing could read it without opening a window.
/// </summary>
public class EndOfRunTests
{
    /// <summary>Walks down to a floor, taking the stairs rather than hunting for them.</summary>
    private static Run DeepRun(int to, int seed = 4242)
    {
        Run run = new(seed);

        while (run.Depth < to)
        {
            run.Player.Position = run.Map.StairsDown;
            Assert.Equal(ActionResult.Took, run.Descend());
        }

        return run;
    }

    [Fact]
    public void ADeathIsCalledADeath()
    {
        Run run = new(1);

        Assert.Equal("You died.", EndOfRun.Lines(run, best: 0)[0]);
    }

    [Fact]
    public void ThePanelNamesTheDeepestFloorReachedAndNotTheOneUnderfoot()
    {
        Run run = DeepRun(to: 4);

        run.Player.Position = run.Map.Entrance;
        Assert.Equal(ActionResult.Took, run.Ascend());

        Assert.Equal(3, run.Depth);
        Assert.Equal(4, run.DeepestDepth);
        Assert.Contains("Floor 4,", EndOfRun.Lines(run, best: 0)[1]);
    }

    [Fact]
    public void ThePanelAgreesWithWhatTheScoreWasPaidOn()
    {
        // The two numbers are read off the same panel by the same person. A
        // score that counts four floors beside a line that says three is a
        // player being told their points are wrong.
        Run run = DeepRun(to: 5);

        run.Player.Position = run.Map.Entrance;
        Assert.Equal(ActionResult.Took, run.Ascend());

        string line = EndOfRun.Lines(run, best: 0)[1];

        Assert.Contains($"Floor {run.DeepestDepth},", line);
        Assert.Contains($"{run.Score} points", line);
    }

    [Fact]
    public void TheBestOnAnySeedIsWhateverItWasToldTheBestIs()
    {
        Run run = new(1);

        Assert.Contains("Best on any seed 1234.", EndOfRun.Lines(run, best: 1234));
    }

    [Fact]
    public void ASeedNobodyHasFinishedIsSaidToBeANewOne()
    {
        Run run = new(31337);

        Assert.Contains("Seed 31337, played through for the first time.", EndOfRun.Lines(run, best: 900));
    }

    [Fact]
    public void BeatingTheSeedSaysSoAndSaysWhatWasBeaten()
    {
        Run run = new(8);
        run.Player.TakeCoins(50);

        Assert.Contains(
            $"Your best on seed 8, beating 10.",
            EndOfRun.Lines(run, best: 900, bestOnThisSeed: 10));
    }

    [Fact]
    public void FallingShortOfTheSeedSaysWhatToBeat()
    {
        Run run = new(8);

        Assert.Contains(
            "Your best on seed 8 is 400.",
            EndOfRun.Lines(run, best: 900, bestOnThisSeed: 400));
    }

    [Fact]
    public void MatchingTheSeedIsNotBeatingIt()
    {
        Run run = new(8);
        run.Player.TakeCoins(40);

        Assert.Equal(40, run.Score);
        Assert.Contains(
            "Your best on seed 8 is 40.",
            EndOfRun.Lines(run, best: 900, bestOnThisSeed: 40));
    }
}
