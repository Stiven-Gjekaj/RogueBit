namespace RogueBit.Core.Entities;

/// <summary>How a monster decides what to do on its turn.</summary>
public enum MonsterBehaviour
{
    /// <summary>Walks up to the player and hits it.</summary>
    Chaser,

    /// <summary>Chases, but takes two steps for every one the player takes.</summary>
    Swift,

    /// <summary>Keeps its distance and shoots along a clear line.</summary>
    Archer,

    /// <summary>Chases while it is whole, and runs once it is hurt.</summary>
    Scavenger,

    /// <summary>Chases, and on seeing the player calls everything nearby to it.</summary>
    Howler,

    /// <summary>Chases, hits hard, and hits harder once it is hurt.</summary>
    Boss,
}

/// <summary>Anything in the dungeon that is trying to kill the player.</summary>
public sealed class Monster : Actor
{
    public required MonsterBehaviour Behaviour { get; init; }

    /// <summary>How far away this monster notices the player.</summary>
    public required int AggroRadius { get; init; }

    /// <summary>How far this monster can shoot, zero for one that cannot.</summary>
    public int Range { get; init; }

    /// <summary>What killing this monster is worth.</summary>
    public required int CoinReward { get; init; }

    /// <summary>
    /// True when something has roused this monster, so it hunts the player
    /// whatever the distance between them.
    ///
    /// It is deliberately not saved. A save holds where everything stands, not
    /// what it was thinking, and a resumed floor comes back unlit for the same
    /// reason. A monster that was chasing you across a room forgets once, and
    /// takes it up again on the next turn if you are still anywhere near.
    ///
    /// Nothing clears it. A monster that has heard you does not unhear you.
    /// </summary>
    public bool IsAlerted { get; set; }

    public Monster(Position position, int maxHealth, int power, int defence)
        : base(position, maxHealth, power, defence)
    {
    }

    /// <summary>
    /// True when a boss has dropped below half health. A boss hits harder from
    /// that point on, which is the whole of its second phase.
    /// </summary>
    public bool IsEnraged => Behaviour == MonsterBehaviour.Boss && Health * 2 <= MaxHealth;

    /// <summary>
    /// True when a scavenger has taken enough to break and run. It is the
    /// mirror of a boss going into its second phase: the same half of the same
    /// health bar, read the opposite way.
    /// </summary>
    public bool IsFleeing => Behaviour == MonsterBehaviour.Scavenger && Health * 2 < MaxHealth;

    /// <summary>How many steps this monster takes for each player turn.</summary>
    public int Speed => Behaviour == MonsterBehaviour.Swift ? 2 : 1;

    public int EffectivePower => IsEnraged ? Power * 2 : Power;
}
