namespace RogueBit.Console;

using SadConsole;
using SadConsole.Input;
using SadRogue.Primitives;
using RogueBit.Core;
using RogueBit.Core.Entities;
using RogueBit.Core.Items;
using RogueBit.Core.Map;
using RogueBit.Core.Saves;

// SadConsole ships its own Rectangle. Aliasing it keeps the two apart, which is
// the collision that stopped the first version of this game compiling at all.
using Area = SadRogue.Primitives.Rectangle;

/// <summary>
/// Draws the run and turns keys into actions.
///
/// This screen holds no rules. It asks the run what is true and draws that, so
/// the game cannot behave one way here and another way under test.
/// </summary>
public sealed class GameScreen : SadConsole.Console
{
    public const int ScreenWidth = 80;
    public const int MapTop = 1;
    public const int StatusRow = MapTop + Run.MapHeight + 1;
    public const int LogTop = StatusRow + 1;
    public const int LogLines = 6;
    public const int ScreenHeight = LogTop + LogLines;

    private readonly Theme theme;
    private readonly Effects effects;
    private readonly SaveSystem saves;
    private readonly bool effectsEnabled;

    private Run run;
    private bool showingInventory;
    private bool scoreRecorded;

    public GameScreen(Run run, Theme theme, SaveSystem saves, bool effectsEnabled = true)
        : base(ScreenWidth, ScreenHeight)
    {
        ArgumentNullException.ThrowIfNull(run);
        ArgumentNullException.ThrowIfNull(saves);

        this.run = run;
        this.theme = theme;
        this.saves = saves;
        this.effectsEnabled = effectsEnabled;

        effects = new Effects(theme, run.Seed);

        IsFocused = true;
        Draw();
    }

    /// <summary>
    /// Keeps the particles moving between key presses. The game itself is turn
    /// based and only changes when a key is pressed, so this redraws only while
    /// something is actually in flight.
    /// </summary>
    public override void Update(TimeSpan delta)
    {
        base.Update(delta);

        if (!effectsEnabled || !effects.IsBusy) return;

        effects.Update(delta.TotalSeconds);
        Draw();
    }

    public override bool ProcessKeyboard(Keyboard keyboard)
    {
        ArgumentNullException.ThrowIfNull(keyboard);

        if (keyboard.IsKeyPressed(Keys.Escape))
        {
            // Leaving keeps the run, so the game can be picked up again.
            if (!run.IsOver) saves.Write(RunSerialiser.Capture(run));
            Environment.Exit(0);
            return true;
        }

        if (run.IsOver)
        {
            RecordTheEndOfTheRun();

            if (keyboard.IsKeyPressed(Keys.R))
            {
                run = run.Restart();
                showingInventory = false;
                scoreRecorded = false;
                effects.Clear();
                Draw();
            }

            return true;
        }

        if (showingInventory)
        {
            HandleInventoryKeys(keyboard);
            Draw();
            return true;
        }

        HandlePlayKeys(keyboard);

        if (effectsEnabled) effects.Play(run.LastTurnEvents);
        if (run.IsOver) RecordTheEndOfTheRun();

        Draw();
        return true;
    }

    private void SaveNow()
    {
        saves.Write(RunSerialiser.Capture(run));
        run.Log.Add("The run is saved.", MessageKind.Good);
    }

    /// <summary>
    /// Records the score once and removes the save. A dead run must not be
    /// resumable, or the save becomes a way to undo dying.
    /// </summary>
    private void RecordTheEndOfTheRun()
    {
        if (scoreRecorded) return;

        scoreRecorded = true;
        saves.RecordScore(run.Score);
        saves.Delete();
    }

    private void HandlePlayKeys(Keyboard keyboard)
    {
        foreach ((Keys key, Position step) in Keybindings.Movement)
        {
            if (keyboard.IsKeyPressed(key))
            {
                run.Move(step);
                return;
            }
        }

        if (keyboard.IsKeyPressed(Keys.OemPeriod) || keyboard.IsKeyPressed(Keys.NumPad5)) run.Wait();
        else if (keyboard.IsKeyPressed(Keys.G)) run.PickUp();
        else if (keyboard.IsKeyPressed(Keys.OemComma)) run.Descend();
        else if (keyboard.IsKeyPressed(Keys.I)) showingInventory = true;
        else if (keyboard.IsKeyPressed(Keys.S)) SaveNow();
        else if (keyboard.IsKeyPressed(Keys.R))
        {
            run = run.Restart();
            effects.Clear();
        }
    }

    private void HandleInventoryKeys(Keyboard keyboard)
    {
        if (keyboard.IsKeyPressed(Keys.I) || keyboard.IsKeyPressed(Keys.Escape))
        {
            showingInventory = false;
            return;
        }

        // The pack is listed a to p, so the key is the index.
        for (int i = 0; i < run.Inventory.Items.Count && i < 16; i++)
        {
            if (!keyboard.IsKeyPressed(Keys.A + i)) continue;

            run.Use(run.Inventory.Items[i]);
            showingInventory = false;
            return;
        }
    }

    private void Draw()
    {
        this.Clear();
        this.Fill(new Area(0, 0, Width, Height), theme.Text, Theme.Background, 0);

        (int shakeX, int shakeY) = effectsEnabled ? effects.ShakeOffset : (0, 0);

        DrawMap(shakeX, shakeY);
        DrawEntities(shakeX, shakeY);
        if (effectsEnabled) DrawParticles(shakeX, shakeY);
        DrawStatus();
        DrawLog();

        if (showingInventory) DrawInventory();
        if (run.IsOver) DrawGameOver();
    }

    private void DrawMap(int shakeX, int shakeY)
    {
        for (int y = 0; y < run.Map.Height; y++)
        {
            for (int x = 0; x < run.Map.Width; x++)
            {
                Position cell = new(x, y);
                if (!run.Map.IsExplored(cell)) continue;

                bool lit = run.Map.IsVisible(cell);
                TileKind kind = run.Map[cell];

                (char glyph, Color colour) = kind switch
                {
                    TileKind.StairsDown => ('>', lit ? theme.Stairs : theme.FloorRemembered),
                    TileKind.Floor => ('.', lit ? theme.FloorLit : theme.FloorRemembered),
                    _ => ('#', lit ? theme.WallLit : theme.WallRemembered),
                };

                PrintOnMap(x + shakeX, y + shakeY, glyph, colour);
            }
        }
    }

    private void DrawEntities(int shakeX, int shakeY)
    {
        // Items first, so a monster standing on one is still the thing you see.
        foreach (Item item in run.Items)
        {
            if (!run.Map.IsVisible(item.Position)) continue;

            Color colour = item.Kind switch
            {
                ItemKind.Coin => theme.Coin,
                ItemKind.Potion => theme.Potion,
                _ => theme.Equipment,
            };

            PrintOnMap(item.Position.X + shakeX, item.Position.Y + shakeY, item.Glyph, colour);
        }

        foreach (Monster monster in run.Monsters)
        {
            if (!run.Map.IsVisible(monster.Position)) continue;

            Color colour = monster.Behaviour == MonsterBehaviour.Boss ? theme.Boss : theme.Monster;
            PrintOnMap(monster.Position.X + shakeX, monster.Position.Y + shakeY, monster.Glyph, colour);
        }

        PrintOnMap(
            run.Player.Position.X + shakeX,
            run.Player.Position.Y + shakeY,
            run.Player.Glyph,
            theme.Player);
    }

    private void DrawParticles(int shakeX, int shakeY)
    {
        foreach (Particle particle in effects.Particles)
        {
            // A particle fades by moving toward the background rather than by
            // changing its alpha, because a console cell has no transparency.
            Color colour = Color.Lerp(Theme.Background, particle.Colour, (float)particle.Remaining);

            PrintOnMap(
                (int)Math.Round(particle.X) + shakeX,
                (int)Math.Round(particle.Y) + shakeY,
                particle.Glyph,
                colour);
        }
    }

    /// <summary>
    /// Draws one cell of the map, dropping anything the shake has pushed off
    /// the edge rather than letting it spill into the status bar.
    /// </summary>
    private void PrintOnMap(int x, int y, char glyph, Color colour)
    {
        if (x < 0 || x >= Run.MapWidth || y < 0 || y >= Run.MapHeight) return;

        this.Print(x + 1, y + MapTop, glyph.ToString(), colour, Theme.Background);
    }

    private void DrawStatus()
    {
        Player player = run.Player;

        string health = $"HP {player.Health}/{player.MaxHealth}";
        string floor = GameRules.IsFinalDepth(run.Depth) ? $"{run.Depth} (the bottom)" : run.Depth.ToString();
        string rest = $"  Score {run.Score}   Floor {floor}   Turn {run.Turns}   Seed {run.Seed}";
        string gear = run.Inventory.Weapon is { } weapon ? $"   {weapon.Name}" : string.Empty;

        Color healthColour = player.Health * 3 <= player.MaxHealth ? theme.Bad
            : player.Health * 2 <= player.MaxHealth ? theme.Warning
            : theme.Good;

        this.Print(1, StatusRow, health, healthColour, Theme.Background);
        this.Print(1 + health.Length, StatusRow, rest + gear, theme.TextDim, Theme.Background);
    }

    private void DrawLog()
    {
        IReadOnlyList<LogMessage> lines = run.Log.Latest(LogLines);

        for (int i = 0; i < lines.Count; i++)
        {
            LogMessage message = lines[i];
            string text = message.Display;
            if (text.Length > ScreenWidth - 2) text = text[..(ScreenWidth - 2)];

            // The oldest line on the panel is dimmed, so the eye finds the newest.
            Color colour = i == lines.Count - 1 ? theme.ForMessage(message.Kind) : theme.TextDim;
            this.Print(1, LogTop + i, text, colour, Theme.Background);
        }
    }

    private void DrawInventory()
    {
        const int left = 20;
        const int top = 6;
        const int width = 40;
        int height = Math.Max(6, run.Inventory.Items.Count + 5);

        this.Fill(new Area(left, top, width, height), theme.Text, new Color(22, 26, 30), 0);
        this.Print(left + 2, top + 1, "Pack", theme.Stairs, new Color(22, 26, 30));

        if (run.Inventory.Items.Count == 0)
        {
            this.Print(left + 2, top + 3, "You are carrying nothing.", theme.TextDim, new Color(22, 26, 30));
        }

        for (int i = 0; i < run.Inventory.Items.Count; i++)
        {
            Item item = run.Inventory.Items[i];
            this.Print(left + 2, top + 3 + i, $"{(char)('a' + i)}) {item.Name}", theme.Text, new Color(22, 26, 30));
        }

        this.Print(left + 2, top + height - 1, "letter to use, i to close", theme.TextDim, new Color(22, 26, 30));
    }

    private void DrawGameOver()
    {
        string headline = run.HasWon ? "You reached the bottom and lived." : "You died.";

        string[] lines =
        [
            headline,
            $"Floor {run.Depth}, {run.Turns} turns, {run.Score} points.",
            $"Best so far {saves.ReadBestScore()}.",
            "R to play the same seed again, Escape to leave.",
        ];

        // Green for a win, red for a death, so the ending reads before the words do.
        Color panel = run.HasWon ? new Color(14, 44, 26) : new Color(48, 16, 16);
        Color accent = run.HasWon ? theme.Good : theme.Bad;

        int width = lines.Max(l => l.Length) + 6;
        int left = (ScreenWidth - width) / 2;
        int top = (Run.MapHeight / 2) - 2;

        this.Fill(new Area(left, top, width, lines.Length + 4), theme.Text, panel, 0);

        for (int i = 0; i < lines.Length; i++)
        {
            this.Print(left + 3, top + 2 + i, lines[i], i == 0 ? accent : theme.Text, panel);
        }
    }
}
