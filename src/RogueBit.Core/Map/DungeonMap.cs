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
