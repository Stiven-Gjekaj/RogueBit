using RogueBit.Core;
using RogueBit.Core.Entities;
using RogueBit.Core.Map;
using Xunit;

namespace RogueBit.Core.Tests;

/// <summary>
/// Which monster the status bar should name. The run answers that; nothing
/// here is about how it is drawn.
/// </summary>
public class MonsterWithinReachTests
{
    /// <summary>A five by five room with the player in the middle.</summary>
    private static Run Arena(params Monster[] monsters)
    {
        DungeonMap map = MapBuilder.From(
            "#######",
            "#.....#",
            "#.....#",
            "#.....#",
            "#.....#",
            "#.....#",
            "#######");
        map.Entrance = new Position(1, 1);
        map.StairsDown = new Position(5, 5);
        map[map.StairsDown] = TileKind.StairsDown;

        Floor floor = new() { Depth = 1, Map = map };
        floor.Monsters.AddRange(monsters);

        return Run.Resume(
            random: new SeededRandom(1),
            player: new Player(new Position(3, 3)),
            floors: [floor],
            depth: 1,
            deepestDepth: 1,
            turns: 0,
            log: []);
    }

    private static Monster Goblin(Position at, int health, string name = "a goblin")
    {
        Monster monster = new(at, maxHealth: 20, power: 0, defence: 0)
        {
            Glyph = 'g',
            Name = name,
            Behaviour = MonsterBehaviour.Chaser,
            AggroRadius = 0,
            CoinReward = 1,
        };

        monster.TakeDamage(20 - health);
        return monster;
    }

    [Fact]
    public void ReportsNothingWhenNothingIsBeside()
    {
        Run run = Arena(Goblin(new Position(1, 1), health: 5));

        Assert.Null(run.MonsterWithinReach);
    }

    [Fact]
    public void ReportsTheOneThatIsBeside()
    {
        Monster goblin = Goblin(new Position(4, 3), health: 5);
        Run run = Arena(goblin);

        Assert.Same(goblin, run.MonsterWithinReach);
    }

    [Fact]
    public void CountsACornerAsBeside()
    {
        Monster goblin = Goblin(new Position(4, 4), health: 5);
        Run run = Arena(goblin);

        Assert.Same(goblin, run.MonsterWithinReach);
    }

    [Fact]
    public void ReportsTheWeakestOfSeveral()
    {
        Monster strong = Goblin(new Position(4, 3), health: 9, name: "a strong goblin");
        Monster weak = Goblin(new Position(2, 3), health: 2, name: "a weak goblin");
        Monster middling = Goblin(new Position(3, 2), health: 5, name: "a middling goblin");

        Run run = Arena(strong, weak, middling);

        Assert.Same(weak, run.MonsterWithinReach);
    }

    [Fact]
    public void DoesNotReportOneThatIsAlreadyDead()
    {
        Monster dead = Goblin(new Position(4, 3), health: 20);
        dead.TakeDamage(20);

        Run run = Arena(dead);

        Assert.False(dead.IsAlive);
        Assert.Null(run.MonsterWithinReach);
    }

    [Fact]
    public void DoesNotReportOneTwoCellsAway()
    {
        Run run = Arena(Goblin(new Position(5, 3), health: 1));

        Assert.Null(run.MonsterWithinReach);
    }
}
