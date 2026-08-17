namespace RogueBit.Console;

using SadRogue.Primitives;
using RogueBit.Core;

/// <summary>One drifting glyph thrown off by something that happened.</summary>
public sealed class Particle
{
    public required double X { get; set; }

    public required double Y { get; set; }

    public required double VelocityX { get; init; }

    public required double VelocityY { get; init; }

    public required char Glyph { get; init; }

    public required Color Colour { get; init; }

    public required double Life { get; set; }

    public required double TotalLife { get; init; }

    /// <summary>How much of this particle's life is left, from 1 down to 0.</summary>
    public double Remaining => Math.Clamp(Life / TotalLife, 0, 1);
}

/// <summary>
/// The flashes, sparks and shake that make a hit feel like one.
///
/// This lives entirely in the frontend. The core reports that something was hit
/// on a given cell and how hard, and everything here is one reading of that.
/// The game plays identically with all of it switched off.
/// </summary>
public sealed class Effects
{
    private const double ParticleLife = 0.45;
    private const double ShakeDecay = 7.0;

    private readonly List<Particle> particles = [];
    private readonly SeededRandom random;
    private readonly Theme theme;

    private double shake;

    public Effects(Theme theme, int seed)
    {
        this.theme = theme;
        random = new SeededRandom(seed);
    }

    public IReadOnlyList<Particle> Particles => particles;

    /// <summary>True while there is something left to draw.</summary>
    public bool IsBusy => particles.Count > 0 || shake > 0.05;

    /// <summary>How far the map should be pushed off centre this frame.</summary>
    public (int X, int Y) ShakeOffset
    {
        get
        {
            if (shake <= 0.35) return (0, 0);

            // A whole cell either way. Anything smaller cannot be shown on a
            // grid, and anything larger tears the map away from its border.
            int magnitude = shake > 1.6 ? 2 : 1;
            return (random.Between(-magnitude, magnitude), random.Between(-magnitude, magnitude));
        }
    }

    /// <summary>Reads a turn and throws off whatever it calls for.</summary>
    public void Play(IReadOnlyList<TurnEvent> events)
    {
        ArgumentNullException.ThrowIfNull(events);

        foreach (TurnEvent turn in events)
        {
            switch (turn.Kind)
            {
                case TurnEventKind.Hit:
                    Burst(turn.Where, '*', turn.AgainstPlayer ? theme.Bad : theme.Warning, 4 + Math.Min(6, turn.Magnitude));

                    // The screen only shakes for a blow the player took, and
                    // harder for a heavier one. Shaking for every hit anywhere
                    // makes the whole game feel loose.
                    if (turn.AgainstPlayer) shake = Math.Max(shake, 0.6 + (turn.Magnitude * 0.22));
                    break;

                case TurnEventKind.Blocked:
                    Burst(turn.Where, '-', theme.TextDim, 3);
                    break;

                case TurnEventKind.Death:
                    Burst(turn.Where, 'x', turn.AgainstPlayer ? theme.Bad : theme.Monster, 14);
                    shake = Math.Max(shake, turn.AgainstPlayer ? 2.4 : 0.9);
                    break;

                case TurnEventKind.Shot:
                    Burst(turn.Where, '\'', theme.Warning, 3);
                    break;

                case TurnEventKind.Pickup:
                    Rise(turn.Where, '+', theme.Coin, 5);
                    break;

                case TurnEventKind.Heal:
                    Rise(turn.Where, '+', theme.Good, 4 + Math.Min(6, turn.Magnitude));
                    break;

                case TurnEventKind.Descend:
                    Rise(turn.Where, '.', theme.Stairs, 8);
                    break;

                default:
                    break;
            }
        }
    }

    /// <summary>Moves everything on by one frame.</summary>
    public void Update(double seconds)
    {
        if (seconds <= 0) return;

        shake = Math.Max(0, shake - (seconds * ShakeDecay));

        for (int i = particles.Count - 1; i >= 0; i--)
        {
            Particle particle = particles[i];

            particle.X += particle.VelocityX * seconds;
            particle.Y += particle.VelocityY * seconds;
            particle.Life -= seconds;

            if (particle.Life <= 0) particles.RemoveAt(i);
        }
    }

    public void Clear()
    {
        particles.Clear();
        shake = 0;
    }

    /// <summary>Throws particles out in every direction, for a hit.</summary>
    private void Burst(Position where, char glyph, Color colour, int count)
    {
        for (int i = 0; i < count; i++)
        {
            double angle = random.NextDouble() * Math.Tau;
            double speed = 2.0 + (random.NextDouble() * 5.0);

            Add(where, glyph, colour, Math.Cos(angle) * speed, Math.Sin(angle) * speed);
        }
    }

    /// <summary>Floats particles upward, for something gained.</summary>
    private void Rise(Position where, char glyph, Color colour, int count)
    {
        for (int i = 0; i < count; i++)
        {
            double drift = (random.NextDouble() - 0.5) * 2.5;

            Add(where, glyph, colour, drift, -1.5 - (random.NextDouble() * 2.0));
        }
    }

    private void Add(Position where, char glyph, Color colour, double velocityX, double velocityY)
    {
        double life = ParticleLife * (0.6 + random.NextDouble());

        particles.Add(new Particle
        {
            X = where.X,
            Y = where.Y,
            VelocityX = velocityX,
            VelocityY = velocityY,
            Glyph = glyph,
            Colour = colour,
            Life = life,
            TotalLife = life,
        });
    }
}
