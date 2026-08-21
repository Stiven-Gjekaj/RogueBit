namespace RogueBit.Route;

using RogueBit.Core;
using RogueBit.Core.Map;
using RogueBit.Core.Pathing;
using RogueBit.Core.Saves;

/// <summary>A walk that was found, and the run it starts from.</summary>
/// <param name="Seed">The dungeon it was found on.</param>
/// <param name="Save">The run as it stands at the first key of the walk.</param>
/// <param name="Keys">The keys to send, in order.</param>
/// <param name="LeadTurns">How many turns were played before the save.</param>
/// <param name="Doorway">The doorway the walk goes through.</param>
/// <param name="Trap">The trap the walk sets off.</param>
public sealed record Route(
    int Seed,
    SaveData Save,
    IReadOnlyList<char> Keys,
    int LeadTurns,
    Position Doorway,
    Position Trap);

/// <summary>
/// Finds a short walk that shows what the game can do, and writes the run it
/// starts from.
///
/// The recording is made by replaying keys into the real window with xdotool,
/// and xdotool drops keys over a long replay. So the walk is split in two. The
/// long approach is played here, against the rules, and saved. Only the last
/// twenty or so steps are handed to xdotool, and the window picks the run up
/// with --continue.
///
/// A walk is only offered once it has been played through from the save and
/// seen to do what it claims: through a doorway, onto a trap, and at least one
/// step taken corner first.
/// </summary>
public sealed class RouteFinder
{
    private readonly int keys;
    private readonly int lead;
    private readonly int after;
    private readonly int explored;

    /// <param name="keys">How many keys the walk may be, at most.</param>
    /// <param name="lead">How many steps to start ahead of the doorway.</param>
    /// <param name="after">How many steps to keep walking once the trap has gone off.</param>
    /// <param name="explored">
    /// How much of the floor has to be walked before the recording starts, as
    /// a percentage. A run recorded from its first turn is a screen of black
    /// with a dot on it, which is a picture of nothing having happened yet.
    /// </param>
    public RouteFinder(int keys = 26, int lead = 5, int after = 8, int explored = 30)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(keys, 4);
        ArgumentOutOfRangeException.ThrowIfNegative(lead);
        ArgumentOutOfRangeException.ThrowIfNegative(after);
        ArgumentOutOfRangeException.ThrowIfNegative(explored);

        this.keys = keys;
        this.lead = lead;
        this.after = after;
        this.explored = explored;
    }

    /// <summary>Which key walks one step. These are the roguelike bindings.</summary>
    public static char KeyFor(Position step)
    {
        if (step == Directions.North) return 'k';
        if (step == Directions.South) return 'j';
        if (step == Directions.West) return 'h';
        if (step == Directions.East) return 'l';
        if (step == Directions.NorthWest) return 'y';
        if (step == Directions.NorthEast) return 'u';
        if (step == Directions.SouthWest) return 'b';
        if (step == Directions.SouthEast) return 'n';

        throw new ArgumentOutOfRangeException(nameof(step), step, "That is not one step.");
    }

    /// <summary>Looks for a walk on one seed, or returns nothing when it has none.</summary>
    public Route? Find(int seed)
    {
        Run run = new(seed);
        if (!Explore(run)) return null;

        // The floor is walked once and then handed to every candidate as a
        // save, rather than walked again for each of them. Restoring is exact,
        // so each candidate starts from the same floor in the same state.
        SaveData ready = RunSerialiser.Capture(run);

        foreach ((Position door, Position trap) in Pairs(run.Map))
        {
            if (Try(ready, door, trap) is { } route) return route;
        }

        return null;
    }

    /// <summary>
    /// Walks the rooms until enough of the floor has been shown.
    ///
    /// This only ever runs on floor one, which is built out of rooms joined by
    /// corridors, so their centres are a route that covers the place. A cave
    /// floor has no rooms and would be recorded some other way.
    /// </summary>
    private bool Explore(Run run)
    {
        foreach (Rectangle room in run.Map.Rooms.OrderBy(r => r.Centre.ChebyshevDistanceTo(run.Map.Entrance)))
        {
            if (ExploredPercent(run.Map) >= explored) return true;
            if (!run.Map.IsWalkable(room.Centre)) continue;

            IReadOnlyList<Position> path = PathFinder.Find(run.Map, run.Player.Position, room.Centre);
            if (path.Count == 0) continue;
            if (!Approach(run, path)) return false;
        }

        return ExploredPercent(run.Map) >= explored;
    }

    /// <summary>
    /// Every doorway and armed trap on the floor, the pairs that are close
    /// together first. A short hop between the two keeps the whole walk inside
    /// the number of keys xdotool can be trusted with.
    /// </summary>
    private static IEnumerable<(Position Door, Position Trap)> Pairs(DungeonMap map)
    {
        List<Position> doors = [];
        List<Position> traps = [];

        foreach (Position cell in map.WalkableCells())
        {
            if (map[cell] == TileKind.Door) doors.Add(cell);
            if (map[cell] == TileKind.TrapArmed) traps.Add(cell);
        }

        return from door in doors
               from trap in traps
               orderby door.ChebyshevDistanceTo(trap), door.X, door.Y, trap.X, trap.Y
               select (door, trap);
    }

    private Route? Try(SaveData ready, Position door, Position trap)
    {
        Run run = RunSerialiser.Restore(ready);
        DungeonMap map = run.Map;

        List<Position> walk = [.. PathFinder.Find(map, run.Player.Position, door)];
        if (walk.Count == 0) return null;

        int doorIndex = walk.Count - 1;

        List<Position> toTrap = [.. PathFinder.Find(map, door, trap)];
        if (toTrap.Count == 0) return null;
        walk.AddRange(toTrap);

        // Stopping the instant the trap goes off reads as the recording being
        // cut short, so the walk carries on towards the stairs for a moment.
        walk.AddRange(PathFinder.Find(map, trap, map.StairsDown).Take(after));

        int first = Math.Max(0, doorIndex - lead);
        if (walk.Count - first > keys) return null;

        if (!Approach(run, walk.Take(first))) return null;

        SaveData save = RunSerialiser.Capture(run);
        List<Position> steps = [.. walk.Skip(first)];

        return Replay(save, steps)
            ? new Route(save.Seed, save, [.. Keystrokes(save, steps)], run.Turns, door, trap)
            : null;
    }

    /// <summary>How much of the walkable floor the player has been shown.</summary>
    private static int ExploredPercent(DungeonMap map)
    {
        int walkable = 0;
        int seen = 0;

        foreach (Position cell in map.WalkableCells())
        {
            walkable++;
            if (map.IsExplored(cell)) seen++;
        }

        return walkable == 0 ? 0 : seen * 100 / walkable;
    }

    /// <summary>
    /// Plays the run up to where the recording starts. A monster in the way is
    /// hit rather than walked round, which costs a turn and leaves the player
    /// where it was, so the same step is asked for again.
    /// </summary>
    private static bool Approach(Run run, IEnumerable<Position> cells)
    {
        foreach (Position cell in cells)
        {
            for (int attempt = 0; attempt < 12; attempt++)
            {
                if (run.Move(cell - run.Player.Position) == ActionResult.Refused) return false;
                if (!run.Player.IsAlive) return false;
                if (run.Player.Position == cell) break;
            }

            if (run.Player.Position != cell) return false;
        }

        return true;
    }

    /// <summary>
    /// Plays the walk out from the save and reports whether it did what it is
    /// being offered for. This is the whole point of the tool. A walk that was
    /// only planned is a walk that has not been seen to work, and the run it
    /// is played on here is restored from the same file the window will read.
    /// </summary>
    private static bool Replay(SaveData save, IReadOnlyList<Position> steps)
    {
        Run run = RunSerialiser.Restore(save);
        bool throughADoorway = false;
        bool ontoATrap = false;
        bool cornerFirst = false;

        foreach (Position cell in steps)
        {
            Position step = cell - run.Player.Position;
            bool doorway = run.Map[cell] == TileKind.Door;
            bool armed = run.Map[cell] == TileKind.TrapArmed;

            if (run.Move(step) == ActionResult.Refused) return false;

            // A monster standing on the cell is hit instead of walked onto, so
            // the rest of the walk would be aimed from the wrong place.
            if (run.Player.Position != cell) return false;
            if (!run.Player.IsAlive) return false;

            throughADoorway |= doorway;
            ontoATrap |= armed;
            cornerFirst |= step.X != 0 && step.Y != 0;
        }

        return throughADoorway && ontoATrap && cornerFirst;
    }

    private static IEnumerable<char> Keystrokes(SaveData save, IEnumerable<Position> steps)
    {
        Position at = new(save.Player.X, save.Player.Y);

        foreach (Position cell in steps)
        {
            yield return KeyFor(cell - at);
            at = cell;
        }
    }
}
