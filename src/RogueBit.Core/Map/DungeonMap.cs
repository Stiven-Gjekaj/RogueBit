namespace RogueBit.Core.Map;

/// <summary>One floor of the dungeon, and what the player knows about it.</summary>
public sealed class DungeonMap
{
    private readonly Grid<TileKind> tiles;
    private readonly Grid<bool> visible;
    private readonly Grid<bool> explored;

    public int Width => tiles.Width;

    public int Height => tiles.Height;

    /// <summary>The rooms a generator carved, empty for a generator that carves none.</summary>
    public IReadOnlyList<Rectangle> Rooms { get; internal set; } = [];

    /// <summary>Where the player arrives on this floor.</summary>
    public Position Entrance { get; internal set; }

    /// <summary>Where the stairs to the next floor are.</summary>
    public Position StairsDown { get; internal set; }

    public DungeonMap(int width, int height)
    {
        tiles = new Grid<TileKind>(width, height, TileKind.Wall);
        visible = new Grid<bool>(width, height, false);
        explored = new Grid<bool>(width, height, false);
    }

    public bool Contains(Position p) => tiles.Contains(p.X, p.Y);

    public TileKind this[Position p]
    {
        get => tiles[p.X, p.Y];
        set => tiles[p.X, p.Y] = value;
    }

    public bool IsWalkable(Position p) => tiles.GetOrDefault(p.X, p.Y, TileKind.Wall).IsWalkable();

    /// <summary>
    /// True when something standing on <paramref name="from"/> may move to the
    /// neighbouring cell <paramref name="to"/>.
    ///
    /// A diagonal step between two walls that meet at a corner is refused.
    /// Both answers are defensible and this is the one the game takes: sliding
    /// through the join of two solid walls reads as a bug, and it would let a
    /// player go round the corner of a doorway rather than through it. A
    /// diagonal that brushes one wall is allowed, because forbidding that
    /// makes diagonal movement useless anywhere indoors.
    ///
    /// The rule lives here rather than in the player or in the pathfinder,
    /// because the two have to agree. A monster that could cut a corner the
    /// player could not would be a monster that catches somebody who did
    /// everything right.
    /// </summary>
    public bool CanStep(Position from, Position to)
    {
        if (!IsWalkable(to)) return false;

        Position step = to - from;
        if (step.X == 0 || step.Y == 0) return true;

        return IsWalkable(new Position(to.X, from.Y)) || IsWalkable(new Position(from.X, to.Y));
    }

    public bool IsTransparent(Position p) => tiles.GetOrDefault(p.X, p.Y, TileKind.Wall).IsTransparent();

    public bool IsVisible(Position p) => visible.GetOrDefault(p.X, p.Y, false);

    public bool IsExplored(Position p) => explored.GetOrDefault(p.X, p.Y, false);

    /// <summary>Forgets what is lit, keeping what has been seen before.</summary>
    public void ClearVisible() => visible.Fill(false);

    /// <summary>Marks a cell lit, and remembers it from now on.</summary>
    public void MarkVisible(Position p)
    {
        if (!Contains(p)) return;
        visible[p.X, p.Y] = true;
        explored[p.X, p.Y] = true;
    }

    /// <summary>Every walkable cell on the floor.</summary>
    public IEnumerable<Position> WalkableCells()
    {
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                Position p = new(x, y);
                if (IsWalkable(p)) yield return p;
            }
        }
    }

    public int CountWalkable()
    {
        int total = 0;
        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                if (tiles[x, y].IsWalkable()) total++;
            }
        }

        return total;
    }
}
