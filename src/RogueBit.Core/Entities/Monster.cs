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

    public Monster(Position position, int maxHealth, int power, int defence)
        : base(position, maxHealth, power, defence)
    {
    }

    /// <summary>
    /// True when a boss has dropped below half health. A boss hits harder from
    /// that point on, which is the whole of its second phase.
    /// </summary>
    public bool IsEnraged => Behaviour == MonsterBehaviour.Boss && Health * 2 <= MaxHealth;

    /// <summary>How many steps this monster takes for each player turn.</summary>
    public int Speed => Behaviour == MonsterBehaviour.Swift ? 2 : 1;

    public int EffectivePower => IsEnraged ? Power * 2 : Power;
}
