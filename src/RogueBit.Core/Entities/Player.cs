namespace RogueBit.Core.Entities;

using System.Diagnostics.CodeAnalysis;
using RogueBit.Core.Items;

/// <summary>The character the player moves.</summary>
public sealed class Player : Actor
{
    public int Coins { get; private set; }

    /// <summary>What the player is carrying and wearing.</summary>
    public Inventory Inventory { get; } = new();

    /// <summary>Power, with the wielded weapon counted.</summary>
    public override int Power => BasePower + Inventory.TotalPower;

    /// <summary>Defence, with the worn armour counted.</summary>
    public override int Defence => BaseDefence + Inventory.TotalDefence;

    [SetsRequiredMembers]
    public Player(Position position)
        : base(position, maxHealth: 20, power: 4, defence: 1)
    {
        Glyph = '@';
        Name = "you";
    }

    public void TakeCoins(int amount)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(amount);
        Coins += amount;
    }
}
