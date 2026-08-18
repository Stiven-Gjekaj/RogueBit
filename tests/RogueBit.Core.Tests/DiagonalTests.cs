using RogueBit.Core;
using RogueBit.Core.Entities;
using RogueBit.Core.Map;
using Xunit;

namespace RogueBit.Core.Tests;

/// <summary>
/// Moving in eight directions, for the player and for the monsters together.
/// A change that only touched the keys would look right and would quietly make
/// the monsters useless, so most of this is about them.
/// </summary>
public class DiagonalTests
{
    private static Run Arena(string[] rows, Position playerAt, params Monster[] monsters)
    {
        DungeonMap map = MapBuilder.From(rows);
        map.Entrance = playerAt;
        map.StairsDown = playerAt;

        Floor floor = new() { Depth = 1, Map = map };
        floor.Monsters.AddRange(monsters);

        return Run.Resume(
            random: new SeededRandom(1),
            player: new Player(playerAt),
            floors: [floor],
            depth: 1,
            deepestDepth: 1,
            turns: 0,
            log: []);
    }

    private static Monster Goblin(Position at, int power = 3) =>
        new(at, maxHealth: 50, power: power, defence: 0)
        {
            Glyph = 'g',
            Name = "a goblin",
            Behaviour = MonsterBehaviour.Chaser,
            AggroRadius = 12,
            CoinReward = 1,
        };

    [Fact]
    public void ThePlayerCanStepDiagonally()
    {
        Run run = Arena(["#####", "#...#", "#...#", "#...#", "#####"], new Position(1, 1));

        Assert.Equal(ActionResult.Took, run.Move(Directions.SouthEast));
        Assert.Equal(new Position(2, 2), run.Player.Position);
    }

    [Fact]
    public void ThePlayerCannotCutBetweenTwoWallsAtACorner()
    {
        Run run = Arena(
            [
                "######",
                "#..###",
                "#..###",
                "###..#",
                "###..#",
                "######",
            ],
            new Position(2, 2));

        Assert.Equal(ActionResult.Refused, run.Move(Directions.SouthEast));
        Assert.Equal(new Position(2, 2), run.Player.Position);
    }

    [Fact]
    public void ARefusedDiagonalCostsNoTurn()
    {
        Run run = Arena(
            [
                "######",
                "#..###",
                "#..###",
                "###..#",
                "###..#",
                "######",
            ],
            new Position(2, 2));

        run.Move(Directions.SouthEast);

        Assert.Equal(0, run.Turns);
    }

    [Fact]
    public void WalkingDiagonallyIntoAMonsterAttacksIt()
    {
        Monster goblin = Goblin(new Position(2, 2), power: 0);
        Run run = Arena(["#####", "#...#", "#...#", "#...#", "#####"], new Position(1, 1), goblin);

        Assert.Equal(ActionResult.Took, run.Move(Directions.SouthEast));

        Assert.Equal(new Position(1, 1), run.Player.Position);
        Assert.True(goblin.Health < goblin.MaxHealth);
    }

    [Fact]
    public void AMonsterOnACornerSwingsRatherThanShuffling()
    {
        // This is what a change to the keys alone would have broken. Under the
        // old distance a monster standing on a corner was two cells away, so it
        // never attacked and spent every turn walking into an occupied cell.
        Monster goblin = Goblin(new Position(2, 2));
        Run run = Arena(["#####", "#...#", "#...#", "#...#", "#####"], new Position(1, 1), goblin);

        int before = run.Player.Health;

        run.Wait();

        Assert.Equal(new Position(2, 2), goblin.Position);
        Assert.True(run.Player.Health < before, "the goblin on the corner did not swing");
    }

    [Fact]
    public void AMonsterWalksDiagonallyToReachThePlayer()
    {
        Monster goblin = Goblin(new Position(1, 1), power: 0);
        Run run = Arena(
            [
                "######",
                "#....#",
                "#....#",
                "#....#",
                "#....#",
                "######",
            ],
            new Position(4, 4),
            goblin);

        run.Wait();

        // One step of a walker that moves in four directions would reach (2,1)
        // or (1,2). Only an eight direction walker reaches the corner.
        Assert.Equal(new Position(2, 2), goblin.Position);
    }

    [Fact]
    public void AMonsterWillNotCutBetweenTwoWallsAtACornerEither()
    {
        // The player is one diagonal step away through solid rock. The monster
        // has to go the long way round, and here there is no long way round.
        Monster goblin = Goblin(new Position(2, 2), power: 0);
        Run run = Arena(
            [
                "######",
                "#..###",
                "#..###",
                "###..#",
                "###..#",
                "######",
            ],
            new Position(3, 3),
            goblin);

        int before = run.Player.Health;

        run.Wait();

        Assert.Equal(new Position(2, 2), goblin.Position);
        Assert.Equal(before, run.Player.Health);
    }
}
