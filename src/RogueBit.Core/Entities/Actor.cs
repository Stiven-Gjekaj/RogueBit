namespace RogueBit.Core.Entities;

/// <summary>An entity that takes turns, deals damage and can be killed.</summary>
public abstract class Actor : Entity
{
    private int health;

    public int MaxHealth { get; private set; }

    public int Health
    {
        get => health;
        private set => health = Math.Clamp(value, 0, MaxHealth);
    }

    /// <summary>How hard this actor hits.</summary>
    public int Power { get; set; }

    /// <summary>How much damage this actor turns aside from each hit.</summary>
    public int Defence { get; set; }

    public bool IsAlive => Health > 0;

    public override bool BlocksMovement => IsAlive;

    protected Actor(Position position, int maxHealth, int power, int defence)
        : base(position)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxHealth);

        MaxHealth = maxHealth;
        health = maxHealth;
        Power = power;
        Defence = defence;
    }

    /// <summary>Takes damage and returns how much actually landed.</summary>
    public int TakeDamage(int amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);

        int before = Health;
        Health -= amount;
        return before - Health;
    }

    /// <summary>Restores health and returns how much was actually restored.</summary>
    public int Heal(int amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);

        int before = Health;
        Health += amount;
        return Health - before;
    }

    /// <summary>Raises the ceiling and fills the new room with health.</summary>
    public void RaiseMaxHealth(int amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);

        MaxHealth += amount;
        Health += amount;
    }
}
