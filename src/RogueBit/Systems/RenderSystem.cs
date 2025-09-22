using System;
using RogueBit.Entities;
using SadRogue.Primitives;

namespace RogueBit.Systems;

public class RenderSystem
{
    private readonly Game game;

    public RenderSystem(Game game)
    {
        this.game = game;
    }

    public void DrawAll()
    {
        var con = game.RootConsole;
        con.Clear();

        // Draw map
        for (int y = 0; y < GameConstants.MapHeight; y++)
        for (int x = 0; x < GameConstants.MapWidth; x++)
        {
            var p = new Point(x, y);
            if (!game.Map.IsExplored(p)) continue;
            bool vis = game.Map.IsVisible(p);

            char glyph = game.Map.GetGlyph(p);
            var fg = vis ? (glyph == '#' ? new Color(130,130,130) : new Color(180,180,180))
                         : (glyph == '#' ? new Color(50,50,50) : new Color(70,70,70));

            con.Print(x, y, glyph.ToString(), fg, Color.Black);
        }

        // Draw items and enemies (only if visible)
        foreach (var it in game.Items)
        {
            if (game.Map.IsVisible(it.Pos))
                con.Print(it.Pos.X, it.Pos.Y, it.Glyph.ToString(), it.Color, Color.Black);
        }

        foreach (var e in game.Enemies)
        {
            if (game.Map.IsVisible(e.Pos))
                con.Print(e.Pos.X, e.Pos.Y, e.Glyph.ToString(), e.Color, Color.Black);
        }

        // Draw player (always)
        con.Print(game.Player.Pos.X, game.Player.Pos.Y, game.Player.Glyph.ToString(), game.Player.Color, Color.Black);

        DrawHud();

        if (game.IsGameOver)
        {
            DrawGameOver();
        }
    }

    private void DrawHud()
    {
        var con = game.RootConsole;
        int y = GameConstants.ScreenHeight - 1; // bottom row
        var bar = new string(' ', GameConstants.ScreenWidth);
        con.Print(0, y, bar, Color.White, Color.Black);

        game.Save.TryLoadHighScore(out int high);
        string hud = $"HP: {game.Player.HP}/{game.Player.MaxHP}   Coins: {game.Player.Coins}   Depth: {game.Depth}   Seed: {game.Seed}";
        con.Print(0, y, hud, Color.White, Color.Black);
    }

    private void DrawGameOver()
    {
        game.Save.TryLoadHighScore(out int high);
        string msg = $" You died!  Score: {game.Player.Coins}  High Score: {Math.Max(high, game.Player.Coins)}  (R)estart  (Q)uit ";
        int x = Math.Max(0, (GameConstants.ScreenWidth - msg.Length) / 2);
        int y = GameConstants.ScreenHeight / 2;
        game.RootConsole.Print(x, y, msg, Color.White, new Color(60, 0, 0));
    }
}
