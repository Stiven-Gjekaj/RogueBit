using RogueBit.Core;
using RogueBit.Core.Entities;
using RogueBit.Core.Map;
using Xunit;

namespace RogueBit.Core.Tests;

/// <summary>
/// The one way a run ends other than dying.
///
/// Each test builds its own floor, because reaching depth ten by playing takes
/// thousands of turns and depends on the bot surviving them.
/// </summary>
public class VictoryTests
{
    private static Run FinalFloor(int depth = GameRules.FinalDepth, bool withBoss = true, int playerHealth = 20)
    {
        DungeonMap map = MapBuilder.From(
            "#######",
            "#.....#",
            "#.....#",
            "#######");
        map.StairsDown = new Position(5, 2);
        map.Entrance = new Position(1, 1);

        Player player = new(new Position(1, 1));
        if (playerHealth < player.MaxHealth) player.TakeDamage(player.MaxHealth - playerHealth);

        List<Monster> monsters = [];
        if (withBoss)
        {
            Monster boss = SpawnTable.Boss(new Position(2, 1), depth);
            boss.BasePower = 0;   // The tests are about the ending, not the fight.
            boss.BaseDefence = 0;
            boss.TakeDamage(boss.MaxHealth - 1);
            monsters.Add(boss);
        }

        Floor floor = new() { Depth = depth, Map = map };
        floor.Monsters.AddRange(monsters);

        return Run.Resume(
            random: new SeededRandom(1),
            player: player,
            floors: [floor],
            depth: depth,
            deepestDepth: depth,
            turns: 0,
            log: []);
    }

    [Fact]
    public void KillingTheWardenOnTheBottomFloorWinsTheRun()
    {
        Run run = FinalFloor();

        Assert.False(run.HasWon);

        run.Move(Directions.East);

        Assert.True(run.HasWon);
        Assert.True(run.IsOver);
    }

    [Fact]
    public void AWinAddsItsBonusToTheScore()
    {
        Run run = FinalFloor();

        run.Move(Directions.East);

        int coins = run.Player.Coins;
        int depthBonus = (run.Depth - 1) * 10;

        Assert.Equal(coins + depthBonus + GameRules.VictoryBonus, run.Score);
    }

    [Fact]
    public void TheSameRunScoresLessWithoutTheWin()
    {
        Run won = FinalFloor();
        won.Move(Directions.East);

        // The same floor, with the warden still standing.
        Run unfinished = FinalFloor();

        Assert.True(won.Score > unfinished.Score);
    }

    [Fact]
    public void KillingABossOnAnEarlierFloorDoesNotWin()
    {
        Run run = FinalFloor(depth: 5);

        run.Move(Directions.East);

        Assert.False(run.HasWon);
        Assert.False(run.IsOver);
    }

    [Fact]
    public void TheBottomFloorHasNoStairsToTake()
    {
        Run run = FinalFloor(withBoss: false);
        run.Player.Position = run.Map.StairsDown;

        Assert.Equal(ActionResult.Refused, run.Descend());
        Assert.Equal(GameRules.FinalDepth, run.Depth);
    }

    [Fact]
    public void AWonRunTakesNoFurtherActions()
    {
        Run run = FinalFloor();
        run.Move(Directions.East);

        int turns = run.Turns;

        Assert.Equal(ActionResult.Refused, run.Move(Directions.East));
        Assert.Equal(ActionResult.Refused, run.Wait());
        Assert.Equal(turns, run.Turns);
    }

    [Fact]
    public void NothingCanKillThePlayerOnTheTurnTheRunIsWon()
    {
        // The winning blow ends the run. A monster taking one more turn here
        // would be able to kill somebody who has already finished.
        Run run = FinalFloor(playerHealth: 1);

        Monster bystander = SpawnTable.Goblin(new Position(1, 2), GameRules.FinalDepth);
        bystander.BasePower = 100;

        Floor floor = new() { Depth = GameRules.FinalDepth, Map = run.Map };
        floor.Monsters.AddRange([.. run.Monsters, bystander]);

        Run staged = Run.Resume(
            random: new SeededRandom(1),
            player: run.Player,
            floors: [floor],
            depth: GameRules.FinalDepth,
            deepestDepth: GameRules.FinalDepth,
            turns: 0,
            log: []);

        staged.Move(Directions.East);

        Assert.True(staged.HasWon);
        Assert.True(staged.Player.IsAlive);
    }

    [Fact]
    public void TheDeepWardenIsNamedApartFromTheOthers()
    {
        Assert.Equal("the warden", SpawnTable.Boss(new Position(0, 0), 5).Name);
        Assert.Equal("the deep warden", SpawnTable.Boss(new Position(0, 0), GameRules.FinalDepth).Name);
    }

    [Fact]
    public void TheDeepWardenIsTougherThanTheOneHalfwayDown()
    {
        Monster halfway = SpawnTable.Boss(new Position(0, 0), 5);
        Monster deep = SpawnTable.Boss(new Position(0, 0), GameRules.FinalDepth);

        Assert.True(deep.MaxHealth > halfway.MaxHealth);
        Assert.True(deep.CoinReward > halfway.CoinReward);
    }

    [Fact]
    public void EveryFifthFloorAndTheBottomOneHoldABoss()
    {
        Assert.True(SpawnTable.HasBoss(5));
        Assert.True(SpawnTable.HasBoss(GameRules.FinalDepth));
        Assert.False(SpawnTable.HasBoss(4));
    }
}
