using System;
using SadRogue.Primitives;

namespace RogueBit.Map;

public class DungeonGenerator
{
    public int Width { get; }
    public int Height { get; }
    private readonly Random rng;

    public bool[,] Walkable { get; private set; }
    public bool[,] Transparent { get; private set; }

    public DungeonGenerator(int width, int height, Random rng)
    {
        Width = width;
        Height = height;
        this.rng = rng;
        Walkable = new bool[width, height];
        Transparent = new bool[width, height];
    }

    public void Generate()
    {
        // Initialize all walls
        for (int y = 0; y < Height; y++)
        for (int x = 0; x < Width; x++)
        {
            Walkable[x, y] = false;
            Transparent[x, y] = false;
        }

        // Drunkard walk starting near center, carve until desired coverage
        int targetFloors = (int)(Width * Height * 0.50); // aim for ~50%
        int floors = 0;
        var p = new Point(Width / 2, Height / 2);
        Carve(p, ref floors);

        int maxSteps = Width * Height * 20;
        int steps = 0;
        while (floors < targetFloors && steps < maxSteps)
        {
            var dir = rng.Next(4);
            p = dir switch
            {
                0 => new Point(Math.Clamp(p.X, 1, Width - 2), p.Y - 1),
                1 => new Point(Math.Clamp(p.X, 1, Width - 2), p.Y + 1),
                2 => new Point(p.X - 1, Math.Clamp(p.Y, 1, Height - 2)),
                _ => new Point(p.X + 1, Math.Clamp(p.Y, 1, Height - 2)),
            };
            p = new Point(Math.Clamp(p.X, 1, Width - 2), Math.Clamp(p.Y, 1, Height - 2));
            Carve(p, ref floors);
            steps++;
        }

        // Ensure connectivity with a simple pass connecting all floor tiles using BFS from center
        EnsureConnectivity();
    }

    private void Carve(Point p, ref int floors)
    {
        if (!Walkable[p.X, p.Y])
        {
            Walkable[p.X, p.Y] = true;
            Transparent[p.X, p.Y] = true;
            floors++;
        }
    }

    private void EnsureConnectivity()
    {
        var start = new Point(Width / 2, Height / 2);
        if (!Walkable[start.X, start.Y])
        {
            // Find nearest walkable
            for (int radius = 1; radius < Math.Max(Width, Height); radius++)
            {
                for (int y = start.Y - radius; y <= start.Y + radius; y++)
                for (int x = start.X - radius; x <= start.X + radius; x++)
                {
                    if (x < 1 || y < 1 || x >= Width - 1 || y >= Height - 1) continue;
                    if (Walkable[x, y]) { start = new Point(x, y); radius = Math.Max(Width, Height); break; }
                }
            }
        }

        var visited = new bool[Width, Height];
        var q = new System.Collections.Generic.Queue<Point>();
        q.Enqueue(start);
        visited[start.X, start.Y] = true;
        var dirs = new[] { new Point(1,0), new Point(-1,0), new Point(0,1), new Point(0,-1) };
        while (q.Count > 0)
        {
            var p = q.Dequeue();
            foreach (var d in dirs)
            {
                var n = p + d;
                if (n.X < 1 || n.Y < 1 || n.X >= Width - 1 || n.Y >= Height - 1) continue;
                if (visited[n.X, n.Y]) continue;
                if (Walkable[n.X, n.Y])
                {
                    visited[n.X, n.Y] = true;
                    q.Enqueue(n);
                }
            }
        }

        // Any unvisited walkable: dig corridor toward nearest visited
        for (int y = 1; y < Height - 1; y++)
        for (int x = 1; x < Width - 1; x++)
        {
            if (Walkable[x, y] && !visited[x, y])
            {
                // dig a straight line horizontally then vertically toward start
                int cx = x, cy = y;
                while (!visited[cx, cy])
                {
                    if (cx != start.X) cx += Math.Sign(start.X - cx);
                    else if (cy != start.Y) cy += Math.Sign(start.Y - cy);
                    Walkable[cx, cy] = true;
                    Transparent[cx, cy] = true;
                    visited[cx, cy] = true;
                }
            }
        }
    }
}

