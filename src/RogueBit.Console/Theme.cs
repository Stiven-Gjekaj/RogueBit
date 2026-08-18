namespace RogueBit.Console;

using SadRogue.Primitives;
using RogueBit.Core;

/// <summary>
/// The colours the game draws with.
///
/// Two palettes are offered. The plain one separates its hues by lightness as
/// well as by hue, so the map still reads for a player who cannot tell red from
/// green. Nothing in the game is told apart by colour alone: every monster and
/// every item carries its own glyph as well.
/// </summary>
public sealed class Theme
{
    public required Color WallLit { get; init; }

    public required Color WallRemembered { get; init; }

    public required Color FloorLit { get; init; }

    public required Color FloorRemembered { get; init; }

    public required Color Stairs { get; init; }

    public required Color Door { get; init; }

    public required Color Trap { get; init; }

    public required Color Player { get; init; }

    public required Color Monster { get; init; }

    public required Color Boss { get; init; }

    public required Color Coin { get; init; }

    public required Color Potion { get; init; }

    public required Color Equipment { get; init; }

    public required Color Text { get; init; }

    public required Color TextDim { get; init; }

    public required Color Good { get; init; }

    public required Color Bad { get; init; }

    public required Color Warning { get; init; }

    public static readonly Color Background = new(10, 12, 14);

    /// <summary>The default palette.</summary>
    public static Theme Standard { get; } = new()
    {
        WallLit = new Color(122, 130, 138),
        WallRemembered = new Color(52, 57, 62),
        FloorLit = new Color(176, 184, 190),
        FloorRemembered = new Color(70, 76, 82),
        Stairs = new Color(240, 200, 90),
        Door = new Color(186, 140, 96),
        Trap = new Color(206, 92, 120),
        Player = new Color(90, 214, 226),
        Monster = new Color(126, 196, 108),
        Boss = new Color(226, 96, 78),
        Coin = new Color(238, 190, 68),
        Potion = new Color(226, 110, 150),
        Equipment = new Color(150, 170, 240),
        Text = new Color(222, 232, 236),
        TextDim = new Color(126, 142, 148),
        Good = new Color(126, 208, 150),
        Bad = new Color(238, 122, 104),
        Warning = new Color(238, 190, 68),
    };

    /// <summary>
    /// A palette that does not ask the player to tell red from green. The
    /// monsters move to yellow and the dangerous things to blue, and every pair
    /// that has to be told apart also differs in lightness.
    /// </summary>
    public static Theme ColourBlind { get; } = new()
    {
        WallLit = new Color(126, 132, 138),
        WallRemembered = new Color(54, 58, 62),
        FloorLit = new Color(180, 186, 192),
        FloorRemembered = new Color(72, 78, 84),
        Stairs = new Color(248, 232, 120),
        Door = new Color(196, 166, 120),
        Trap = new Color(160, 190, 250),
        Player = new Color(120, 220, 240),
        Monster = new Color(232, 180, 60),
        Boss = new Color(120, 150, 250),
        Coin = new Color(250, 240, 150),
        Potion = new Color(180, 200, 255),
        Equipment = new Color(140, 170, 210),
        Text = new Color(228, 234, 238),
        TextDim = new Color(130, 144, 150),
        Good = new Color(150, 200, 250),
        Bad = new Color(250, 200, 90),
        Warning = new Color(248, 232, 120),
    };

    /// <summary>Reads the palette the player asked for on the command line.</summary>
    public static Theme For(bool colourBlind) => colourBlind ? ColourBlind : Standard;

    public Color ForMessage(MessageKind kind) => kind switch
    {
        MessageKind.Good => Good,
        MessageKind.Bad => Bad,
        MessageKind.Warning => Warning,
        _ => Text,
    };
}
