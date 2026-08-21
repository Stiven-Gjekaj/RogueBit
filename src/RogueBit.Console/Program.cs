namespace RogueBit.Console;

using SadConsole.Configuration;
using RogueBit.Core;
using RogueBit.Core.Saves;

/// <summary>Starts the window and hands control to SadConsole.</summary>
public static class Program
{
    public static int Main(string[] arguments)
    {
        Options options = Options.Parse(arguments, out string? error);

        if (error is not null)
        {
            System.Console.Error.WriteLine(error);
            System.Console.Error.WriteLine();
            System.Console.Error.WriteLine(Options.Usage);
            return 1;
        }

        if (options.ShowHelp)
        {
            System.Console.WriteLine(Options.Usage);
            return 0;
        }

        if (options.PrintFloor)
        {
            foreach (string row in FloorPrinter.Render(options.Seed ?? 0, options.Depth))
            {
                System.Console.WriteLine(row);
            }

            return 0;
        }

        SaveSystem saves = new();
        Run? run = null;

        if (options.Resume)
        {
            if (saves.Read() is { } saved)
            {
                run = RunSerialiser.Restore(saved);
                run.Log.Add("You take up where you left off.", MessageKind.Good);
            }
            else
            {
                System.Console.Error.WriteLine("There is no saved run to continue.");
                return 1;
            }
        }

        if (run is null)
        {
            run = new Run(options.Seed ?? Environment.TickCount);

            // Only for a run that is starting. A resumed run is the same run
            // carrying on, and what happened on some earlier one is not what
            // the player opened the game to read.
            StartOfRun.Announce(run, saves.History);
        }

        Theme theme = Theme.For(options.ColourBlind);

        SadConsole.Settings.WindowTitle = $"RogueBit, seed {run.Seed}";

        Builder configuration = new Builder()
            .SetWindowSizeInCells(GameScreen.ScreenWidth, GameScreen.ScreenHeight)
            .OnStart((sender, args) =>
                SadConsole.Game.Instance.Screen = new GameScreen(run, theme, saves, !options.NoEffects));

        SadConsole.Game.Create(configuration);
        SadConsole.Game.Instance.Run();
        SadConsole.Game.Instance.Dispose();

        return 0;
    }
}
