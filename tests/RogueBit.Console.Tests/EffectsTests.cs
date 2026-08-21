using RogueBit.Core;
using Xunit;

namespace RogueBit.Console.Tests;

/// <summary>
/// The flashes, sparks and shake.
///
/// None of this changes how the game plays, which is why it lives in the
/// frontend. It is still logic, so it is still tested. Nothing here opens a
/// window, so the suite runs with no display attached.
/// </summary>
public class EffectsTests
{
    private static Effects New(int seed = 1) => new(Theme.Standard, seed);

    private static TurnEvent Hit(bool onPlayer, int damage = 4) =>
        new(TurnEventKind.Hit, new Position(5, 5), damage, onPlayer);

    private static TurnEvent Trap(bool onPlayer, int damage = 4) =>
        new(TurnEventKind.Trap, new Position(5, 5), damage, onPlayer);

    private static TurnEvent Call(int answered) =>
        new(TurnEventKind.Call, new Position(5, 5), answered);

    /// <summary>Runs the clock until nothing is left, or gives up.</summary>
    private static double Settle(Effects effects, double limit = 10.0)
    {
        double elapsed = 0;

        while (effects.IsBusy && elapsed < limit)
        {
            effects.Update(1.0 / 60);
            elapsed += 1.0 / 60;
        }

        return elapsed;
    }

    [Fact]
    public void NothingIsHappeningToStartWith()
    {
        Effects effects = New();

        Assert.False(effects.IsBusy);
        Assert.Empty(effects.Particles);
        Assert.Equal((0, 0), effects.ShakeOffset);
    }

    [Fact]
    public void AHitThrowsParticles()
    {
        Effects effects = New();

        effects.Play([Hit(onPlayer: false)]);

        Assert.NotEmpty(effects.Particles);
        Assert.True(effects.IsBusy);
    }

    [Fact]
    public void OnlyABlowThePlayerTookShakesTheScreen()
    {
        // Shaking for every hit anywhere makes the whole game feel loose.
        Effects onPlayer = New();
        onPlayer.Play([Hit(onPlayer: true)]);

        Effects onMonster = New();
        onMonster.Play([Hit(onPlayer: false)]);

        Assert.NotEqual((0, 0), onPlayer.ShakeOffset);
        Assert.Equal((0, 0), onMonster.ShakeOffset);
    }

    [Fact]
    public void AHeavierBlowShakesFurther()
    {
        Effects light = New();
        light.Play([Hit(onPlayer: true, damage: 1)]);

        Effects heavy = New();
        heavy.Play([Hit(onPlayer: true, damage: 12)]);

        Assert.True(FurthestShake(heavy) >= FurthestShake(light));
    }

    [Fact]
    public void ABiggerHitThrowsMoreParticles()
    {
        Effects small = New();
        small.Play([Hit(onPlayer: false, damage: 1)]);

        Effects big = New();
        big.Play([Hit(onPlayer: false, damage: 10)]);

        Assert.True(big.Particles.Count > small.Particles.Count);
    }

    [Fact]
    public void ABlowTurnedAsideIsQuieterThanOneThatLanded()
    {
        Effects blocked = New();
        blocked.Play([new TurnEvent(TurnEventKind.Blocked, new Position(3, 3))]);

        Effects landed = New();
        landed.Play([Hit(onPlayer: false, damage: 6)]);

        Assert.True(blocked.Particles.Count < landed.Particles.Count);
        Assert.Equal((0, 0), blocked.ShakeOffset);
    }

    /// <summary>
    /// The furthest the screen is pushed. The offset is drawn at random within
    /// the current magnitude, so it has to be sampled rather than read once.
    /// </summary>
    private static int FurthestShake(Effects effects, int samples = 400)
    {
        int furthest = 0;

        for (int i = 0; i < samples; i++)
        {
            (int x, int y) = effects.ShakeOffset;
            furthest = Math.Max(furthest, Math.Max(Math.Abs(x), Math.Abs(y)));
        }

        return furthest;
    }

    [Fact]
    public void ThePlayerDyingShakesHarderThanAMonsterDying()
    {
        Effects player = New();
        player.Play([new TurnEvent(TurnEventKind.Death, new Position(3, 3), 0, AgainstPlayer: true)]);

        Effects monster = New();
        monster.Play([new TurnEvent(TurnEventKind.Death, new Position(3, 3))]);

        // Measure the shake itself. An earlier version of this test timed how
        // long the effect took to settle, which is dominated by how long the
        // particles live and says nothing about how hard the screen moved.
        Assert.True(
            FurthestShake(player) > FurthestShake(monster),
            "the player dying should move the screen further than a monster dying");
    }

    [Fact]
    public void ATrapThrowsWhatABlowThrows()
    {
        Effects trap = New();
        Effects blow = New();

        trap.Play([Trap(onPlayer: true)]);
        blow.Play([Hit(onPlayer: true)]);

        Assert.NotEmpty(trap.Particles);
        Assert.Equal(blow.Particles.Count, trap.Particles.Count);
    }

    [Fact]
    public void OnlyATrapThePlayerSteppedOnShakesTheScreen()
    {
        Effects onPlayer = New();
        onPlayer.Play([Trap(onPlayer: true)]);

        Effects onMonster = New();
        onMonster.Play([Trap(onPlayer: false)]);

        Assert.True(FurthestShake(onPlayer) > 0);
        Assert.Equal(0, FurthestShake(onMonster));
    }

    [Fact]
    public void EverythingSettlesOnItsOwn()
    {
        Effects effects = New();
        effects.Play([Hit(onPlayer: true, damage: 10)]);

        double elapsed = Settle(effects);

        Assert.False(effects.IsBusy);
        Assert.Empty(effects.Particles);
        Assert.Equal((0, 0), effects.ShakeOffset);
        Assert.True(elapsed < 3.0, $"took {elapsed:F2}s to settle, which is far too long for one hit");
    }

    [Fact]
    public void AParticleFadesFromFullToNothing()
    {
        Effects effects = New();
        effects.Play([Hit(onPlayer: false)]);

        Assert.All(effects.Particles, p => Assert.InRange(p.Remaining, 0.0, 1.0));

        double before = effects.Particles[0].Remaining;
        effects.Update(0.1);

        Assert.True(effects.Particles.Count == 0 || effects.Particles[0].Remaining < before);
    }

    [Fact]
    public void AParticleMovesWhileItLives()
    {
        Effects effects = New();
        effects.Play([Hit(onPlayer: false)]);

        Particle particle = effects.Particles[0];
        (double x, double y) = (particle.X, particle.Y);

        effects.Update(0.05);

        Assert.True(particle.X != x || particle.Y != y);
    }

    [Fact]
    public void NoTimePassingChangesNothing()
    {
        Effects effects = New();
        effects.Play([Hit(onPlayer: true)]);

        int before = effects.Particles.Count;
        effects.Update(0);
        effects.Update(-1);

        Assert.Equal(before, effects.Particles.Count);
    }

    [Fact]
    public void ClearingStopsEverythingAtOnce()
    {
        Effects effects = New();
        effects.Play([Hit(onPlayer: true, damage: 10)]);

        effects.Clear();

        Assert.False(effects.IsBusy);
        Assert.Empty(effects.Particles);
        Assert.Equal((0, 0), effects.ShakeOffset);
    }

    [Fact]
    public void GainedThingsRiseRatherThanBurst()
    {
        Effects effects = New();
        effects.Play([new TurnEvent(TurnEventKind.Heal, new Position(4, 4), 6)]);

        // Rising means every particle is heading up the screen.
        Assert.NotEmpty(effects.Particles);
        Assert.All(effects.Particles, p => Assert.True(p.VelocityY < 0, "a rising particle should move up"));
    }

    [Fact]
    public void TheSameSeedPlaysTheSameEffect()
    {
        Effects first = New(seed: 99);
        Effects second = New(seed: 99);

        first.Play([Hit(onPlayer: true, damage: 5)]);
        second.Play([Hit(onPlayer: true, damage: 5)]);

        Assert.Equal(
            first.Particles.Select(p => (p.X, p.Y, p.VelocityX, p.VelocityY, p.Glyph)),
            second.Particles.Select(p => (p.X, p.Y, p.VelocityX, p.VelocityY, p.Glyph)));
    }

    [Fact]
    public void AnEmptyTurnProducesNothing()
    {
        Effects effects = New();

        effects.Play([]);

        Assert.False(effects.IsBusy);
    }

    [Fact]
    public void RefusesATurnThatIsNotThere()
        => Assert.Throws<ArgumentNullException>(() => New().Play(null!));

    [Fact]
    public void ACallThrowsARing()
    {
        Effects effects = New();

        effects.Play([Call(answered: 0)]);

        Assert.NotEmpty(effects.Particles);

        // Every particle of a ring leaves at the same speed, which is what
        // holds its shape. A spray does not.
        double[] speeds =
        [
            .. effects.Particles.Select(p => Math.Sqrt((p.VelocityX * p.VelocityX) + (p.VelocityY * p.VelocityY))),
        ];

        Assert.All(speeds, speed => Assert.Equal(speeds[0], speed, 6));
    }

    [Fact]
    public void ACallManyAnsweredIsWiderThanOneNobodyDid()
    {
        Effects quiet = New();
        Effects loud = New();

        quiet.Play([Call(answered: 0)]);
        loud.Play([Call(answered: 4)]);

        Assert.True(loud.Particles.Count > quiet.Particles.Count);
    }

    [Fact]
    public void ACallDoesNotShakeTheScreen()
    {
        Effects effects = New();

        effects.Play([Call(answered: 8)]);

        Assert.Equal((0, 0), effects.ShakeOffset);
    }
}
