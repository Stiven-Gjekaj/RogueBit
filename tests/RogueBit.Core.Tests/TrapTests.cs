using RogueBit.Core;
using RogueBit.Core.Entities;
using RogueBit.Core.Map;
using Xunit;

namespace RogueBit.Core.Tests;

/// <summary>
/// A trap is hidden until something stands on it, and then it is spent. Every
/// test here builds its own arena, so nothing depends on where a generator
/// happens to put one.
/// </summary>
public class TrapTests
{
    /// <summary>A corridor with an armed trap in the middle of it.</summary>
    private static Run Arena(IEnumerable<Monster>? monsters = null, int depth = 1, Position? playerAt = null)
    {
        DungeonMap map = MapBuilder.From(
            "########",
            "#.~....#",
            "#......#",
            "########");
        map.Entrance = new Position(1, 1);
        map.StairsDown = new Position(6, 2);
        map[map.StairsDown] = TileKind.StairsDown;

        Floor floor = new() { Depth = depth, Map = map };
        floor.Monsters.AddRange(monsters ?? []);

        return Run.Resume(
            random: new SeededRandom(1),
            player: new Player(playerAt ?? new Position(1, 1)),
            floors: [floor],
            depth: depth,
            deepestDepth: depth,
            turns: 0,
            log: []);
    }

    /// <summary>A goblin that chases and cannot hurt anybody when it arrives.</summary>
    private static Monster Chaser(Position at, int health = 100) =>
        new(at, health, power: 0, defence: 0)
        {
            Glyph = 'g',
            Name = "a goblin",
            Behaviour = MonsterBehaviour.Chaser,
            AggroRadius = 20,
            CoinReward = 7,
        };

    [Fact]
    public void SteppingOnATrapHurts()
    {
        Run run = Arena();
        int before = run.Player.Health;

        run.Move(Directions.East);

        Assert.Equal(before - SpawnTable.TrapDamage(1), run.Player.Health);
    }

    [Fact]
    public void SteppingOnATrapSaysSo()
    {
        Run run = Arena();

        run.Move(Directions.East);

        Assert.Contains(run.Log.Messages, m => m.Text.StartsWith("A trap goes off under you", StringComparison.Ordinal));
    }

    [Fact]
    public void ATrapReportsItselfWhereItWentOff()
    {
        Run run = Arena();

        run.Move(Directions.East);

        TurnEvent sprung = Assert.Single(run.LastTurnEvents, e => e.Kind == TurnEventKind.Trap);
        Assert.Equal(new Position(2, 1), sprung.Where);
        Assert.Equal(SpawnTable.TrapDamage(1), sprung.Magnitude);
        Assert.True(sprung.AgainstPlayer);
    }

    [Fact]
    public void ATrapIsHiddenUntilItGoesOffAndDrawnAfterwards()
    {
        Run run = Arena();

        Assert.Equal(TileKind.TrapArmed, run.Map[new Position(2, 1)]);

        run.Move(Directions.East);

        Assert.Equal(TileKind.TrapSprung, run.Map[new Position(2, 1)]);
    }

    [Fact]
    public void ATrapGoesOffOnceAndThenDoesNothing()
    {
        Run run = Arena();

        run.Move(Directions.East);
        int afterTheFirst = run.Player.Health;

        run.Move(Directions.East);
        run.Move(Directions.West);

        Assert.Equal(new Position(2, 1), run.Player.Position);
        Assert.Equal(afterTheFirst, run.Player.Health);
        Assert.Single(run.Log.Messages, m => m.Text.StartsWith("A trap goes off", StringComparison.Ordinal));
    }

    [Fact]
    public void ATrapHurtsAMonsterThatWalksOntoIt()
    {
        // The goblin stands west of the trap and the player stands east of it,
        // so the goblin's own turn walks it across. Nothing here reaches into
        // the trap; it goes off through the same path a real turn uses.
        Monster goblin = Chaser(new Position(1, 1));
        Run run = Arena(monsters: [goblin], playerAt: new Position(5, 1));

        int before = goblin.Health;

        run.Wait();

        Assert.Equal(new Position(2, 1), goblin.Position);
        Assert.Equal(before - SpawnTable.TrapDamage(1), goblin.Health);
        Assert.Equal(TileKind.TrapSprung, run.Map[new Position(2, 1)]);
        Assert.Contains(run.Log.Messages, m => m.Text == $"A trap goes off under a goblin for {SpawnTable.TrapDamage(1)}.");
    }

    [Fact]
    public void ATrapThatKillsAMonsterPaysNothing()
    {
        Monster goblin = Chaser(new Position(1, 1), health: 1);
        Run run = Arena(monsters: [goblin], playerAt: new Position(5, 1));

        int coins = run.Player.Coins;

        run.Wait();

        Assert.False(goblin.IsAlive);
        Assert.DoesNotContain(run.Monsters, m => ReferenceEquals(m, goblin));
        Assert.Equal(coins, run.Player.Coins);
    }

    [Fact]
    public void TrapsGrowMoreCommonAndHarderWithDepth()
    {
        Assert.True(SpawnTable.TrapCount(10) > SpawnTable.TrapCount(1));
        Assert.True(SpawnTable.TrapDamage(10) > SpawnTable.TrapDamage(1));

        for (int depth = 2; depth <= GameRules.FinalDepth; depth++)
        {
            Assert.True(SpawnTable.TrapCount(depth) >= SpawnTable.TrapCount(depth - 1));
            Assert.True(SpawnTable.TrapDamage(depth) >= SpawnTable.TrapDamage(depth - 1));
        }
    }

    [Fact]
    public void EveryFloorHasTrapsHiddenOnIt()
    {
        for (int seed = 0; seed < 20; seed++)
        {
            Run run = new(seed);

            Assert.Contains(run.Map.WalkableCells(), c => run.Map[c] == TileKind.TrapArmed);
        }
    }

    [Fact]
    public void ATrapIsNeverHiddenOnTheStairsOrWhereThePlayerArrives()
    {
        for (int seed = 0; seed < 20; seed++)
        {
            Run run = new(seed);

            Assert.NotEqual(TileKind.TrapArmed, run.Map[run.Map.Entrance]);
            Assert.NotEqual(TileKind.TrapArmed, run.Map[run.Map.StairsDown]);
        }
    }
}
