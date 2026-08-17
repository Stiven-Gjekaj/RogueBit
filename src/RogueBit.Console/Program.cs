using SadConsole.Configuration;

namespace RogueBit.Console;

/// <summary>Starts the window and hands control to SadConsole.</summary>
public static class Program
{
    public static void Main()
    {
        SadConsole.Settings.WindowTitle = "RogueBit";

        Builder configuration = new Builder()
            .SetWindowSizeInCells(80, 30)
            .OnStart(static (sender, args) => { });

        SadConsole.Game.Create(configuration);
        SadConsole.Game.Instance.Run();
        SadConsole.Game.Instance.Dispose();
    }
}
