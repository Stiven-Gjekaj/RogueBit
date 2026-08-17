using RogueBit.Core;
using RogueBit.Core.Combat;
using RogueBit.Core.Entities;
using Xunit;

namespace RogueBit.Core.Tests;

public class CombatResolverTests
{
    private static Monster Target(int health, int defence) =>
        new(new Position(0, 0), health, power: 1, defence)
        {
            Glyph = 'g',
            Name = "goblin",
            Behaviour = MonsterBehaviour.Chaser,
            AggroRadius = 8,
            CoinReward = 1,
        };

    [Fact]
    public void DamageIsPowerLessDefence()
    {
        Monster goblin = Target(health: 10, defence: 2);

        AttackResult result = CombatResolver.Resolve(power: 5, goblin);

        Assert.Equal(3, result.Damage);
        Assert.Equal(7, goblin.Health);
    }

    [Fact]
    public void ArmourNeverTurnsAnAttackIntoHealing()
    {
        Monster goblin = Target(health: 10, defence: 99);

        AttackResult result = CombatResolver.Resolve(power: 5, goblin);

        Assert.Equal(0, result.Damage);
        Assert.False(result.Hit);
        Assert.Equal(10, goblin.Health);
    }

    [Fact]
    public void ReportsAKill()
    {
        Monster goblin = Target(health: 3, defence: 0);

        AttackResult result = CombatResolver.Resolve(power: 5, goblin);

        Assert.True(result.Killed);
        Assert.False(goblin.IsAlive);
    }

    [Fact]
    public void ReportsOnlyTheDamageThatLanded()
    {
        Monster goblin = Target(health: 3, defence: 0);

        // Twenty points swung at three points of health is three points dealt.
        AttackResult result = CombatResolver.Resolve(power: 20, goblin);

        Assert.Equal(3, result.Damage);
    }

    [Fact]
    public void DescribesAHitAKillAndABlowTurnedAside()
    {
        Assert.Contains("for 3", CombatResolver.Describe("you", "a goblin", new AttackResult(3, false), true));
        Assert.Contains("kill it", CombatResolver.Describe("you", "a goblin", new AttackResult(3, true), true));
        Assert.Contains("turned aside", CombatResolver.Describe("you", "a goblin", new AttackResult(0, false), true));
    }

    [Fact]
    public void TheVerbAgreesWithWhoIsSwinging()
    {
        // "You hits a goblin" shipped in the first playable build.
        Assert.StartsWith("You hit a goblin", CombatResolver.Describe("you", "a goblin", new AttackResult(3, false), true));
        Assert.StartsWith("A goblin hits you", CombatResolver.Describe("a goblin", "you", new AttackResult(3, false), false));
    }

    [Fact]
    public void AKillNamesWhoActuallyDied()
    {
        // "a jackal hits you for 2, and kills it" also shipped. The player died.
        Assert.EndsWith("kills you", CombatResolver.Describe("a jackal", "you", new AttackResult(2, true), false));
        Assert.EndsWith("kill it", CombatResolver.Describe("you", "a jackal", new AttackResult(2, true), true));
    }

    [Fact]
    public void ASentenceOpensWithACapitalLetter()
    {
        Assert.StartsWith("A goblin", CombatResolver.Describe("a goblin", "you", new AttackResult(1, false), false));
    }
}
