namespace RogueBit.Core.Entities;

using System.Diagnostics.CodeAnalysis;

/// <summary>The character the player moves.</summary>
public sealed class Player : Actor
{
    public int Coins { get; private set; }

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
