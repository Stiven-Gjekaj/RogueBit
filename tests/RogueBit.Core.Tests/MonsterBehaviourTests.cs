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

    private static Monster Howler(Position at) =>
        new(at, maxHealth: 10, power: 0, defence: 0)
        {
            Glyph = 'h',
            Name = "a howler",
            Behaviour = MonsterBehaviour.Howler,
            AggroRadius = 6,
            CoinReward = 1,
        };

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

    [Fact]
    public void AScavengerDrivenOntoATrapSetsItOff()
    {
        Monster scavenger = Hurt(Scavenger(new Position(3, 1)), to: 1);
        Run run = Arena(Corridor, new Position(1, 1), seed: 3, scavenger);
        run.Map[new Position(4, 1)] = TileKind.TrapArmed;

        run.Wait();

        Assert.Equal(TileKind.TrapSprung, run.Map[new Position(4, 1)]);
        Assert.Contains(
            run.LastTurnEvents,
            turn => turn.Kind == TurnEventKind.Trap && !turn.AgainstPlayer);
    }

    [Fact]
    public void ATrapCanFinishAScavengerThatWouldNotStandAndFight()
    {
        Monster scavenger = Hurt(Scavenger(new Position(3, 1)), to: 1);
        Run run = Arena(Corridor, new Position(1, 1), seed: 3, scavenger);
        run.Map[new Position(4, 1)] = TileKind.TrapArmed;

        run.Wait();

        Assert.False(scavenger.IsAlive);
    }

    [Fact]
    public void AWanderingMonsterStillWalksOverTrapsWithoutSettingThemOff()
    {
        // If wandering sprang traps, a floor would disarm itself while the
        // player was elsewhere, and every trap would be spent by the time it
        // arrived.
        string[] room =
        [
            "#####",
            "#...#",
            "#...#",
            "#...#",
            "#####",
        ];

        int sprung = 0;

        for (int seed = 0; seed < 40; seed++)
        {
            Monster goblin = Goblin(new Position(3, 3), aggroRadius: 0);
            Run run = Arena(room, new Position(1, 1), seed, goblin);

            foreach (Position cell in run.Map.WalkableCells())
            {
                if (cell != run.Player.Position) run.Map[cell] = TileKind.TrapArmed;
            }

            for (int turn = 0; turn < 6; turn++) run.Wait();

            sprung += run.Map.WalkableCells().Count(cell => run.Map[cell] == TileKind.TrapSprung);
        }

        Assert.Equal(0, sprung);
    }

    /// <summary>A room wide enough to stand a howler well away from a goblin.</summary>
    private static readonly string[] Hall =
    [
        "####################",
        "#..................#",
        "#..................#",
        "#..................#",
        "####################",
    ];

    [Fact]
    public void AHowlerRousesWhatIsNearIt()
    {
        Monster howler = Howler(new Position(5, 1));
        Monster goblin = Goblin(new Position(10, 3), aggroRadius: 1);

        Run run = Arena(Hall, new Position(1, 1), seed: 3, howler, goblin);

        Assert.False(goblin.IsAlerted);

        run.Wait();

        Assert.True(goblin.IsAlerted);
    }

    [Fact]
    public void AHowlerLeavesAloneWhatIsOutOfEarshot()
    {
        Monster howler = Howler(new Position(2, 1));
        Monster goblin = Goblin(new Position(18, 3), aggroRadius: 1);

        Run run = Arena(Hall, new Position(1, 1), seed: 3, howler, goblin);

        run.Wait();

        Assert.True(howler.IsAlerted);
        Assert.False(goblin.IsAlerted);
    }

    [Fact]
    public void AHowlerThatHasNotSeenThePlayerSaysNothing()
    {
        Monster howler = Howler(new Position(18, 3));
        Monster goblin = Goblin(new Position(17, 1), aggroRadius: 1);

        Run run = Arena(Hall, new Position(1, 1), seed: 3, howler, goblin);

        run.Wait();

        Assert.False(howler.IsAlerted);
        Assert.False(goblin.IsAlerted);
    }

    [Fact]
    public void AHowlerCallsOnceAndThenGetsOnWithIt()
    {
        Monster howler = Howler(new Position(5, 1));
        Run run = Arena(Hall, new Position(1, 1), seed: 3, howler);

        run.Wait();
        Assert.Equal(new Position(5, 1), howler.Position);

        // The turn after the call it closes in like anything else.
        run.Wait();
        Assert.Equal(new Position(4, 1), howler.Position);

        Assert.Single(run.Log.Messages, message => message.Text.Contains("calls out"));
    }

    [Fact]
    public void AHowlerRousedByAnotherDoesNotPassTheCallOn()
    {
        // Three in a line, each within earshot of the next but only the first
        // within sight of the player. Relaying would wake the third.
        Monster near = Howler(new Position(5, 1));
        Monster middle = Howler(new Position(15, 1));
        Monster far = Howler(new Position(19, 3));

        Run run = Arena(Hall, new Position(1, 1), seed: 3, near, middle, far);

        for (int turn = 0; turn < 3; turn++) run.Wait();

        Assert.True(near.IsAlerted);
        Assert.True(middle.IsAlerted);
        Assert.False(far.IsAlerted);
    }

    [Fact]
    public void ACallIsHeardEvenWhereItCannotBeSeen()
    {
        // A wall between the two. The howler is near enough to notice the
        // player and out of sight of them, which is the case the warning is
        // for.
        string[] twoRooms =
        [
            "##########",
            "#...#....#",
            "#...#....#",
            "#........#",
            "##########",
        ];

        Monster howler = Howler(new Position(6, 1));
        Run run = Arena(twoRooms, new Position(1, 1), seed: 3, howler);

        Assert.False(run.Map.IsVisible(howler.Position), "the howler was in plain sight, so this proves nothing");

        run.Wait();

        Assert.Contains(run.Log.Messages, message => message.Text.Contains("calls out"));
    }

    [Fact]
    public void ACallCountsOnlyWhatItActuallyRoused()
    {
        Monster howler = Howler(new Position(5, 1));
        Monster asleep = Goblin(new Position(8, 3), aggroRadius: 1);
        Monster alreadyComing = Goblin(new Position(9, 3), aggroRadius: 1);
        alreadyComing.IsAlerted = true;

        Run run = Arena(Hall, new Position(1, 1), seed: 3, howler, asleep, alreadyComing);

        run.Wait();

        Assert.Contains(
            run.Log.Messages,
            message => message.Text == "A howler calls out. One more comes for you.");
    }
}
