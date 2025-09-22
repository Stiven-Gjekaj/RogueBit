using System;
using System.Collections.Generic;
using SadConsole;
using SadRogue.Primitives;
using RogueBit.Map;
using RogueBit.Entities;
using RogueBit.Systems;

namespace RogueBit;

public static class GameConstants
{
    public const int MapWidth = 80;
    public const int MapHeight = 25; // Last row (26th) is HUD
    public const int ScreenWidth = 80;
    public const int ScreenHeight = 26;
    public const int FovRadius = 8;
    public const int EnemyAggroRadius = 8;
}

public class Game
{
    public int Seed { get; private set; }
    public bool IsGameOver { get; private set; }
    public int Depth { get; private set; } = 1;

    public Console RootConsole { get; private set; } = null!;
    public MapManager Map { get; private set; } = null!;
    public Player Player { get; private set; } = null!;
    public List<Enemy> Enemies { get; private set; } = new();
    public List<Item> Items { get; private set; } = new();

    public InputSystem Input { get; private set; } = null!;
    public RenderSystem Renderer { get; private set; } = null!;
    public CombatSystem Combat { get; private set; } = null!;
    public SaveSystem Save { get; private set; } = null!;

    private readonly Random rng;

    public Game(int? seed)
    {
        Seed = seed ?? Environment.TickCount;
        rng = new Random(Seed);
    }

    public void Init()
    {
        RootConsole = new GameConsole(GameConstants.ScreenWidth, GameConstants.ScreenHeight, this)
        {
            IsFocused = true
        };
        Game.Instance.Screen = RootConsole;

        Save = new SaveSystem();
        Combat = new CombatSystem(this);
        Input = new InputSystem(this);

        StartNewRun(Seed);

        Renderer = new RenderSystem(this);
        Renderer.DrawAll();
    }

    public void StartNewRun(int seed)
    {
        Seed = seed;
        IsGameOver = false;
        Enemies.Clear();
        Items.Clear();

        Map = new MapManager(GameConstants.MapWidth, GameConstants.MapHeight, rng);
        Map.GenerateDungeon();

        // Place player near center on a floor tile
        var center = new Point(GameConstants.MapWidth / 2, GameConstants.MapHeight / 2);
        var start = Map.GetNearestWalkable(center);
        Player = new Player(start)
        {
            Glyph = '@',
            Color = Color.Cyan,
            MaxHP = 10,
            HP = 10,
            Blocks = true,
        };

        // Place enemies and items
        PlaceEntities();

        // Compute initial FOV
        Map.RecomputeFov(Player.Pos, GameConstants.FovRadius);
    }

    private void PlaceEntities()
    {
        int enemyCount = 10;
        int coinCount = 25;
        int heartCount = 6;

        for (int i = 0; i < enemyCount; i++)
        {
            var pos = Map.GetRandomWalkable(rng);
            if (pos == Player.Pos) { i--; continue; }
            var e = new Enemy(pos)
            {
                Glyph = 'g',
                Color = Color.Green,
                MaxHP = 3,
                HP = 3,
                Blocks = true,
            };
            Enemies.Add(e);
        }

        for (int i = 0; i < coinCount; i++)
        {
            var pos = Map.GetRandomWalkable(rng);
            if (pos == Player.Pos) { i--; continue; }
            Items.Add(Item.Coin(pos));
        }

        for (int i = 0; i < heartCount; i++)
        {
            var pos = Map.GetRandomWalkable(rng);
            if (pos == Player.Pos) { i--; continue; }
            Items.Add(Item.Heart(pos));
        }
    }

    public void HandlePlayerAction(Point delta)
    {
        if (IsGameOver) return;

        var target = Player.Pos + delta;
        if (!Map.IsInBounds(target) || !Map.IsWalkable(target))
        {
            return; // bump into wall does nothing
        }

        // Combat: if enemy at target, attack
        var enemy = GetEnemyAt(target);
        if (enemy != null)
        {
            Combat.ResolveAttack(Player, enemy);
            if (enemy.HP <= 0)
            {
                Enemies.Remove(enemy);
            }
        }
        else
        {
            // Move
            Player.Pos = target;
            // Pickup items on tile
            PickupItemsAt(Player.Pos);
        }

        // After player plays, enemies take turns
        EnemiesAct();

        // Recompute FOV and redraw
        Map.RecomputeFov(Player.Pos, GameConstants.FovRadius);
        Renderer.DrawAll();

        if (Player.HP <= 0)
        {
            OnPlayerDied();
        }
    }

    private void PickupItemsAt(Point pos)
    {
        for (int i = Items.Count - 1; i >= 0; i--)
        {
            var it = Items[i];
            if (it.Pos == pos)
            {
                if (it.Type == ItemType.Heart)
                {
                    Player.HP = Math.Min(Player.MaxHP, Player.HP + 3);
                }
                else if (it.Type == ItemType.Coin)
                {
                    Player.Coins += 1;
                }
                Items.RemoveAt(i);
            }
        }
    }

    private void EnemiesAct()
    {
        foreach (var e in new List<Enemy>(Enemies))
        {
            if (e.HP <= 0) continue;

            var distance = Math.Abs(e.Pos.X - Player.Pos.X) + Math.Abs(e.Pos.Y - Player.Pos.Y);
            Point newPos = e.Pos;

            if (distance <= GameConstants.EnemyAggroRadius)
            {
                var next = Map.GetNextStepToward(e.Pos, Player.Pos);
                if (next != null) newPos = next.Value;
            }
            else
            {
                // Wander randomly
                var dirs = new[] { new Point(0,-1), new Point(0,1), new Point(-1,0), new Point(1,0) };
                var d = dirs[rng.Next(dirs.Length)];
                var p = e.Pos + d;
                if (Map.IsInBounds(p) && Map.IsWalkable(p)) newPos = p;
            }

            if (newPos == Player.Pos)
            {
                Combat.ResolveAttack(e, Player);
            }
            else if (newPos != e.Pos && !IsBlocked(newPos))
            {
                e.Pos = newPos;
            }
        }
    }

    private bool IsBlocked(Point pos)
    {
        if (!Map.IsWalkable(pos)) return true;
        if (Player.Pos == pos && Player.Blocks) return true;
        foreach (var e in Enemies)
            if (e.Pos == pos && e.Blocks) return true;
        return false;
    }

    private Enemy? GetEnemyAt(Point pos)
    {
        foreach (var e in Enemies)
            if (e.Pos == pos) return e;
        return null;
    }

    private void OnPlayerDied()
    {
        IsGameOver = true;
        Save.TryLoadHighScore(out int maxScore);
        if (Player.Coins > maxScore)
        {
            Save.SaveHighScore(Player.Coins);
        }
        Renderer.DrawAll();
    }

    public void Restart()
    {
        StartNewRun(Seed);
        Renderer.DrawAll();
    }

    public void Quit()
    {
        Game.Instance.Dispose();
        Environment.Exit(0);
    }
}

public class GameConsole : Console
{
    private readonly Game game;
    public GameConsole(int width, int height, Game game) : base(width, height)
    {
        this.game = game;
    }

    public override bool ProcessKeyboard(SadConsole.Input.Keyboard keyboard)
    {
        game.Input.HandleKeyboard(keyboard);
        return true;
    }
}
