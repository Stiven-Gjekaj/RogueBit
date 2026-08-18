namespace RogueBit.Core;

using RogueBit.Core.Entities;
using RogueBit.Core.Items;
using RogueBit.Core.Map;

/// <summary>
/// One floor of the dungeon: the ground, and everything standing or lying on
/// it.
///
/// Not to be confused with <see cref="TileKind.Floor"/>, which is one cell of
/// walkable ground. This is a whole depth.
///
/// The three parts are held together because they are one thing. Kept in three
/// fields beside each other, a map belonging to one depth and monsters
/// belonging to another is a state that can be written by accident, and a run
/// that reaches it cannot be reasoned about at all.
/// </summary>
public sealed class Floor
{
    public required DungeonMap Map { get; init; }

    /// <summary>What is alive on this floor.</summary>
    public List<Monster> Monsters { get; } = [];

    /// <summary>What is lying on the ground of this floor.</summary>
    public List<Item> Items { get; } = [];
}
