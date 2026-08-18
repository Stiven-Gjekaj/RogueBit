namespace RogueBit.Core;

/// <summary>A cell on the map, in columns and rows.</summary>
public readonly record struct Position(int X, int Y)
{
    public static Position operator +(Position a, Position b) => new(a.X + b.X, a.Y + b.Y);

    public static Position operator -(Position a, Position b) => new(a.X - b.X, a.Y - b.Y);

    /// <summary>The number of steps a piece that moves in four directions needs.</summary>
    public int ManhattanDistanceTo(Position other) => Math.Abs(X - other.X) + Math.Abs(Y - other.Y);

    /// <summary>The number of steps a piece that also moves diagonally needs.</summary>
    public int ChebyshevDistanceTo(Position other) => Math.Max(Math.Abs(X - other.X), Math.Abs(Y - other.Y));

    public override string ToString() => $"({X},{Y})";
}

/// <summary>The steps an entity can take.</summary>
public static class Directions
{
    public static readonly Position North = new(0, -1);
    public static readonly Position South = new(0, 1);
    public static readonly Position West = new(-1, 0);
    public static readonly Position East = new(1, 0);

    public static readonly Position NorthWest = new(-1, -1);
    public static readonly Position NorthEast = new(1, -1);
    public static readonly Position SouthWest = new(-1, 1);
    public static readonly Position SouthEast = new(1, 1);

    /// <summary>The four steps that share an edge.</summary>
    public static readonly IReadOnlyList<Position> Cardinal = [North, South, West, East];

    /// <summary>The eight steps that share an edge or a corner.</summary>
    public static readonly IReadOnlyList<Position> All =
    [
        North, South, West, East,
        NorthWest, NorthEast, SouthWest, SouthEast,
    ];
}
