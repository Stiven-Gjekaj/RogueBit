namespace RogueBit.Console;

using SadConsole.Input;
using RogueBit.Core;

/// <summary>What a key asks for when the game is waiting for a move.</summary>
public enum PlayAction
{
    /// <summary>Stand still and let the monsters close in.</summary>
    Wait,

    /// <summary>Take what is underfoot.</summary>
    PickUp,

    /// <summary>Go up or down, whichever staircase this is.</summary>
    TakeStairs,

    /// <summary>Open the pack.</summary>
    OpenPack,

    /// <summary>Read back through the message log.</summary>
    OpenLog,

    /// <summary>Write the run out without leaving it.</summary>
    Save,

    /// <summary>Start the same seed again from the top.</summary>
    Restart,
}

/// <summary>
/// Which keys do what.
///
/// Three layouts are offered at once rather than being chosen in a menu: the
/// arrows, WASD, and the roguelike habit of hjkl. A player reaches for whichever
/// they already know and it works.
///
/// The two tables are here together, and a test reads them both, because a key
/// that is in both of them does whichever the game asks about first and the
/// other thing silently never happens.
/// </summary>
public static class Keybindings
{
    public static IReadOnlyList<(Keys Key, Position Step)> Movement { get; } =
    [
        (Keys.Up, Directions.North),
        (Keys.W, Directions.North),
        (Keys.K, Directions.North),
        (Keys.NumPad8, Directions.North),

        (Keys.Down, Directions.South),
        (Keys.S, Directions.South),
        (Keys.J, Directions.South),
        (Keys.NumPad2, Directions.South),

        (Keys.Left, Directions.West),
        (Keys.A, Directions.West),
        (Keys.H, Directions.West),
        (Keys.NumPad4, Directions.West),

        (Keys.Right, Directions.East),
        (Keys.D, Directions.East),
        (Keys.L, Directions.East),
        (Keys.NumPad6, Directions.East),

        // The diagonals. The numeric keypad has them where they point, and
        // yubn is the roguelike habit, sitting round hjkl the same way. There
        // is no arrow key or WASD key for a diagonal, so those two layouts
        // stay on four directions and nothing is taken away from them.
        (Keys.Y, Directions.NorthWest),
        (Keys.NumPad7, Directions.NorthWest),

        (Keys.U, Directions.NorthEast),
        (Keys.NumPad9, Directions.NorthEast),

        (Keys.B, Directions.SouthWest),
        (Keys.NumPad1, Directions.SouthWest),

        (Keys.N, Directions.SouthEast),
        (Keys.NumPad3, Directions.SouthEast),
    ];

    /// <summary>Everything else a key can ask for while the game is being played.</summary>
    public static IReadOnlyList<(Keys Key, PlayAction Action)> Actions { get; } =
    [
        (Keys.OemPeriod, PlayAction.Wait),
        (Keys.NumPad5, PlayAction.Wait),

        (Keys.G, PlayAction.PickUp),
        (Keys.OemComma, PlayAction.TakeStairs),
        (Keys.I, PlayAction.OpenPack),
        (Keys.M, PlayAction.OpenLog),
        (Keys.R, PlayAction.Restart),

        // Not S. S walks south for anybody on WASD, and the game asks about
        // walking first, so a save bound to S was never once reached.
        (Keys.F5, PlayAction.Save),
    ];
}
