namespace RogueBit.Core.Pathing;

using RogueBit.Core.Map;

/// <summary>
/// Finds the shortest walk between two cells with A*.
///
/// The search is deliberately given a blocking test rather than reading the map
/// alone. An enemy has to path round the other enemies as well as round the
/// walls, and the goal itself must stay reachable even when a body stands on
/// it, which is what lets one monster walk up to the player and attack.
/// </summary>
public static class PathFinder
{
    /// <summary>The cost of one step. Kept as a constant so the heuristic can match it exactly.</summary>
    private const int StepCost = 1;

    /// <summary>
    /// The cells to walk from <paramref name="start"/> to
    /// <paramref name="goal"/>, not counting the start. Empty when there is no
    /// walk, and empty when the two are the same cell.
    /// </summary>
    /// <param name="isBlocked">
    /// True for a cell the walker may not enter. The goal is exempt, so a
    /// walker can always reach whatever it is chasing.
    /// </param>
    public static IReadOnlyList<Position> Find(
        DungeonMap map,
        Position start,
        Position goal,
        Func<Position, bool>? isBlocked = null)
    {
        ArgumentNullException.ThrowIfNull(map);

        if (start == goal) return [];
        if (!map.IsWalkable(goal)) return [];

        PriorityQueue<Position, int> frontier = new();
        Dictionary<Position, Position> cameFrom = [];
        Dictionary<Position, int> costSoFar = new() { [start] = 0 };

        frontier.Enqueue(start, Heuristic(start, goal));

        while (frontier.TryDequeue(out Position current, out _))
        {
            if (current == goal) return Rebuild(cameFrom, start, goal);

            foreach (Position step in Directions.Cardinal)
            {
                Position next = current + step;

                if (!map.IsWalkable(next)) continue;

                // The goal may be occupied. Anything else that is blocked is
                // not a cell this walker can stand on.
                if (next != goal && isBlocked?.Invoke(next) == true) continue;

                int newCost = costSoFar[current] + StepCost;
                if (costSoFar.TryGetValue(next, out int known) && newCost >= known) continue;

                costSoFar[next] = newCost;
                cameFrom[next] = current;
                frontier.Enqueue(next, newCost + Heuristic(next, goal));
            }
        }

        return [];
    }

    /// <summary>
    /// The first step of the walk, or null when there is none. This is what an
    /// enemy asks for on its turn.
    /// </summary>
    public static Position? NextStep(
        DungeonMap map,
        Position start,
        Position goal,
        Func<Position, bool>? isBlocked = null)
    {
        IReadOnlyList<Position> path = Find(map, start, goal, isBlocked);
        return path.Count == 0 ? null : path[0];
    }

    /// <summary>
    /// Manhattan distance. It never overstates the true cost on a grid of four
    /// directions, which is what keeps the result a shortest path rather than
    /// merely a short one.
    /// </summary>
    private static int Heuristic(Position from, Position to) => from.ManhattanDistanceTo(to) * StepCost;

    private static List<Position> Rebuild(Dictionary<Position, Position> cameFrom, Position start, Position goal)
    {
        List<Position> path = [];

        for (Position at = goal; at != start; at = cameFrom[at])
        {
            path.Add(at);
        }

        path.Reverse();
        return path;
    }
}
