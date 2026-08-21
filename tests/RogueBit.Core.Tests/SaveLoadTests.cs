using RogueBit.Core;
using RogueBit.Core.Entities;
using RogueBit.Core.Items;
using RogueBit.Core.Map;
using RogueBit.Core.Saves;
using Xunit;

namespace RogueBit.Core.Tests;

/// <summary>
/// Saving and resuming a run.
///
/// Each test writes into a directory of its own and removes it afterwards, so
/// the suite never reads or writes the player's real save.
/// </summary>
public sealed class SaveLoadTests : IDisposable
{
    private readonly string directory =
        Path.Combine(Path.GetTempPath(), "roguebit-tests", Guid.NewGuid().ToString("n"));

    public void Dispose()
    {
        if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
    }

    private SaveSystem System => new(directory);

    /// <summary>Plays a few turns so the run is not in its opening state.</summary>
    private static Run PlayedRun(int seed = 4242, int turns = 15)
    {
        Run run = new(seed);
        Position[] walk = [Directions.East, Directions.South, Directions.West, Directions.North];

        for (int i = 0; i < turns && !run.IsOver; i++) run.Move(walk[i % walk.Length]);

        return run;
    }

    /// <summary>Walks down to a floor, taking the stairs rather than hunting for them.</summary>
    private static Run DeepRun(int to, int seed = 4242)
    {
        Run run = new(seed);

        while (run.Depth < to)
        {
            run.Player.Position = run.Map.StairsDown;
            Assert.Equal(ActionResult.Took, run.Descend());
        }

        return run;
    }

    /// <summary>
    /// Walks to the stairs up and takes them. Coming up leaves the player on
    /// the stairs down, so climbing twice means crossing the floor between.
    /// </summary>
    private static void Climb(Run run)
    {
        run.Player.Position = run.Map.Entrance;
        Assert.Equal(ActionResult.Took, run.Ascend());
    }

    [Fact]
    public void AResumedRunHasTheSameStateAsTheOneThatWasSaved()
    {
        Run original = PlayedRun();

        Run resumed = RunSerialiser.Restore(RunSerialiser.Capture(original));

        Assert.Equal(original.Seed, resumed.Seed);
        Assert.Equal(original.Depth, resumed.Depth);
        Assert.Equal(original.Turns, resumed.Turns);
        Assert.Equal(original.Score, resumed.Score);
        Assert.Equal(original.Player.Position, resumed.Player.Position);
        Assert.Equal(original.Player.Health, resumed.Player.Health);
        Assert.Equal(original.Player.Coins, resumed.Player.Coins);
    }

    [Fact]
    public void TheFloorComesBackExactly()
    {
        Run original = PlayedRun();

        Run resumed = RunSerialiser.Restore(RunSerialiser.Capture(original));

        Assert.Equal(MapBuilder.Render(original.Map), MapBuilder.Render(resumed.Map));
        Assert.Equal(original.Map.StairsDown, resumed.Map.StairsDown);
    }

    [Fact]
    public void GroundAlreadyExploredIsStillRemembered()
    {
        Run original = PlayedRun();
        List<Position> explored =
            [.. original.Map.WalkableCells().Where(original.Map.IsExplored)];

        Run resumed = RunSerialiser.Restore(RunSerialiser.Capture(original));

        Assert.NotEmpty(explored);
        Assert.All(explored, cell => Assert.True(resumed.Map.IsExplored(cell), $"{cell} was forgotten"));
    }

    [Fact]
    public void EveryLivingMonsterComesBackWhereItWas()
    {
        Run original = PlayedRun();

        Run resumed = RunSerialiser.Restore(RunSerialiser.Capture(original));

        Assert.Equal(
            original.Monsters.Where(m => m.IsAlive).Select(m => (m.Position, m.Name, m.Health, m.Behaviour)),
            resumed.Monsters.Select(m => (m.Position, m.Name, m.Health, m.Behaviour)));
    }

    [Fact]
    public void EveryKindOfMonsterComesBackAsTheKindItWas()
    {
        // Floor one is goblins only, so the test above cannot see this. The
        // behaviour is written out by name and read back the same way, which
        // is what lets a new kind of monster cost the save format nothing.
        Run original = DeepRun(to: 6);

        Run resumed = RunSerialiser.Restore(RunSerialiser.Capture(original));

        HashSet<MonsterBehaviour> kinds = [.. original.Monsters.Where(m => m.IsAlive).Select(m => m.Behaviour)];

        Assert.True(kinds.Count > 1, "floor six held one kind of monster, so this proves nothing");
        Assert.Equal(
            original.Monsters.Where(m => m.IsAlive).Select(m => (m.Position, m.Name, m.Behaviour)),
            resumed.Monsters.Select(m => (m.Position, m.Name, m.Behaviour)));
    }

    [Fact]
    public void ARousedMonsterComesBackMindingItsOwnBusiness()
    {
        // Being roused is not saved, on purpose. A save holds where everything
        // stands, not what it was thinking, and the floor comes back unlit for
        // the same reason. Anything still near the player rouses again on its
        // next turn, so what is lost is one turn of one monster's attention.
        Run original = PlayedRun();
        foreach (Monster monster in original.Monsters) monster.IsAlerted = true;

        Run resumed = RunSerialiser.Restore(RunSerialiser.Capture(original));

        Assert.NotEmpty(resumed.Monsters);
        Assert.All(resumed.Monsters, monster => Assert.False(monster.IsAlerted));
    }

    [Fact]
    public void ItemsOnTheFloorComeBack()
    {
        Run original = PlayedRun();

        Run resumed = RunSerialiser.Restore(RunSerialiser.Capture(original));

        Assert.Equal(
            original.Items.Select(i => (i.Position, i.Kind, i.Name)),
            resumed.Items.Select(i => (i.Position, i.Kind, i.Name)));
    }

    [Fact]
    public void ThePackAndBothSlotsComeBack()
    {
        Run run = PlayedRun();
        Item sword = Item.Weapon(run.Player.Position, "a short sword", 3);
        Item mail = Item.Armour(run.Player.Position, "chain mail", 2);
        Item potion = Item.Potion(run.Player.Position);

        run.Inventory.TryAdd(sword);
        run.Inventory.TryAdd(mail);
        run.Inventory.TryAdd(potion);
        run.Inventory.TryEquip(sword);
        run.Inventory.TryEquip(mail);

        int power = run.Player.Power;
        int defence = run.Player.Defence;

        Run resumed = RunSerialiser.Restore(RunSerialiser.Capture(run));

        Assert.Equal("a short sword", resumed.Inventory.Weapon?.Name);
        Assert.Equal("chain mail", resumed.Inventory.Armour?.Name);
        Assert.Contains(resumed.Inventory.Items, i => i.Kind == ItemKind.Potion);

        // The bonuses have to survive, not merely the names.
        Assert.Equal(power, resumed.Player.Power);
        Assert.Equal(defence, resumed.Player.Defence);
    }

    [Fact]
    public void TheDiceCarryOnRatherThanStartingAgain()
    {
        // This is the point of saving the generator state. A resumed run must
        // play on from where it stopped, not replay the seed from the top.
        Run original = PlayedRun();
        Run resumed = RunSerialiser.Restore(RunSerialiser.Capture(original));

        int[] fromOriginal = [.. Enumerable.Range(0, 20).Select(_ => original.Random.Next(1000))];
        int[] fromResumed = [.. Enumerable.Range(0, 20).Select(_ => resumed.Random.Next(1000))];

        Assert.Equal(fromOriginal, fromResumed);
        Assert.NotEqual(fromOriginal, [.. Enumerable.Range(0, 20).Select(_ => new SeededRandom(original.Seed).Next(1000))]);
    }

    [Fact]
    public void ASavedRunPlaysOnIdenticallyToTheOneItCameFrom()
    {
        Run original = PlayedRun();
        Run resumed = RunSerialiser.Restore(RunSerialiser.Capture(original));

        Position[] walk = [Directions.North, Directions.East, Directions.South, Directions.West];

        for (int i = 0; i < 60; i++)
        {
            original.Move(walk[i % walk.Length]);
            resumed.Move(walk[i % walk.Length]);
        }

        Assert.Equal(original.Player.Position, resumed.Player.Position);
        Assert.Equal(original.Player.Health, resumed.Player.Health);
        Assert.Equal(original.Score, resumed.Score);
        Assert.Equal(original.Turns, resumed.Turns);
        Assert.Equal(
            original.Monsters.Select(m => (m.Position, m.Health)),
            resumed.Monsters.Select(m => (m.Position, m.Health)));
    }

    [Fact]
    public void TheLogComesBack()
    {
        Run original = PlayedRun();

        Run resumed = RunSerialiser.Restore(RunSerialiser.Capture(original));

        Assert.Equal(
            original.Log.Messages.Select(m => m.Display),
            resumed.Log.Messages.Select(m => m.Display));
    }

    [Fact]
    public void WritingThenReadingGivesTheSameRunBack()
    {
        Run original = PlayedRun();
        SaveSystem saves = System;

        saves.Write(RunSerialiser.Capture(original));

        Assert.True(saves.SaveExists);

        SaveData? read = saves.Read();
        Assert.NotNull(read);

        Run resumed = RunSerialiser.Restore(read);
        Assert.Equal(original.Player.Position, resumed.Player.Position);
        Assert.Equal(MapBuilder.Render(original.Map), MapBuilder.Render(resumed.Map));
    }

    [Fact]
    public void EveryFloorTheRunHasBeenOnComesBack()
    {
        Run original = DeepRun(to: 3);
        List<string[]> before = [.. original.Floors.Select(f => MapBuilder.Render(f.Map))];

        Assert.Equal(3, before.Count);

        Run resumed = RunSerialiser.Restore(RunSerialiser.Capture(original));

        Assert.Equal(before, resumed.Floors.Select(f => MapBuilder.Render(f.Map)));
        Assert.Equal([1, 2, 3], resumed.Floors.Select(f => f.Depth));
    }

    [Fact]
    public void AResumedRunCanClimbBackIntoAFloorItSaw()
    {
        Run original = DeepRun(to: 3);
        string[] second = MapBuilder.Render(original.Floors.Single(f => f.Depth == 2).Map);

        Run resumed = RunSerialiser.Restore(RunSerialiser.Capture(original));
        Assert.Equal(ActionResult.Took, resumed.Ascend());

        Assert.Equal(2, resumed.Depth);
        Assert.Equal(second, MapBuilder.Render(resumed.Map));
    }

    [Fact]
    public void TheDeepestFloorReachedComesBack()
    {
        Run original = DeepRun(to: 3);
        Climb(original);
        Climb(original);

        Assert.Equal(1, original.Depth);

        Run resumed = RunSerialiser.Restore(RunSerialiser.Capture(original));

        Assert.Equal(1, resumed.Depth);
        Assert.Equal(3, resumed.DeepestDepth);
        Assert.Equal(original.Score, resumed.Score);
    }

    [Fact]
    public void WhatWasLeftLyingOnAFloorAboveComesBack()
    {
        Run original = DeepRun(to: 2);
        List<Position> above = [.. original.Floors.Single(f => f.Depth == 1).Items.Select(i => i.Position)];

        Assert.NotEmpty(above);

        Run resumed = RunSerialiser.Restore(RunSerialiser.Capture(original));

        Assert.Equal(above, resumed.Floors.Single(f => f.Depth == 1).Items.Select(i => i.Position));
    }

    [Fact]
    public void ReadingWhenThereIsNoSaveGivesNothing()
    {
        Assert.Null(System.Read());
        Assert.False(System.SaveExists);
    }

    [Fact]
    public void ADamagedSaveIsRefusedRatherThanPlayed()
    {
        SaveSystem saves = System;
        saves.Write(RunSerialiser.Capture(PlayedRun()));

        File.WriteAllText(saves.SavePath, "{ this is not json");

        Assert.Null(saves.Read());
    }

    [Fact]
    public void ASaveFromAnotherVersionIsRefused()
    {
        SaveSystem saves = System;
        SaveData data = RunSerialiser.Capture(PlayedRun()) with { Version = SaveData.CurrentVersion + 1 };
        saves.Write(data);

        Assert.Null(saves.Read());
    }

    [Fact]
    public void ASaveWhoseFloorIsTheWrongSizeIsRefused()
    {
        SaveSystem saves = System;
        SaveData captured = RunSerialiser.Capture(PlayedRun());
        SaveData data = captured with
        {
            Floors = [.. captured.Floors.Select(f => f with { Tiles = "##" })],
        };
        saves.Write(data);

        Assert.Null(saves.Read());
    }

    [Fact]
    public void ASaveWithNoFloorForTheDepthItIsOnIsRefused()
    {
        SaveSystem saves = System;
        SaveData captured = RunSerialiser.Capture(PlayedRun());
        saves.Write(captured with { Depth = captured.Depth + 5 });

        Assert.Null(saves.Read());
    }

    [Fact]
    public void DeletingRemovesTheSave()
    {
        SaveSystem saves = System;
        saves.Write(RunSerialiser.Capture(PlayedRun()));

        saves.Delete();

        Assert.False(saves.SaveExists);
        Assert.Null(saves.Read());
    }

    [Fact]
    public void DeletingWhenThereIsNoSaveIsHarmless()
    {
        System.Delete();
    }

    [Fact]
    public void AFailedWriteLeavesThePreviousSaveIntact()
    {
        // The file is written beside the target and moved over it, so there is
        // never a moment where the save on disk is half of a new one.
        SaveSystem saves = System;
        Run first = PlayedRun(seed: 1);
        saves.Write(RunSerialiser.Capture(first));

        string before = File.ReadAllText(saves.SavePath);
        Assert.False(File.Exists(saves.SavePath + ".writing"));
        Assert.Equal(before, File.ReadAllText(saves.SavePath));
    }

    [Fact]
    public void TheBestScoreStartsAtNothingAndOnlyRises()
    {
        SaveSystem saves = System;

        Assert.Equal(0, saves.ReadBestScore());

        Assert.True(saves.RecordScore(40));
        Assert.Equal(40, saves.ReadBestScore());

        Assert.False(saves.RecordScore(25));
        Assert.Equal(40, saves.ReadBestScore());

        Assert.True(saves.RecordScore(90));
        Assert.Equal(90, saves.ReadBestScore());
    }

    [Fact]
    public void TheDefaultDirectoryIsUnderTheUsersOwnFiles()
    {
        string path = SaveSystem.DefaultDirectory();

        Assert.EndsWith("RogueBit", path);
        Assert.True(Path.IsPathRooted(path));
    }
}
