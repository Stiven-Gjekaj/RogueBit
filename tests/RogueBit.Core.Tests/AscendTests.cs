using RogueBit.Core;
using RogueBit.Core.Entities;
using RogueBit.Core.Map;
using Xunit;

namespace RogueBit.Core.Tests;

/// <summary>
/// Going back up, and the thing that makes it worth doing: the floor above is
/// the floor that was left, not a fresh one grown on the same seed.
/// </summary>
public class AscendTests
{
    [Fact]
    public void ClimbingIsRefusedOnTheFirstFloor()
    {
        Run run = new(seed: 3);

        Assert.Equal(ActionResult.Refused, run.Ascend());
        Assert.Equal(1, run.Depth);
    }

    [Fact]
    public void ClimbingIsRefusedAwayFromTheStairs()
    {
        Run run = new(seed: 3);
        Descend(run);

        // Somewhere on the floor that is not the cell the player arrived on.
        run.Player.Position = run.Map.StairsDown;

        Assert.Equal(ActionResult.Refused, run.Ascend());
        Assert.Equal(2, run.Depth);
    }

    [Fact]
    public void ClimbingTheStairsGoesUpAFloor()
    {
        Run run = new(seed: 3);
        Descend(run);

        Assert.Equal(ActionResult.Took, run.Ascend());
        Assert.Equal(1, run.Depth);
    }

    [Fact]
    public void ComingBackUpLandsThePlayerOnTheStairsDown()
    {
        Run run = new(seed: 3);
        Descend(run);
        run.Ascend();

        Assert.Equal(run.Map.StairsDown, run.Player.Position);
        Assert.Equal(TileKind.StairsDown, run.Map[run.Player.Position]);
    }

    [Fact]
    public void TheGroundComeBackToIsTheGroundLeftBehind()
    {
        Run run = new(seed: 3);
        string[] before = MapBuilder.Render(run.Map);

        Descend(run);
        run.Ascend();

        Assert.Equal(before, MapBuilder.Render(run.Map));
    }

    [Fact]
    public void WhatWasLyingOnAFloorIsStillLyingThere()
    {
        Run run = new(seed: 3);
        List<Position> before = [.. run.Items.Select(i => i.Position)];

        Descend(run);
        run.Ascend();

        Assert.Equal(before, run.Items.Select(i => i.Position));
    }

    [Fact]
    public void TheMonstersLeftBehindAreTheSameMonsters()
    {
        Run run = new(seed: 3);
        List<(string Name, Position Where)> before = [.. run.Monsters.Select(m => (m.Name, m.Position))];

        Descend(run);
        run.Ascend();

        Assert.Equal(before, run.Monsters.Select(m => (m.Name, m.Position)));
    }

    [Fact]
    public void WhatThePlayerExploredIsStillRemembered()
    {
        Run run = new(seed: 3);
        List<Position> before = [.. run.Map.WalkableCells().Where(run.Map.IsExplored)];

        Assert.NotEmpty(before);

        Descend(run);
        run.Ascend();

        // Every cell remembered before is still remembered. Not the same set:
        // the player comes back up on the stairs down rather than where it
        // started, so it lights ground it had not reached on the way through.
        Assert.All(before, cell => Assert.True(run.Map.IsExplored(cell)));
    }

    [Fact]
    public void GoingBackToAFloorDrawsNothingFromTheDice()
    {
        Run run = new(seed: 3);

        // Building floor two draws. Everything after this is a floor that has
        // been built already, and a floor taken out of store must cost nothing,
        // or a run that walks up and down would drift away from the same run
        // that walked straight down.
        Descend(run);
        (int Seed, ulong State, ulong Increment) afterBuilding = run.Random.Snapshot();

        run.Ascend();
        Descend(run);
        run.Ascend();

        Assert.Equal(afterBuilding, run.Random.Snapshot());
    }

    [Fact]
    public void GoingDownAgainReturnsToTheFloorAlreadyBuilt()
    {
        Run run = new(seed: 3);
        Descend(run);
        string[] second = MapBuilder.Render(run.Map);

        run.Ascend();
        Descend(run);

        Assert.Equal(2, run.Depth);
        Assert.Equal(second, MapBuilder.Render(run.Map));
    }

    [Fact]
    public void TheScoreDoesNotFallWhenThePlayerTurnsBack()
    {
        Run run = new(seed: 3);
        Descend(run);
        int deep = run.Score;

        run.Ascend();

        Assert.Equal(1, run.Depth);
        Assert.Equal(2, run.DeepestDepth);
        Assert.Equal(deep, run.Score);
    }

    private static void Descend(Run run)
    {
        run.Player.Position = run.Map.StairsDown;
        Assert.Equal(ActionResult.Took, run.Descend());
    }
}
