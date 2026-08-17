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

    /// <summary>How hard this actor hits before anything it carries is counted.</summary>
    public int BasePower { get; set; }

    /// <summary>How much this actor turns aside before anything it wears is counted.</summary>
    public int BaseDefence { get; set; }

    /// <summary>
    /// How hard this actor actually hits. Equipment is added by whoever can
    /// carry it, so combat has one number to ask for and cannot miss a bonus.
    /// </summary>
    public virtual int Power => BasePower;

    /// <summary>How much this actor actually turns aside, equipment included.</summary>
    public virtual int Defence => BaseDefence;

    public bool IsAlive => Health > 0;

    public override bool BlocksMovement => IsAlive;

    protected Actor(Position position, int maxHealth, int power, int defence)
        : base(position)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maxHealth);

        MaxHealth = maxHealth;
        health = maxHealth;
        BasePower = power;
        BaseDefence = defence;
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

    /// <summary>Puts an actor back to full health, used when a run restarts.</summary>
    internal void RestoreToFull() => Health = MaxHealth;
}
