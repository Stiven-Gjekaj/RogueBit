using RogueBit.Core;
using RogueBit.Core.Entities;
using RogueBit.Core.Items;
using RogueBit.Core.Map;
using Xunit;

namespace RogueBit.Core.Tests;

public class RunTests
{
    [Fact]
    public void StartsOnTheFirstFloorWithThePlayerOnWalkableGround()
    {
        Run run = new(seed: 42);

        Assert.Equal(1, run.Depth);
        Assert.True(run.Map.IsWalkable(run.Player.Position));
        Assert.False(run.IsOver);
    }

    [Fact]
    public void WalkingIntoAWallIsRefusedAndCostsNoTurn()
    {
        Run run = new(seed: 42);

        // Stand the player against a wall rather than hoping it starts there.
        // The opening room on some seeds has open ground on all four sides.
        (Position cell, Position intoWall) = run.Map.WalkableCells()
            .SelectMany(c => Directions.Cardinal.Select(step => (Cell: c, Step: step)))
            .First(pair => !run.Map.IsWalkable(pair.Cell + pair.Step));

        run.Player.Position = cell;
        Position before = run.Player.Position;
        int turnsBefore = run.Turns;
        ActionResult result = run.Move(intoWall);

        Assert.Equal(ActionResult.Refused, result);
        Assert.Equal(before, run.Player.Position);
        Assert.Equal(turnsBefore, run.Turns);
    }

    [Fact]
    public void WalkingIntoOpenGroundTakesATurn()
    {
        Run run = new(seed: 42);
        Position before = run.Player.Position;
        Position open = Directions.Cardinal.First(step => run.Map.IsWalkable(before + step));

        ActionResult result = run.Move(open);

        Assert.Equal(ActionResult.Took, result);
        Assert.Equal(1, run.Turns);
    }

    [Fact]
    public void WaitingTakesATurnWithoutMoving()
    {
        Run run = new(seed: 42);
        Position before = run.Player.Position;

        run.Wait();

        Assert.Equal(before, run.Player.Position);
        Assert.Equal(1, run.Turns);
    }

    [Fact]
    public void EveryFloorPlacesMonstersAndCoins()
    {
        Run run = new(seed: 7);

        Assert.NotEmpty(run.Monsters);
        Assert.Contains(run.Items, i => i.Kind == ItemKind.Coin);
    }

    [Fact]
    public void NothingSpawnsOnTopOfThePlayer()
    {
        for (int seed = 0; seed < 25; seed++)
        {
            Run run = new(seed);

            Assert.DoesNotContain(run.Monsters, m => m.Position == run.Player.Position);
        }
    }

    [Fact]
    public void NoTwoMonstersShareACell()
    {
        for (int seed = 0; seed < 25; seed++)
        {
            Run run = new(seed);

            Assert.Equal(run.Monsters.Count, run.Monsters.Select(m => m.Position).Distinct().Count());
        }
    }

    [Fact]
    public void EveryMonsterStandsOnWalkableGround()
    {
        for (int seed = 0; seed < 25; seed++)
        {
            Run run = new(seed);

            Assert.All(run.Monsters, m => Assert.True(run.Map.IsWalkable(m.Position)));
        }
    }

    [Fact]
    public void DescendingIsRefusedAwayFromTheStairs()
    {
        Run run = new(seed: 42);
        while (run.Map[run.Player.Position] == TileKind.StairsDown) run = new Run(run.Seed + 1);

        Assert.Equal(ActionResult.Refused, run.Descend());
        Assert.Equal(1, run.Depth);
    }

    [Fact]
    public void TheSameSeedReplaysTheSameOpeningFloor()
    {
        Run first = new(seed: 4242);
        Run second = new(seed: 4242);

        Assert.Equal(first.Player.Position, second.Player.Position);
        Assert.Equal(first.Monsters.Count, second.Monsters.Count);
        Assert.Equal(
            first.Monsters.Select(m => (m.Position, m.Name)),
            second.Monsters.Select(m => (m.Position, m.Name)));
    }

    [Fact]
    public void RestartingReplaysTheRunRatherThanRollingANewOne()
    {
        // This is the defect the old code shipped: restart reused the advanced
        // generator, so the same seed gave a different dungeon.
        Run run = new(seed: 4242);
        for (int i = 0; i < 20; i++) run.Move(Directions.East);

        Run restarted = run.Restart();
        Run fresh = new(seed: 4242);

        Assert.Equal(fresh.Player.Position, restarted.Player.Position);
        Assert.Equal(
            fresh.Monsters.Select(m => m.Position),
            restarted.Monsters.Select(m => m.Position));
    }

    [Fact]
    public void TheSameSeedPlaysOutIdenticallyForAWholeRun()
    {
        static (int Depth, int Score, int Health, int Turns) Play(int seed)
        {
            Run run = new(seed);
            Position[] walk = [Directions.East, Directions.South, Directions.West, Directions.North];

            for (int i = 0; i < 400 && !run.IsOver; i++)
            {
                run.Move(walk[i % walk.Length]);
                run.Descend();
            }

            return (run.Depth, run.Score, run.Player.Health, run.Turns);
        }

        Assert.Equal(Play(2024), Play(2024));
    }

    [Fact]
    public void ScoreRewardsGoingDeeper()
    {
        Run run = new(seed: 1);
        int shallow = run.Score;

        Assert.True(StandOnStairsAndDescend(run), "descending from the stairs was refused");

        Assert.Equal(2, run.Depth);
        Assert.True(run.Score > shallow);
    }

    [Fact]
    public void GoingDownBuildsAFreshFloor()
    {
        Run run = new(seed: 1);
        string[] first = MapBuilder.Render(run.Map);

        Assert.True(StandOnStairsAndDescend(run));

        Assert.NotEqual(first, MapBuilder.Render(run.Map));
        Assert.True(run.Map.IsWalkable(run.Player.Position));
    }

    [Fact]
    public void TheFirstFloorHasNoWayBackUp()
    {
        Run run = new(seed: 1);

        Assert.DoesNotContain(
            run.Map.WalkableCells(),
            c => run.Map[c] == TileKind.StairsUp);
    }

    [Fact]
    public void GoingDownLandsThePlayerOnTheStairsBackUp()
    {
        Run run = new(seed: 1);

        Assert.True(StandOnStairsAndDescend(run));

        Assert.Equal(TileKind.StairsUp, run.Map[run.Player.Position]);
        Assert.Equal(run.Map.Entrance, run.Player.Position);
    }

    [Fact]
    public void ThePlayerCanSeeTheGroundItStandsOn()
    {
        Run run = new(seed: 9);

        Assert.True(run.Map.IsVisible(run.Player.Position));
    }

    [Fact]
    public void ARunEndsWhenThePlayerRunsOutOfHealth()
    {
        Run run = new(seed: 3);
        Assert.False(run.IsOver);

        while (run.Player.IsAlive) run.Player.TakeDamage(1);

        Assert.True(run.IsOver);
        Assert.Equal(ActionResult.Refused, run.Move(Directions.East));
    }

    [Fact]
    public void PickingUpWithNothingUnderfootIsRefused()
    {
        Run run = new(seed: 5);
        while (run.ItemsAt(run.Player.Position).Any()) run = new Run(run.Seed + 1);

        Assert.Equal(ActionResult.Refused, run.PickUp());
    }

    [Fact]
    public void TheLogSaysSomethingFromTheStart()
    {
        Run run = new(seed: 5);

        Assert.NotEmpty(run.Log.Messages);
    }

    /// <summary>
    /// Puts the player on the stairs and takes them.
    ///
    /// An earlier version walked there by A*, and died on the way as soon as
    /// the generator changed. These two tests are about what descending does,
    /// not about surviving a walk, so they build the state they need instead of
    /// hoping a bot reaches it. That the stairs are reachable at all is held by
    /// PathFinderTests.FindsTheStairsFromTheEntranceOnEveryFloorItGenerates.
    /// </summary>
    private static bool StandOnStairsAndDescend(Run run)
    {
        run.Player.Position = run.Map.StairsDown;
        return run.Descend() == ActionResult.Took;
    }
}
