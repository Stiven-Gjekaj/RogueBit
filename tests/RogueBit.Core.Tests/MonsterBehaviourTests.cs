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
}
