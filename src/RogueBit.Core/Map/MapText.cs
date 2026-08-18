namespace RogueBit.Core.Map;

/// <summary>
/// A floor as text.
///
/// The glyphs live here rather than in whoever is drawing, because there is
/// more than one of those: the window, the banner tool, and the command that
/// prints a floor without playing it. Three copies of the same table is three
/// chances for a new tile to be drawn as a wall in one of them, which has
/// already happened once.
///
/// This is not the save format. That has its own characters on purpose, so a
/// file written last year still reads after somebody changes what a door looks
/// like.
/// </summary>
public static class MapText
{
    /// <summary>The character this game draws a tile with.</summary>
    public static char Glyph(TileKind kind) => kind switch
    {
        TileKind.Floor => '.',
        TileKind.StairsDown => '>',
        TileKind.StairsUp => '<',
        TileKind.Door => '+',
        TileKind.TrapSprung => '^',

        // An armed trap is the ground it is hidden under. A trap anybody can
        // see is not a trap, and that is true wherever it is being drawn.
        TileKind.TrapArmed => '.',
        _ => '#',
    };

    /// <summary>Every row of a floor, as text, with nothing hidden.</summary>
    public static string[] Render(DungeonMap map)
    {
        ArgumentNullException.ThrowIfNull(map);

        string[] rows = new string[map.Height];

        for (int y = 0; y < map.Height; y++)
        {
            char[] row = new char[map.Width];

            for (int x = 0; x < map.Width; x++)
            {
                row[x] = Glyph(map[new Position(x, y)]);
            }

            rows[y] = new string(row);
        }

        return rows;
    }
}
