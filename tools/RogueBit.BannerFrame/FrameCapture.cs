namespace RogueBit.BannerFrame;

using System.Text;
using RogueBit.Core;
using RogueBit.Core.Entities;
using RogueBit.Core.Items;
using RogueBit.Core.Map;
using RogueBit.Core.Pathing;

/// <summary>What one cell of a captured frame is.</summary>
/// <remarks>
/// The letters are what the drawing script keys its colours off, so they are a
/// contract between this tool and scripts/make_banner.py.
/// </remarks>
public static class CellKind
{
    public const char Player = 'P';
    public const char Monster = 'M';
    public const char Boss = 'B';
    public const char Coin = 'C';
    public const char Potion = 'O';
    public const char Equipment = 'E';
    public const char Stairs = 'S';
    public const char Door = 'D';
    public const char Trap = 'T';
    public const char FloorLit = 'f';
    public const char FloorRemembered = 'r';
    public const char WallLit = 'w';
    public const char WallRemembered = 'd';
    public const char Unseen = ' ';
}

/// <summary>One viewport of one turn, and how good it looks as a banner.</summary>
public sealed record Frame(int Score, string[] Rows, string[] Kinds, FrameMeta Meta);

/// <summary>The status the banner shows beside the map.</summary>
public sealed record FrameMeta(
    int Hp,
    int MaxHp,
    int Score,
    int Depth,
    int Turns,
    int Seed,
    string? Weapon,
    IReadOnlyList<FrameMessage> Log);

public sealed record FrameMessage(string Text, string Kind);

/// <summary>
/// Plays a run and keeps the best looking viewport of it.
///
/// The banner is not drawn by hand. A bot plays the real game, and every few
/// turns each viewport that still contains the player is scored. That way the
/// dungeon on the front page is one the code actually produced, and it stays
/// honest when the generators change.
/// </summary>
public sealed class FrameCapture
{
    private readonly int width;
    private readonly int height;

    public FrameCapture(int width = 36, int height = 17)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(width, 8);
        ArgumentOutOfRangeException.ThrowIfLessThan(height, 6);

        this.width = width;
        this.height = height;
    }

    /// <summary>Plays one seed through and returns its best frame, if it has one.</summary>
    public Frame? Capture(int seed, int maxTurns = 4000)
    {
        Run run = new(seed);
        Frame? best = null;

        for (int turn = 0; turn < maxTurns && !run.IsOver; turn++)
        {
            PlayOneTurn(run);

            // Sampling every turn is needlessly slow, and neighbouring turns
            // look almost identical anyway.
            if (turn % 2 != 0) continue;

            // A half dead player reads as a bug rather than as tension.
            if (run.Player.Health * 2 < run.Player.MaxHealth) continue;

            // A floor barely walked into is mostly unexplored rock, which reads
            // as empty space rather than as a dungeon.
            if (!IsMostlyRevealed(run, 30)) continue;

            best = BestViewport(run, best);
        }

        return best;
    }

    /// <summary>One turn of a bot that heals, takes what it finds, and explores.</summary>
    private static void PlayOneTurn(Run run)
    {
        if (run.Player.Health * 2 < run.Player.MaxHealth
            && run.Inventory.Items.FirstOrDefault(i => i.Kind == ItemKind.Potion) is { } potion)
        {
            run.Use(potion);
            return;
        }

        if (!run.Inventory.IsFull
            && run.ItemsAt(run.Player.Position).FirstOrDefault(i => !i.IsPickedUpByWalkingOver) is { } underfoot)
        {
            string name = underfoot.Name;
            bool wearable = underfoot.IsEquipment;
            run.PickUp();

            if (wearable && run.Inventory.Items.FirstOrDefault(i => i.Name == name) is { } gear) run.Use(gear);
            return;
        }

        Position? frontier = NearestUnexplored(run);

        if (frontier is null && run.Map[run.Player.Position] == TileKind.StairsDown)
        {
            run.Descend();
            return;
        }

        IReadOnlyList<Position> path = PathFinder.Find(run.Map, run.Player.Position, frontier ?? run.Map.StairsDown);

        if (path.Count == 0) run.Wait();
        else if (run.Move(path[0] - run.Player.Position) == ActionResult.Refused) run.Wait();
    }

    private static Position? NearestUnexplored(Run run)
    {
        Position? best = null;
        int shortest = int.MaxValue;

        foreach (Position cell in run.Map.WalkableCells())
        {
            if (run.Map.IsExplored(cell)) continue;

            int distance = run.Player.Position.ManhattanDistanceTo(cell);
            if (distance >= shortest) continue;

            shortest = distance;
            best = cell;
        }

        return best;
    }

    private static bool IsMostlyRevealed(Run run, int percent)
    {
        int walkable = 0;
        int seen = 0;

        foreach (Position cell in run.Map.WalkableCells())
        {
            walkable++;
            if (run.Map.IsExplored(cell)) seen++;
        }

        return walkable > 0 && seen * 100 >= walkable * percent;
    }

    /// <summary>Scores every viewport that still holds the player, and keeps the best.</summary>
    private Frame? BestViewport(Run run, Frame? best)
    {
        int minLeft = Math.Max(0, run.Player.Position.X - width + 4);
        int maxLeft = Math.Min(run.Map.Width - width, run.Player.Position.X - 4);
        int minTop = Math.Max(0, run.Player.Position.Y - height + 3);
        int maxTop = Math.Min(run.Map.Height - height, run.Player.Position.Y - 3);

        for (int left = minLeft; left <= maxLeft; left += 2)
        {
            for (int top = minTop; top <= maxTop; top++)
            {
                Frame? candidate = Viewport(run, left, top);
                if (candidate is null) continue;
                if (best is not null && candidate.Score <= best.Score) continue;

                best = candidate;
            }
        }

        return best;
    }

    /// <summary>
    /// Reads one viewport and scores it, or returns nothing when it fails the
    /// requirements a banner has.
    /// </summary>
    private Frame? Viewport(Run run, int left, int top)
    {
        string[] rows = new string[height];
        string[] kinds = new string[height];
        int lit = 0, remembered = 0, wall = 0, monsters = 0, items = 0, stairs = 0, unseen = 0;
        int doors = 0, traps = 0;

        for (int y = 0; y < height; y++)
        {
            StringBuilder glyphs = new(width);
            StringBuilder kind = new(width);

            for (int x = 0; x < width; x++)
            {
                Position p = new(left + x, top + y);
                char glyph;
                char cell;

                if (!run.Map.Contains(p))
                {
                    glyph = ' ';
                    cell = CellKind.Unseen;
                    unseen++;
                }
                else if (p == run.Player.Position)
                {
                    glyph = run.Player.Glyph;
                    cell = CellKind.Player;
                }
                else if (run.MonsterAt(p) is { } monster && run.Map.IsVisible(p))
                {
                    glyph = monster.Glyph;
                    cell = monster.Behaviour == MonsterBehaviour.Boss ? CellKind.Boss : CellKind.Monster;
                    monsters++;
                }
                else if (run.ItemsAt(p).FirstOrDefault() is { } item && run.Map.IsVisible(p))
                {
                    glyph = item.Glyph;
                    cell = item.Kind switch
                    {
                        ItemKind.Coin => CellKind.Coin,
                        ItemKind.Potion => CellKind.Potion,
                        _ => CellKind.Equipment,
                    };
                    items++;
                }
                else if (!run.Map.IsExplored(p))
                {
                    glyph = ' ';
                    cell = CellKind.Unseen;
                    unseen++;
                }
                else
                {
                    bool visible = run.Map.IsVisible(p);

                    TileKind tile = run.Map[p];

                    // One table of glyphs for the whole game, so a new tile
                    // cannot be drawn here as a wall while the window draws it
                    // properly. Only the colour letters are this tool's own.
                    glyph = MapText.Glyph(tile);

                    switch (tile)
                    {
                        case TileKind.StairsDown:
                        case TileKind.StairsUp:
                            cell = CellKind.Stairs;
                            stairs++;
                            break;

                        case TileKind.Door:
                            cell = CellKind.Door;
                            doors++;
                            break;

                        case TileKind.TrapSprung:
                            cell = CellKind.Trap;
                            traps++;
                            break;

                        case TileKind.Floor:
                        case TileKind.TrapArmed:
                            cell = visible ? CellKind.FloorLit : CellKind.FloorRemembered;
                            if (visible) lit++; else remembered++;
                            break;

                        default:
                            cell = visible ? CellKind.WallLit : CellKind.WallRemembered;
                            wall++;
                            break;
                    }
                }

                glyphs.Append(glyph);
                kind.Append(cell);
            }

            rows[y] = glyphs.ToString();
            kinds[y] = kind.ToString();
        }

        // A banner frame needs all four at once: walls, so it reads as a
        // dungeon rather than a scatter of dots; a lit room, so it looks alive;
        // remembered ground, for depth; and something in it worth seeing.
        if (monsters < 1 || lit < 80 || wall < 45 || remembered < 55) return null;

        // Darkness is atmosphere rather than dead space, so it costs little.
        //
        // A doorway is worth as much as an item. It is the thing that makes a
        // corridor read as a way into somewhere rather than a line of dots, and
        // a banner that did not show one would be a banner of an older game.
        int score = (lit * 10) + (remembered * 3) + (wall * 3)
                  + (monsters * 200) + (items * 100) + (stairs * 90)
                  + (doors * 120) + (traps * 60) - (unseen * 2);

        return new Frame(score, rows, kinds, Describe(run));
    }

    private static FrameMeta Describe(Run run) => new(
        run.Player.Health,
        run.Player.MaxHealth,
        run.Score,
        run.Depth,
        run.Turns,
        run.Seed,
        run.Inventory.Weapon?.Name,
        [.. run.Log.Latest(6).Select(m => new FrameMessage(m.Display, m.Kind.ToString()))]);
}
