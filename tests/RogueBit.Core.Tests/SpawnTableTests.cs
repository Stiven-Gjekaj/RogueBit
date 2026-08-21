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

    /// <summary>What a floor of monsters actually turns out to be, by kind.</summary>
    private static Dictionary<MonsterBehaviour, int> Tally(int depth, int seeds = 400)
    {
        Dictionary<MonsterBehaviour, int> counted = [];

        for (int seed = 0; seed < seeds; seed++)
        {
            SeededRandom random = new(seed);

            for (int i = 0; i < SpawnTable.MonsterCount(depth); i++)
            {
                MonsterBehaviour kind = SpawnTable.CreateMonster(Origin, depth, random).Behaviour;
                counted[kind] = counted.GetValueOrDefault(kind) + 1;
            }
        }

        return counted;
    }

    [Fact]
    public void ScavengersStartOnTheSecondFloor()
    {
        Assert.False(Tally(1).ContainsKey(MonsterBehaviour.Scavenger));
        Assert.True(Tally(2).ContainsKey(MonsterBehaviour.Scavenger));
    }

    [Fact]
    public void AboutOneMonsterInFiveIsAScavengerOnceTheyAppear()
    {
        // Counted rather than reasoned about. The bands in CreateMonster
        // overlap, so what a threshold is worth depends on the lines above it
        // and cannot be read off the number itself.
        foreach (int depth in (int[])[2, 3, 5, 10])
        {
            Dictionary<MonsterBehaviour, int> tally = Tally(depth);
            int total = tally.Values.Sum();

            Assert.InRange(tally[MonsterBehaviour.Scavenger] * 100 / total, 15, 25);
        }
    }

    [Fact]
    public void GoblinsStayTheCommonestThingOnEveryFloor()
    {
        // Whatever else arrives, the kind the player learned first has to stay
        // the one they meet most, or the floor stops reading as this dungeon.
        foreach (int depth in (int[])[1, 2, 4, 7, 10])
        {
            Dictionary<MonsterBehaviour, int> tally = Tally(depth);

            Assert.All(
                tally.Where(pair => pair.Key != MonsterBehaviour.Chaser),
                pair => Assert.True(
                    pair.Value < tally[MonsterBehaviour.Chaser],
                    $"floor {depth} had more of {pair.Key} than goblins"));
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
        Assert.Contains(MonsterBehaviour.Scavenger, seen);
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
