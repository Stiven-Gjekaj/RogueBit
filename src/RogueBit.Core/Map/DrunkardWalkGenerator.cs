namespace RogueBit.Core.Map;

/// <summary>
/// Carves a cave by walking at random and digging out every cell it stands on.
///
/// The walk restarts from a cell it has already dug whenever it runs into the
/// edge, which keeps the cave in one piece and stops it pressing itself flat
/// against a wall. It digs until it reaches the coverage asked for, or until it
/// has taken the largest number of steps allowed, whichever comes first.
/// </summary>
public sealed class DrunkardWalkGenerator : IDungeonGenerator
{
    private readonly double coverage;

    public string Name => "cave";

    public DrunkardWalkGenerator(double coverage = 0.45)
    {
        if (coverage is <= 0 or >= 1) throw new ArgumentOutOfRangeException(nameof(coverage));
        this.coverage = coverage;
    }

    public DungeonMap Generate(int width, int height, SeededRandom random)
    {
        DungeonMap map = new(width, height);

        Rectangle inside = new(1, 1, Math.Max(1, width - 2), Math.Max(1, height - 2));
        int target = Math.Max(1, (int)(inside.Area * coverage));
        int stepLimit = inside.Area * 40;

        Position digger = inside.Centre;
        List<Position> dug = [];

        map[digger] = TileKind.Floor;
        dug.Add(digger);

        int steps = 0;
        while (dug.Count < target && steps < stepLimit)
        {
            steps++;
            Position next = digger + random.Pick(Directions.Cardinal);

            if (!inside.Contains(next))
            {
                // Walking off the edge would flatten the cave against the wall.
                // Jump back to somewhere already dug and carry on from there.
                digger = random.Pick(dug);
                continue;
            }

            digger = next;
            if (map[digger] == TileKind.Wall)
            {
                map[digger] = TileKind.Floor;
                dug.Add(digger);
            }
        }

        MapRegions.ConnectAll(map);

        map.Rooms = [];
        map.Entrance = dug[0];
        map.StairsDown = FurthestFrom(map, map.Entrance);
        map[map.StairsDown] = TileKind.StairsDown;

        return map;
    }

    /// <summary>
    /// The walkable cell that takes the most steps to reach. A cave has no
    /// rooms to put the stairs in, so putting them as far away as possible is
    /// what makes the floor worth crossing.
    /// </summary>
    private static Position FurthestFrom(DungeonMap map, Position start)
    {
        Dictionary<Position, int> distance = new() { [start] = 0 };
        Queue<Position> queue = new();
        queue.Enqueue(start);

        Position furthest = start;

        while (queue.Count > 0)
        {
            Position current = queue.Dequeue();

            foreach (Position step in Directions.Cardinal)
            {
                Position next = current + step;
                if (!map.IsWalkable(next) || distance.ContainsKey(next)) continue;

                distance[next] = distance[current] + 1;
                if (distance[next] > distance[furthest]) furthest = next;
                queue.Enqueue(next);
            }
        }

        return furthest;
    }
}
