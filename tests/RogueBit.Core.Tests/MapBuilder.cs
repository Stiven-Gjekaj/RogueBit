using RogueBit.Core;
using RogueBit.Core.Map;

namespace RogueBit.Core.Tests;

/// <summary>
/// Builds a map from ASCII art inside a test.
///
/// Every test that needs a shape states that shape itself rather than reading
/// one out of a generator. A generator is a choice its author changes; a test
/// that leans on one fails the day the choice moves, for no real reason.
/// </summary>
public static class MapBuilder
{
    public const char Wall = '#';
    public const char Floor = '.';
    public const char Stairs = '>';
    public const char StairsUp = '<';
    public const char Door = '+';
    public const char TrapArmed = '~';
    public const char TrapSprung = '^';

    public static DungeonMap From(params string[] rows)
    {
        ArgumentOutOfRangeException.ThrowIfZero(rows.Length);
        int width = rows[0].Length;

        if (rows.Any(row => row.Length != width))
        {
            throw new ArgumentException("Every row must be the same length.", nameof(rows));
        }

        DungeonMap map = new(width, rows.Length);

        for (int y = 0; y < rows.Length; y++)
        {
            for (int x = 0; x < width; x++)
            {
                map[new Position(x, y)] = rows[y][x] switch
                {
                    Floor => TileKind.Floor,
                    Stairs => TileKind.StairsDown,
                    StairsUp => TileKind.StairsUp,
                    Door => TileKind.Door,
                    TrapArmed => TileKind.TrapArmed,
                    TrapSprung => TileKind.TrapSprung,
                    Wall => TileKind.Wall,
                    char other => throw new ArgumentException($"'{other}' is not a tile this builder knows."),
                };
            }
        }

        return map;
    }

    /// <summary>Renders a map back to ASCII, so a failure prints something readable.</summary>
    public static string[] Render(DungeonMap map)
    {
        string[] rows = new string[map.Height];

        for (int y = 0; y < map.Height; y++)
        {
            char[] row = new char[map.Width];
            for (int x = 0; x < map.Width; x++)
            {
                row[x] = map[new Position(x, y)] switch
                {
                    TileKind.Floor => Floor,
                    TileKind.StairsDown => Stairs,
                    TileKind.StairsUp => StairsUp,
                    TileKind.Door => Door,
                    TileKind.TrapArmed => TrapArmed,
                    TileKind.TrapSprung => TrapSprung,
                    _ => Wall,
                };
            }

            rows[y] = new string(row);
        }

        return rows;
    }
}
