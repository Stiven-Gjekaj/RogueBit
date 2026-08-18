namespace RogueBit.Core.Saves;

/// <summary>
/// A run written down.
///
/// Every floor is stored as tiles rather than as the seed it was grown from.
/// Regenerating them would be smaller, but it would tie every save to the exact
/// behaviour of the generator, and a save that stops loading because a
/// generator was tuned is a save that was never really written down. This costs
/// about three kilobytes a floor and survives any change to how floors are
/// built.
/// </summary>
public sealed record SaveData
{
    /// <summary>
    /// The shape of this file. A load refuses a number it does not know rather
    /// than reading old fields into new meanings.
    ///
    /// Version 1 held one floor, because a run could only go down. It cannot be
    /// read into this shape: it does not record where the player came in, so
    /// the floor it holds has nowhere to put the stairs back up.
    /// </summary>
    public const int CurrentVersion = 2;

    public int Version { get; init; } = CurrentVersion;

    public required int Seed { get; init; }

    /// <summary>The floor the player is standing on.</summary>
    public required int Depth { get; init; }

    /// <summary>The deepest floor reached, which is what the score is paid on.</summary>
    public required int DeepestDepth { get; init; }

    public required int Turns { get; init; }

    /// <summary>The state of the dice, so the run carries on rather than restarting.</summary>
    public required ulong RandomState { get; init; }

    public required ulong RandomIncrement { get; init; }

    public required SavedPlayer Player { get; init; }

    /// <summary>
    /// Every floor the run has been on. All of them, not only the one being
    /// stood on, because the player can walk back up into any of them.
    /// </summary>
    public required IReadOnlyList<SavedFloor> Floors { get; init; }

    public required IReadOnlyList<SavedItem> Carried { get; init; }

    public SavedItem? Weapon { get; init; }

    public SavedItem? Armour { get; init; }

    public required IReadOnlyList<SavedMessage> Log { get; init; }
}

/// <summary>One floor, and everything standing or lying on it.</summary>
public sealed record SavedFloor
{
    public required int Depth { get; init; }

    public required int Width { get; init; }

    public required int Height { get; init; }

    /// <summary>The floor, one character per cell, row by row.</summary>
    public required string Tiles { get; init; }

    /// <summary>
    /// Which cells the player has seen, one character per cell, row by row.
    /// A string keeps the file readable and small next to a list of booleans.
    /// </summary>
    public required string Explored { get; init; }

    /// <summary>Where the player arrives coming down, and where the stairs up are.</summary>
    public required int EntranceX { get; init; }

    public required int EntranceY { get; init; }

    public required int StairsX { get; init; }

    public required int StairsY { get; init; }

    public required IReadOnlyList<SavedMonster> Monsters { get; init; }

    public required IReadOnlyList<SavedItem> Items { get; init; }
}

public sealed record SavedPlayer(int X, int Y, int Health, int MaxHealth, int Coins);

public sealed record SavedMonster(
    int X,
    int Y,
    int Health,
    int MaxHealth,
    int Power,
    int Defence,
    string Name,
    char Glyph,
    string Behaviour,
    int AggroRadius,
    int Range,
    int CoinReward);

public sealed record SavedItem(int X, int Y, string Kind, string Name, char Glyph, int Value, int Healing, int Bonus);

public sealed record SavedMessage(string Text, string Kind, int Count);
