namespace RogueBit.Core.Items;

using System.Diagnostics.CodeAnalysis;
using RogueBit.Core.Entities;

/// <summary>What an item does when it is picked up or used.</summary>
public enum ItemKind
{
    /// <summary>Adds to the score the moment it is stepped on.</summary>
    Coin,

    /// <summary>Restores health when it is drunk.</summary>
    Potion,

    /// <summary>Adds to the power of whoever wields it.</summary>
    Weapon,

    /// <summary>Adds to the defence of whoever wears it.</summary>
    Armour,
}

/// <summary>Something on the floor that the player can pick up.</summary>
public sealed class Item : Entity
{
    public required ItemKind Kind { get; init; }

    /// <summary>What a coin is worth, and nothing for anything else.</summary>
    public int Value { get; init; }

    /// <summary>What a potion restores, and nothing for anything else.</summary>
    public int Healing { get; init; }

    /// <summary>What a weapon adds to power, or armour adds to defence.</summary>
    public int Bonus { get; init; }

    /// <summary>True when this item goes into a slot rather than being used up.</summary>
    public bool IsEquipment => Kind is ItemKind.Weapon or ItemKind.Armour;

    /// <summary>True when this item is taken the moment it is walked over.</summary>
    public bool IsPickedUpByWalkingOver => Kind is ItemKind.Coin;

    [SetsRequiredMembers]
    private Item(Position position, ItemKind kind, char glyph, string name)
        : base(position)
    {
        Kind = kind;
        Glyph = glyph;
        Name = name;
    }

    public static Item Coin(Position position, int value = 1) =>
        new(position, ItemKind.Coin, '$', "a coin") { Value = value };

    public static Item Potion(Position position, int healing = 8) =>
        new(position, ItemKind.Potion, '!', "a healing potion") { Healing = healing };

    public static Item Weapon(Position position, string name, int bonus) =>
        new(position, ItemKind.Weapon, '/', name) { Bonus = bonus };

    public static Item Armour(Position position, string name, int bonus) =>
        new(position, ItemKind.Armour, '[', name) { Bonus = bonus };
}
