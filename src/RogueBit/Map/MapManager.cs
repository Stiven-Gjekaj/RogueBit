using System;
using GoRogue.MapViews;
using GoRogue.Pathing;
using SadRogue.Primitives;

namespace RogueBit.Map;

public class MapManager
{
    public int Width { get; }
    public int Height { get; }

    private bool[,] walkable;
    private bool[,] transparent;
    private bool[,] explored;
    private bool[,] visible;

    private AStar? aStar;

    private readonly Random rng;

    public MapManager(int width, int height, Random rng)
    {
        Width = width;
        Height = height;
        this.rng = rng;
        walkable = new bool[width, height];
        transparent = new bool[width, height];
        explored = new bool[width, height];
        visible = new bool[width, height];
    }

    public void GenerateDungeon()
    {
        var gen = new DungeonGenerator(Width, Height, rng);
        gen.Generate();
        walkable = gen.Walkable;
        transparent = gen.Transparent;
        explored = new bool[Width, Height];
        visible = new bool[Width, Height];

        // Build A* cost map
        var cost = new ArrayMap<double>(Width, Height);
        for (int y = 0; y < Height; y++)
        for (int x = 0; x < Width; x++)
            cost[x, y] = walkable[x, y] ? 1.0 : double.MaxValue;
        aStar = new AStar(cost, GoRogue.Distance.MANHATTAN);
    }

    public bool IsInBounds(Point p) => p.X >= 0 && p.X < Width && p.Y >= 0 && p.Y < Height;
    public bool IsWalkable(Point p) => walkable[p.X, p.Y];
    public bool IsTransparent(Point p) => transparent[p.X, p.Y];
    public bool IsExplored(Point p) => explored[p.X, p.Y];
    public bool IsVisible(Point p) => visible[p.X, p.Y];

    public void RecomputeFov(Point origin, int radius)
    {
        for (int y = 0; y < Height; y++)
            for (int x = 0; x < Width; x++)
                visible[x, y] = false;

        int minX = Math.Max(0, origin.X - radius);
        int maxX = Math.Min(Width - 1, origin.X + radius);
        int minY = Math.Max(0, origin.Y - radius);
        int maxY = Math.Min(Height - 1, origin.Y + radius);

        SetVisible(origin.X, origin.Y);

        for (int y = minY; y <= maxY; y++)
        for (int x = minX; x <= maxX; x++)
        {
            var dx = x - origin.X;
            var dy = y - origin.Y;
            if (dx * dx + dy * dy > radius * radius) continue;
            CastRay(origin, new Point(x, y));
        }
    }

    private void CastRay(Point origin, Point target)
    {
        int x0 = origin.X, y0 = origin.Y;
        int x1 = target.X, y1 = target.Y;

        int dx = Math.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
        int dy = -Math.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
        int err = dx + dy;

        int x = x0, y = y0;
        while (true)
        {
            SetVisible(x, y);
            if (!transparent[x, y]) break;
            if (x == x1 && y == y1) break;
            int e2 = 2 * err;
            if (e2 >= dy)
            {
                err += dy;
                x += sx;
            }
            if (e2 <= dx)
            {
                err += dx;
                y += sy;
            }
            if (x < 0 || x >= Width || y < 0 || y >= Height) break;
        }
    }

    private void SetVisible(int x, int y)
    {
        visible[x, y] = true;
        explored[x, y] = true;
    }

    public Point GetNearestWalkable(Point from)
    {
        if (IsInBounds(from) && IsWalkable(from)) return from;
        var queue = new Queue<Point>();
        var visited = new HashSet<Point>();
        queue.Enqueue(from);
        visited.Add(from);
        var dirs = new[] { new Point(0,-1), new Point(0,1), new Point(-1,0), new Point(1,0) };
        while (queue.Count > 0)
        {
            var p = queue.Dequeue();
            foreach (var d in dirs)
            {
                var n = p + d;
                if (!IsInBounds(n) || visited.Contains(n)) continue;
                if (IsWalkable(n)) return n;
                queue.Enqueue(n);
                visited.Add(n);
            }
        }
        return from;
    }

    public Point GetRandomWalkable(Random rng)
    {
        while (true)
        {
            var p = new Point(rng.Next(Width), rng.Next(Height));
            if (walkable[p.X, p.Y]) return p;
        }
    }

    public Point? GetNextStepToward(Point start, Point goal)
    {
        if (aStar == null) return null;
        if (!IsInBounds(goal) || !IsWalkable(goal)) return null;
        var path = aStar.ShortestPath(start, goal);
        if (path == null) return null;
        if (path.Length < 2) return null;
        return path.GetStep(1);
    }

    public char GetGlyph(Point p)
    {
        return walkable[p.X, p.Y] ? '.' : '#';
    }
}
