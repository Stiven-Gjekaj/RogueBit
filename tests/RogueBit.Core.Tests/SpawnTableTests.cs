using RogueBit.Core;
using RogueBit.Core.Entities;
using RogueBit.Core.Items;
using Xunit;

namespace RogueBit.Core.Tests;

public class SpawnTableTests
{
    private static readonly Position Origin = new(0, 0);

    [Fact]
    public void MoreMonstersAppearTheDeeperItGoes()
    {
        Assert.True(SpawnTable.MonsterCount(5) > SpawnTable.MonsterCount(1));
        Assert.True(SpawnTable.MonsterCount(10) > SpawnTable.MonsterCount(5));
    }

    [Fact]
    public void PotionsGrowScarcerTheDeeperItGoes()
    {
        Assert.True(SpawnTable.PotionCount(9) < SpawnTable.PotionCount(1));
    }

    [Fact]
    public void ThereIsAlwaysAtLeastOnePotionHoweverDeepItGets()
    {
        for (int depth = 1; depth <= 60; depth++)
        {
            Assert.True(SpawnTable.PotionCount(depth) >= 1, $"depth {depth} left no potions at all");
        }
    }

    [Fact]
    public void ABossStandsOnEveryFifthFloor()
    {
        Assert.True(SpawnTable.HasBoss(5));
        Assert.True(SpawnTable.HasBoss(10));
        Assert.False(SpawnTable.HasBoss(4));
        Assert.False(SpawnTable.HasBoss(6));
    }

    [Fact]
    public void TheFirstFloorHoldsOnlyGoblins()
    {
        // The harder kinds unlock with depth. A player meeting a swift monster
        // on floor one has no way to have learned what it does.
        for (int seed = 0; seed < 200; seed++)
        {
            Monster monster = SpawnTable.CreateMonster(Origin, depth: 1, new SeededRandom(seed));
            Assert.Equal(MonsterBehaviour.Chaser, monster.Behaviour);
        }
    }

    [Fact]
    public void DeeperFloorsEventuallyProduceEveryKind()
    {
        HashSet<MonsterBehaviour> seen = [];

        for (int seed = 0; seed < 200; seed++)
        {
            seen.Add(SpawnTable.CreateMonster(Origin, depth: 6, new SeededRandom(seed)).Behaviour);
        }

        Assert.Contains(MonsterBehaviour.Chaser, seen);
        Assert.Contains(MonsterBehaviour.Swift, seen);
        Assert.Contains(MonsterBehaviour.Archer, seen);
    }

    [Fact]
    public void MonstersGrowStrongerWithDepth()
    {
        Monster shallow = SpawnTable.Goblin(Origin, depth: 1);
        Monster deep = SpawnTable.Goblin(Origin, depth: 10);

        Assert.True(deep.MaxHealth > shallow.MaxHealth);
        Assert.True(deep.Power > shallow.Power);
    }

    [Fact]
    public void OnlyAnArcherCanShoot()
    {
        Assert.True(SpawnTable.Archer(Origin, 5).Range > 0);
        Assert.Equal(0, SpawnTable.Goblin(Origin, 5).Range);
        Assert.Equal(0, SpawnTable.Jackal(Origin, 5).Range);
    }

    [Fact]
    public void ABossIsWorthMoreThanAnythingElseOnItsFloor()
    {
        Monster boss = SpawnTable.Boss(Origin, depth: 5);

        Assert.True(boss.CoinReward > SpawnTable.Archer(Origin, 5).CoinReward);
        Assert.True(boss.MaxHealth > SpawnTable.Goblin(Origin, 5).MaxHealth * 2);
    }

    [Fact]
    public void EquipmentIsSometimesOfferedAndSometimesNot()
    {
        int offered = 0;

        for (int seed = 0; seed < 100; seed++)
        {
            if (SpawnTable.CreateEquipment(Origin, 3, new SeededRandom(seed)) is not null) offered++;
        }

        Assert.InRange(offered, 1, 99);
    }

    [Fact]
    public void EquipmentIsAlwaysAWeaponOrArmourAndNeverAPotion()
    {
        for (int seed = 0; seed < 100; seed++)
        {
            Item? item = SpawnTable.CreateEquipment(Origin, 3, new SeededRandom(seed));
            if (item is null) continue;

            Assert.True(item.IsEquipment);
            Assert.True(item.Bonus > 0);
        }
    }
}
