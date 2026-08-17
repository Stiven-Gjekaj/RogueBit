using RogueBit.Core;
using RogueBit.Core.Entities;
using RogueBit.Core.Items;
using RogueBit.Core.Map;
using Xunit;

namespace RogueBit.Core.Tests;

/// <summary>
/// What a turn reports for a frontend to draw.
///
/// Every test here builds its own arena through <see cref="Run.Resume"/> rather
/// than generating a floor and hoping something useful is standing nearby. An
/// earlier version of this file did the latter and had to give up part way
/// through several tests, which made them pass without asserting anything.
/// </summary>
public class TurnEventTests
{
    /// <summary>A five by three room with the player at the left end.</summary>
    private static Run Arena(
        IEnumerable<Monster>? monsters = null,
        IEnumerable<Item>? items = null,
        Position? playerAt = null,
        int playerDamage = 0)
    {
        DungeonMap map = MapBuilder.From(
            "########",
            "#......#",
            "#......#",
            "########");
        map.StairsDown = new Position(6, 2);
        map[map.StairsDown] = TileKind.StairsDown;
        map.Entrance = new Position(1, 1);

        Player player = new(playerAt ?? new Position(1, 1));
        if (playerDamage > 0) player.TakeDamage(playerDamage);

        return Run.Resume(
            random: new SeededRandom(1),
            map: map,
            player: player,
            monsters: monsters ?? [],
            items: items ?? [],
            depth: 1,
            turns: 0,
            log: []);
    }

    private static Monster Goblin(Position at, int health = 100, int power = 0) =>
        new(at, health, power, defence: 0)
        {
            Glyph = 'g',
            Name = "a goblin",
            Behaviour = MonsterBehaviour.Chaser,
            AggroRadius = 0, // Never chases, so it cannot wander into the way.
            CoinReward = 1,
        };

    [Fact]
    public void AttackingRecordsAHitWhereTheTargetStands()
    {
        Position where = new(2, 1);
        Run run = Arena(monsters: [Goblin(where)]);

        run.Move(Directions.East);

        TurnEvent hit = Assert.Single(run.LastTurnEvents, e => e.Kind == TurnEventKind.Hit);
        Assert.Equal(where, hit.Where);
        Assert.True(hit.Magnitude > 0);
        Assert.False(hit.AgainstPlayer);
    }

    [Fact]
    public void KillingSomethingRecordsADeathWhereItStood()
    {
        Position where = new(2, 1);
        Run run = Arena(monsters: [Goblin(where, health: 1)]);

        run.Move(Directions.East);

        TurnEvent death = Assert.Single(run.LastTurnEvents, e => e.Kind == TurnEventKind.Death);
        Assert.Equal(where, death.Where);
        Assert.False(death.AgainstPlayer);
    }

    [Fact]
    public void ABlowTurnedAsideIsRecordedAsBlockedRatherThanAsAHit()
    {
        Monster armoured = new(new Position(2, 1), 100, power: 0, defence: 99)
        {
            Glyph = 'g',
            Name = "a goblin",
            Behaviour = MonsterBehaviour.Chaser,
            AggroRadius = 0,
            CoinReward = 1,
        };
        Run run = Arena(monsters: [armoured]);

        run.Move(Directions.East);

        Assert.Contains(run.LastTurnEvents, e => e.Kind == TurnEventKind.Blocked);
        Assert.DoesNotContain(run.LastTurnEvents, e => e.Kind == TurnEventKind.Hit);
    }

    [Fact]
    public void AHitOnThePlayerIsMarkedAgainstThePlayer()
    {
        // A goblin that notices the player and hits hard.
        Monster attacker = new(new Position(2, 1), 100, power: 5, defence: 0)
        {
            Glyph = 'g',
            Name = "a goblin",
            Behaviour = MonsterBehaviour.Chaser,
            AggroRadius = 12,
            CoinReward = 1,
        };
        Run run = Arena(monsters: [attacker], playerAt: new Position(1, 1));

        run.Wait();

        TurnEvent onPlayer = Assert.Single(run.LastTurnEvents, e => e.AgainstPlayer && e.Kind == TurnEventKind.Hit);
        Assert.Equal(run.Player.Position, onPlayer.Where);
        Assert.True(onPlayer.Magnitude > 0);
    }

    [Fact]
    public void ThePlayerDyingIsRecordedAsADeathAgainstThePlayer()
    {
        Monster killer = new(new Position(2, 1), 100, power: 500, defence: 0)
        {
            Glyph = 'g',
            Name = "a goblin",
            Behaviour = MonsterBehaviour.Chaser,
            AggroRadius = 12,
            CoinReward = 1,
        };
        Run run = Arena(monsters: [killer]);

        run.Wait();

        Assert.True(run.IsOver);
        Assert.Contains(run.LastTurnEvents, e => e.AgainstPlayer && e.Kind == TurnEventKind.Death);
    }

    [Fact]
    public void AnArcherRecordsBothTheShotAndWhereItLanded()
    {
        Position archerAt = new(6, 1);
        Monster archer = new(archerAt, 100, power: 3, defence: 0)
        {
            Glyph = 'a',
            Name = "an archer",
            Behaviour = MonsterBehaviour.Archer,
            AggroRadius = 12,
            Range = 8,
            CoinReward = 1,
        };
        Run run = Arena(monsters: [archer]);

        run.Wait();

        TurnEvent shot = Assert.Single(run.LastTurnEvents, e => e.Kind == TurnEventKind.Shot);
        Assert.Equal(archerAt, shot.Where);

        TurnEvent landed = Assert.Single(run.LastTurnEvents, e => e.Kind == TurnEventKind.Hit && e.AgainstPlayer);
        Assert.Equal(run.Player.Position, landed.Where);
    }

    [Fact]
    public void DrinkingAPotionRecordsHealingWorthShowing()
    {
        Run run = Arena(playerDamage: 10);
        Item potion = Item.Potion(run.Player.Position);
        run.Inventory.TryAdd(potion);

        run.Use(potion);

        TurnEvent healing = Assert.Single(run.LastTurnEvents, e => e.Kind == TurnEventKind.Heal);
        Assert.Equal(run.Player.Position, healing.Where);
        Assert.True(healing.Magnitude > 0);
    }

    [Fact]
    public void DrinkingAtFullHealthRecordsNothingToShow()
    {
        Run run = Arena();
        Item potion = Item.Potion(run.Player.Position);
        run.Inventory.TryAdd(potion);

        run.Use(potion);

        Assert.DoesNotContain(run.LastTurnEvents, e => e.Kind == TurnEventKind.Heal);
    }

    [Fact]
    public void PickingSomethingUpRecordsItWhereThePlayerStands()
    {
        Position at = new(1, 1);
        Run run = Arena(items: [Item.Potion(at)], playerAt: at);

        run.PickUp();

        TurnEvent pickup = Assert.Single(run.LastTurnEvents, e => e.Kind == TurnEventKind.Pickup);
        Assert.Equal(at, pickup.Where);
    }

    [Fact]
    public void GoingDownRecordsIt()
    {
        Run run = Arena();
        run.Player.Position = run.Map.StairsDown;

        run.Descend();

        Assert.Contains(run.LastTurnEvents, e => e.Kind == TurnEventKind.Descend);
    }

    [Fact]
    public void EventsDescribeTheLatestTurnAndNotAnOlderOne()
    {
        Run run = Arena(monsters: [Goblin(new Position(2, 1), health: 1)]);

        run.Move(Directions.East);
        Assert.NotEmpty(run.LastTurnEvents);

        // Nothing is left to fight, so the next turn has nothing to report.
        run.Wait();

        Assert.Empty(run.LastTurnEvents);
    }

    [Fact]
    public void ARefusedMoveLeavesTheListAlone()
    {
        Run run = Arena(monsters: [Goblin(new Position(2, 1), health: 1)]);
        run.Move(Directions.East);

        int before = run.LastTurnEvents.Count;
        Assert.True(before > 0);

        // North of the player is a wall, so this move is refused.
        Assert.Equal(ActionResult.Refused, run.Move(Directions.North));

        Assert.Equal(before, run.LastTurnEvents.Count);
    }

    [Fact]
    public void EveryEventLandsOnACellOfTheMap()
    {
        Run run = Arena(monsters: [Goblin(new Position(2, 1))]);

        run.Move(Directions.East);

        Assert.All(run.LastTurnEvents, e => Assert.True(run.Map.Contains(e.Where), $"{e.Where} is off the map"));
    }
}
