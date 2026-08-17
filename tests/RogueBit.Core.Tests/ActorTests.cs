using RogueBit.Core;
using RogueBit.Core.Entities;
using Xunit;

namespace RogueBit.Core.Tests;

public class ActorTests
{
    private static Monster Goblin(int health = 6, int power = 3, int defence = 0) =>
        new(new Position(0, 0), health, power, defence)
        {
            Glyph = 'g',
            Name = "goblin",
            Behaviour = MonsterBehaviour.Chaser,
            AggroRadius = 8,
            CoinReward = 2,
        };

    [Fact]
    public void StartsAtFullHealth()
    {
        Monster goblin = Goblin(health: 6);

        Assert.Equal(6, goblin.Health);
        Assert.Equal(6, goblin.MaxHealth);
        Assert.True(goblin.IsAlive);
    }

    [Fact]
    public void HealthNeverFallsBelowZero()
    {
        Monster goblin = Goblin(health: 6);

        int landed = goblin.TakeDamage(100);

        Assert.Equal(0, goblin.Health);
        Assert.Equal(6, landed);
        Assert.False(goblin.IsAlive);
    }

    [Fact]
    public void HealthNeverRisesAboveTheCeiling()
    {
        Monster goblin = Goblin(health: 6);
        goblin.TakeDamage(2);

        int restored = goblin.Heal(100);

        Assert.Equal(6, goblin.Health);
        Assert.Equal(2, restored);
    }

    [Fact]
    public void ADeadActorStopsBlockingTheWay()
    {
        Monster goblin = Goblin();
        Assert.True(goblin.BlocksMovement);

        goblin.TakeDamage(goblin.MaxHealth);

        Assert.False(goblin.BlocksMovement);
    }

    [Fact]
    public void RaisingTheCeilingAlsoGivesTheHealth()
    {
        Player player = new(new Position(0, 0));
        int before = player.Health;

        player.RaiseMaxHealth(5);

        Assert.Equal(before + 5, player.Health);
        Assert.Equal(player.MaxHealth, player.Health);
    }

    [Fact]
    public void RefusesNegativeDamageAndNegativeHealing()
    {
        Monster goblin = Goblin();

        Assert.Throws<ArgumentOutOfRangeException>(() => goblin.TakeDamage(-1));
        Assert.Throws<ArgumentOutOfRangeException>(() => goblin.Heal(-1));
    }

    [Fact]
    public void ASwiftMonsterTakesTwoStepsAndOthersTakeOne()
    {
        Assert.Equal(1, Goblin().Speed);

        Monster swift = new(new Position(0, 0), 4, 2, 0)
        {
            Glyph = 'j',
            Name = "jackal",
            Behaviour = MonsterBehaviour.Swift,
            AggroRadius = 10,
            CoinReward = 3,
        };

        Assert.Equal(2, swift.Speed);
    }

    [Fact]
    public void ABossHitsHarderOnceItIsBelowHalfHealth()
    {
        Monster boss = new(new Position(0, 0), 20, 5, 2)
        {
            Glyph = 'B',
            Name = "warden",
            Behaviour = MonsterBehaviour.Boss,
            AggroRadius = 12,
            CoinReward = 50,
        };

        Assert.False(boss.IsEnraged);
        Assert.Equal(5, boss.EffectivePower);

        boss.TakeDamage(11);

        Assert.True(boss.IsEnraged);
        Assert.Equal(10, boss.EffectivePower);
    }

    [Fact]
    public void OnlyABossCanBeEnraged()
    {
        Monster goblin = Goblin(health: 6);
        goblin.TakeDamage(5);

        Assert.False(goblin.IsEnraged);
        Assert.Equal(goblin.Power, goblin.EffectivePower);
    }
}
