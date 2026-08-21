using RogueBit.Core;
using RogueBit.Core.Entities;
using RogueBit.Core.Map;
using Xunit;

namespace RogueBit.Core.Tests;

/// <summary>
/// What the monsters do on their turn, apart from walking at the player.
///
/// Every arena here is built by hand and resumed rather than generated, so a
/// test says exactly which cells exist and exactly what is standing on them.
/// The player is given no power in most of them, because a monster that kills
/// the player ends the run and the rest of the test never runs.
/// </summary>
public class MonsterBehaviourTests
{
    private static Run Arena(string[] rows, Position playerAt, int seed, params Monster[] monsters)
    {
        DungeonMap map = MapBuilder.From(rows);
        map.Entrance = playerAt;
        map.StairsDown = playerAt;

        Floor floor = new() { Depth = 1, Map = map };
        floor.Monsters.AddRange(monsters);

        Player player = new(playerAt) { BasePower = 0 };

        return Run.Resume(
            random: new SeededRandom(seed),
            player: player,
            floors: [floor],
            depth: 1,
            deepestDepth: 1,
            turns: 0,
            log: []);
    }

    private static Monster Goblin(Position at, int aggroRadius = 2) =>
        new(at, maxHealth: 50, power: 0, defence: 0)
        {
            Glyph = 'g',
            Name = "a goblin",
            Behaviour = MonsterBehaviour.Chaser,
            AggroRadius = aggroRadius,
            CoinReward = 1,
        };

    private static Monster Scavenger(Position at) =>
        new(at, maxHealth: 10, power: 0, defence: 0)
        {
            Glyph = 's',
            Name = "a scavenger",
            Behaviour = MonsterBehaviour.Scavenger,
            AggroRadius = 12,
            CoinReward = 1,
        };

    /// <summary>
    /// Brings a monster down to a health, because Health cannot be set from
    /// outside and a wound is the only way in.
    /// </summary>
    private static Monster Hurt(Monster monster, int to)
    {
        monster.TakeDamage(monster.Health - to);
        return monster;
    }

    private static readonly string[] Corridor =
    [
        "#########",
        "#.......#",
        "#########",
    ];

    /// <summary>Where a monster that walked four steps straight at the player ends up.</summary>
    private static readonly Position ClosedIn = new(3, 1);

    [Fact]
    public void ARousedMonsterHuntsFromFurtherAwayThanItCanNotice()
    {
        for (int seed = 0; seed < 20; seed++)
        {
            Monster goblin = Goblin(new Position(7, 1));
            goblin.IsAlerted = true;

            Run run = Arena(Corridor, new Position(1, 1), seed, goblin);

            for (int turn = 0; turn < 4; turn++) run.Wait();

            Assert.Equal(ClosedIn, goblin.Position);
        }
    }

    [Fact]
    public void AMonsterThatHasNotBeenRousedDoesNotWalkStraightAtThePlayer()
    {
        int marchedIn = 0;

        for (int seed = 0; seed < 20; seed++)
        {
            Monster goblin = Goblin(new Position(7, 1));
            Run run = Arena(Corridor, new Position(1, 1), seed, goblin);

            for (int turn = 0; turn < 4; turn++) run.Wait();

            if (goblin.Position == ClosedIn) marchedIn++;
        }

        // A wandering monster can drift the right way by chance. What it
        // cannot do is take the shortest line every time on every seed, which
        // is what the roused one above does.
        Assert.True(marchedIn < 20, "every seed walked it straight in, so nothing is being roused");
    }

    [Fact]
    public void AScavengerComesAtThePlayerWhileItIsWhole()
    {
        Monster scavenger = Scavenger(new Position(7, 1));
        Run run = Arena(Corridor, new Position(1, 1), seed: 3, scavenger);

        for (int turn = 0; turn < 4; turn++) run.Wait();

        Assert.Equal(ClosedIn, scavenger.Position);
    }

    [Fact]
    public void AScavengerRunsOnceItIsBelowHalf()
    {
        Monster scavenger = Hurt(Scavenger(new Position(3, 1)), to: 4);
        Run run = Arena(Corridor, new Position(1, 1), seed: 3, scavenger);

        run.Wait();

        Assert.Equal(new Position(4, 1), scavenger.Position);
    }

    [Fact]
    public void AScavengerOnExactlyHalfIsStillComingForYou()
    {
        Monster scavenger = Hurt(Scavenger(new Position(3, 1)), to: 5);
        Run run = Arena(Corridor, new Position(1, 1), seed: 3, scavenger);

        run.Wait();

        Assert.Equal(new Position(2, 1), scavenger.Position);
    }

    [Fact]
    public void AScavengerKeepsRunningRatherThanTurningRoundOnce()
    {
        Monster scavenger = Hurt(Scavenger(new Position(3, 1)), to: 1);
        Run run = Arena(Corridor, new Position(1, 1), seed: 3, scavenger);

        for (int turn = 0; turn < 4; turn++) run.Wait();

        // Four steps, and then the far wall.
        Assert.Equal(new Position(7, 1), scavenger.Position);
    }

    [Fact]
    public void AScavengerThatCannotRunFightsInstead()
    {
        // A pocket one cell deep. The only way out is through the player.
        string[] deadEnd =
        [
            "#####",
            "#.#.#",
            "#...#",
            "#####",
        ];

        Monster scavenger = Hurt(Scavenger(new Position(3, 1)), to: 1);
        scavenger.BasePower = 4;

        Run run = Arena(deadEnd, new Position(2, 2), seed: 3, scavenger);
        int before = run.Player.Health;

        run.Wait();

        Assert.Equal(new Position(3, 1), scavenger.Position);
        Assert.True(run.Player.Health < before, "the cornered scavenger did nothing at all");
    }

    [Fact]
    public void ACorneredScavengerOutOfReachStandsStillRatherThanWalkingIn()
    {
        string[] deadEnd =
        [
            "#####",
            "#.#.#",
            "#.#.#",
            "#...#",
            "#####",
        ];

        Monster scavenger = Hurt(Scavenger(new Position(3, 1)), to: 1);
        Run run = Arena(deadEnd, new Position(1, 1), seed: 3, scavenger);

        for (int turn = 0; turn < 3; turn++) run.Wait();

        Assert.Equal(new Position(3, 1), scavenger.Position);
    }

    [Fact]
    public void RunningAwayNeverDrawsFromTheRunsChance()
    {
        // Two runs on one seed, one of which hurts a scavenger into running.
        // If fleeing rolled for anything, the two would stop matching.
        Monster whole = Scavenger(new Position(6, 1));
        Monster running = Hurt(Scavenger(new Position(6, 1)), to: 1);

        Run first = Arena(Corridor, new Position(1, 1), seed: 11, whole);
        Run second = Arena(Corridor, new Position(1, 1), seed: 11, running);

        for (int turn = 0; turn < 5; turn++)
        {
            first.Wait();
            second.Wait();
        }

        Assert.Equal(first.Random.Snapshot(), second.Random.Snapshot());

        // And the two did behave differently, so the comparison above is not
        // passing because both runs did the same thing.
        Assert.NotEqual(whole.Position, running.Position);
    }
}
