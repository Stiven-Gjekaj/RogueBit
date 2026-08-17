namespace RogueBit.Core.Vision;

using RogueBit.Core.Map;

/// <summary>
/// Works out what the player can see, by recursive shadowcasting.
///
/// The area round the viewer is cut into eight octants. Each octant is scanned
/// row by row outwards, carrying a slope for its left and right edge. A wall
/// narrows that wedge: the scan recurses into the part still in the light and
/// abandons the part behind the wall. Every cell is therefore touched at most
/// once per octant, and a wall casts a shadow with a straight edge.
///
/// This replaces the ray casting the old code called shadowcasting. Casting a
/// ray at every cell in range is not the same algorithm: it visits the cells
/// near the viewer once per ray, and it leaves holes in a diagonal wall,
/// because no ray happens to land on them.
/// </summary>
public static class FieldOfView
{
    /// <summary>
    /// Eight transforms, one per octant, that map the scan's own coordinates
    /// onto the map. Writing the scan once and turning it eight ways is what
    /// keeps the algorithm to a single readable method.
    /// </summary>
    private static readonly (int Xx, int Xy, int Yx, int Yy)[] Octants =
    [
        (1, 0, 0, 1), (0, 1, 1, 0), (0, -1, 1, 0), (-1, 0, 0, 1),
        (-1, 0, 0, -1), (0, -1, -1, 0), (0, 1, -1, 0), (1, 0, 0, -1),
    ];

    /// <summary>
    /// Marks every cell the viewer can see from <paramref name="origin"/>
    /// within <paramref name="radius"/>. Cells already lit are cleared first.
    /// </summary>
    public static void Compute(DungeonMap map, Position origin, int radius)
    {
        ArgumentNullException.ThrowIfNull(map);
        if (radius < 0) throw new ArgumentOutOfRangeException(nameof(radius));

        map.ClearVisible();

        // The viewer always sees the cell it stands on, wall or not.
        map.MarkVisible(origin);

        foreach ((int xx, int xy, int yx, int yy) in Octants)
        {
            Scan(map, origin, radius, row: 1, start: 1.0, end: 0.0, xx, xy, yx, yy);
        }
    }

    private static void Scan(
        DungeonMap map,
        Position origin,
        int radius,
        int row,
        double start,
        double end,
        int xx,
        int xy,
        int yx,
        int yy)
    {
        if (start < end) return;

        double nextStart = 0.0;
        bool blocked = false;

        for (int distance = row; distance <= radius && !blocked; distance++)
        {
            // The scan runs outwards along the negative axis, so that a larger
            // slope means further to the left. Getting this sign wrong mirrors
            // every octant and puts the shadows on the wrong side.
            int deltaY = -distance;

            for (int deltaX = -distance; deltaX <= 0; deltaX++)
            {
                // The slopes of this cell's leading and trailing corner.
                double leftSlope = (deltaX - 0.5) / (deltaY + 0.5);
                double rightSlope = (deltaX + 0.5) / (deltaY - 0.5);

                if (start < rightSlope) continue;
                if (end > leftSlope) break;

                Position cell = new(
                    origin.X + (deltaX * xx) + (deltaY * xy),
                    origin.Y + (deltaX * yx) + (deltaY * yy));

                // Round the field off, so vision is a disc and not a square.
                if ((deltaX * deltaX) + (deltaY * deltaY) <= radius * radius)
                {
                    map.MarkVisible(cell);
                }

                bool isWall = !map.IsTransparent(cell);

                if (blocked)
                {
                    if (isWall)
                    {
                        // Still inside the same wall; keep widening it.
                        nextStart = rightSlope;
                        continue;
                    }

                    // Out the far side of a wall. The light starts again here.
                    blocked = false;
                    start = nextStart;
                }
                else if (isWall && distance < radius)
                {
                    // A wall begins. Follow the light to its left, then let the
                    // loop carry on scanning the shadow's far side.
                    blocked = true;
                    Scan(map, origin, radius, distance + 1, start, leftSlope, xx, xy, yx, yy);
                    nextStart = rightSlope;
                }
            }
        }
    }
}
