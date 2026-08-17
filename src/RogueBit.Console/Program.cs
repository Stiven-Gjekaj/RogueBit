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

        run ??= new Run(options.Seed ?? Environment.TickCount);
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
