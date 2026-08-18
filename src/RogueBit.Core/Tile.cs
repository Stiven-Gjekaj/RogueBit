namespace RogueBit.Core;

/// <summary>What occupies one cell of the map.</summary>
public enum TileKind
{
    Wall,
    Floor,
    StairsDown,

    /// <summary>
    /// The way back to the floor above. Every floor below the first has one,
    /// where the player arrives.
    /// </summary>
    StairsUp,
}

/// <summary>
/// The properties of one cell. Walkability and transparency are read from the
/// kind, so the two can never disagree with each other.
/// </summary>
public static class TileRules
{
    /// <summary>True when an entity can stand on this tile.</summary>
    public static bool IsWalkable(this TileKind kind) =>
        kind is TileKind.Floor or TileKind.StairsDown or TileKind.StairsUp;

    /// <summary>True when light passes through this tile.</summary>
    public static bool IsTransparent(this TileKind kind) =>
        kind is TileKind.Floor or TileKind.StairsDown or TileKind.StairsUp;
}
