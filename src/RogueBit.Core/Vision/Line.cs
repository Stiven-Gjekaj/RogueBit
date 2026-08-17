namespace RogueBit.Core.Vision;

using RogueBit.Core.Map;

/// <summary>Walks the cells between two points.</summary>
public static class Line
{
    /// <summary>
    /// The cells from <paramref name="from"/> to <paramref name="to"/> along a
    /// Bresenham line, including both ends.
    /// </summary>
    public static IEnumerable<Position> Between(Position from, Position to)
    {
        int x = from.X;
        int y = from.Y;

        int dx = Math.Abs(to.X - x);
        int dy = -Math.Abs(to.Y - y);
        int stepX = x < to.X ? 1 : -1;
        int stepY = y < to.Y ? 1 : -1;
        int error = dx + dy;

        while (true)
        {
            yield return new Position(x, y);

            if (x == to.X && y == to.Y) yield break;

            int doubled = error * 2;

            if (doubled >= dy)
            {
                error += dy;
                x += stepX;
            }

            if (doubled <= dx)
            {
                error += dx;
                y += stepY;
            }
        }
    }

    /// <summary>
    /// True when nothing opaque stands between the two cells. The ends
    /// themselves are not tested, so a shooter inside a doorway can still fire
    /// out of it and a target against a wall can still be hit.
    /// </summary>
    public static bool IsClear(DungeonMap map, Position from, Position to)
    {
        ArgumentNullException.ThrowIfNull(map);

        foreach (Position cell in Between(from, to))
        {
            if (cell == from || cell == to) continue;
            if (!map.IsTransparent(cell)) return false;
        }

        return true;
    }
}
