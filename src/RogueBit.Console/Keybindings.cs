namespace RogueBit.Console;

using SadConsole.Input;
using RogueBit.Core;

/// <summary>
/// Which keys move the player.
///
/// Three layouts are offered at once rather than being chosen in a menu: the
/// arrows, WASD, and the roguelike habit of hjkl. A player reaches for whichever
/// they already know and it works.
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
    ];
}
