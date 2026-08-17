namespace RogueBit.Core.Map;

/// <summary>A block of cells, used for rooms and for the halves a split makes.</summary>
public readonly record struct Rectangle(int X, int Y, int Width, int Height)
{
    public int Left => X;

    public int Top => Y;

    /// <summary>The last column inside the rectangle.</summary>
    public int Right => X + Width - 1;

    /// <summary>The last row inside the rectangle.</summary>
    public int Bottom => Y + Height - 1;

    public int Area => Width * Height;

    public Position Centre => new(X + (Width / 2), Y + (Height / 2));

    public bool Contains(Position p) => p.X >= Left && p.X <= Right && p.Y >= Top && p.Y <= Bottom;

    /// <summary>True when the two share at least one cell.</summary>
    public bool Overlaps(Rectangle other)
        => Left <= other.Right && Right >= other.Left && Top <= other.Bottom && Bottom >= other.Top;

    /// <summary>The same rectangle grown by <paramref name="margin"/> on every side.</summary>
    public Rectangle Expand(int margin)
        => new(X - margin, Y - margin, Width + (margin * 2), Height + (margin * 2));

    public IEnumerable<Position> Cells()
    {
        for (int y = Top; y <= Bottom; y++)
        {
            for (int x = Left; x <= Right; x++)
            {
                yield return new Position(x, y);
            }
        }
    }
}
