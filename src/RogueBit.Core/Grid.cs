namespace RogueBit.Core;

/// <summary>A rectangle of cells addressed by column and row.</summary>
public sealed class Grid<T>
{
    private readonly T[] cells;

    public int Width { get; }

    public int Height { get; }

    public Grid(int width, int height, T fill)
    {
        if (width <= 0) throw new ArgumentOutOfRangeException(nameof(width));
        if (height <= 0) throw new ArgumentOutOfRangeException(nameof(height));

        Width = width;
        Height = height;
        cells = new T[width * height];
        Array.Fill(cells, fill);
    }

    public bool Contains(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Height;

    public T this[int x, int y]
    {
        get => cells[Index(x, y)];
        set => cells[Index(x, y)] = value;
    }

    /// <summary>Reads a cell, or returns <paramref name="fallback"/> when it is off the grid.</summary>
    public T GetOrDefault(int x, int y, T fallback) => Contains(x, y) ? cells[Index(x, y)] : fallback;

    public void Fill(T value) => Array.Fill(cells, value);

    private int Index(int x, int y)
    {
        if (!Contains(x, y)) throw new ArgumentOutOfRangeException($"({x},{y}) is not on a {Width}x{Height} grid.");
        return (y * Width) + x;
    }
}
