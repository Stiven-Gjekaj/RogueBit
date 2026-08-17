namespace RogueBit.Core;

/// <summary>Something worth showing that happened during a turn.</summary>
public enum TurnEventKind
{
    /// <summary>A blow landed.</summary>
    Hit,

    /// <summary>A blow was turned aside by armour.</summary>
    Blocked,

    /// <summary>Something died.</summary>
    Death,

    /// <summary>An arrow flew.</summary>
    Shot,

    /// <summary>Something was picked up.</summary>
    Pickup,

    /// <summary>A potion was drunk.</summary>
    Heal,

    /// <summary>The player went down a floor.</summary>
    Descend,
}

/// <summary>
/// Where something happened, so a frontend can draw it.
///
/// The core does not animate anything. It reports what happened and where, and
/// leaves entirely to the frontend whether that becomes a flash, a shake, or
/// nothing at all. This is the narrowest thing the two can share without the
/// rules learning about drawing.
/// </summary>
/// <param name="Kind">What happened.</param>
/// <param name="Where">The cell it happened on.</param>
/// <param name="Magnitude">
/// How big it was, in points of damage or healing. Zero for anything that has
/// no size, which lets a frontend shake harder for a heavier blow.
/// </param>
/// <param name="AgainstPlayer">True when the player was on the receiving end.</param>
public readonly record struct TurnEvent(
    TurnEventKind Kind,
    Position Where,
    int Magnitude = 0,
    bool AgainstPlayer = false);
