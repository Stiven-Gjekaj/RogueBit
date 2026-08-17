namespace RogueBit.Core.Map;

/// <summary>Finds and joins the separate walkable regions of a map.</summary>
public static class MapRegions
{
    /// <summary>
    /// Groups every walkable cell into regions that reach each other by
    /// cardinal steps. A map a player can fully explore returns exactly one.
    /// </summary>
    public static List<List<Position>> Find(DungeonMap map)
    {
        HashSet<Position> seen = [];
        List<List<Position>> regions = [];

        foreach (Position start in map.WalkableCells())
        {
            if (!seen.Add(start)) continue;

            List<Position> region = [];
            Queue<Position> queue = new();
            queue.Enqueue(start);

            while (queue.Count > 0)
            {
                Position current = queue.Dequeue();
                region.Add(current);

                foreach (Position step in Directions.Cardinal)
                {
                    Position next = current + step;
                    if (!map.IsWalkable(next)) continue;
                    if (!seen.Add(next)) continue;
                    queue.Enqueue(next);
                }
            }

            regions.Add(region);
        }

        return regions;
    }

    /// <summary>
    /// Joins every region to the largest one by carving an L shaped corridor
    /// between the closest pair of cells. It repeats until one region is left,
    /// so the result cannot strand the player behind a wall.
    /// </summary>
    public static void ConnectAll(DungeonMap map)
    {
        List<List<Position>> regions = Find(map);

        while (regions.Count > 1)
        {
            regions.Sort((a, b) => b.Count.CompareTo(a.Count));
            List<Position> main = regions[0];
            List<Position> nearest = regions[1];

            (Position from, Position to) = ClosestPair(main, nearest);
            CarveCorridor(map, from, to);

            regions = Find(map);
        }
    }

    /// <summary>Carves a corridor that runs along one axis and then the other.</summary>
    public static void CarveCorridor(DungeonMap map, Position from, Position to)
    {
        int x = from.X;
        int y = from.Y;

        while (x != to.X)
        {
            x += Math.Sign(to.X - x);
            Dig(map, new Position(x, y));
        }

        while (y != to.Y)
        {
            y += Math.Sign(to.Y - y);
            Dig(map, new Position(x, y));
        }
    }

    private static void Dig(DungeonMap map, Position p)
    {
        if (!map.Contains(p)) return;
        if (map[p] == TileKind.Wall) map[p] = TileKind.Floor;
    }

    private static (Position From, Position To) ClosestPair(List<Position> a, List<Position> b)
    {
        Position bestFrom = a[0];
        Position bestTo = b[0];
        int best = int.MaxValue;

        foreach (Position from in a)
        {
            foreach (Position to in b)
            {
                int distance = from.ManhattanDistanceTo(to);
                if (distance >= best) continue;
                best = distance;
                bestFrom = from;
                bestTo = to;
            }
        }

        return (bestFrom, bestTo);
    }
}
