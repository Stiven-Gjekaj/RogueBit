using System;
using SadConsole;
using SadConsole.Configuration;

namespace RogueBit;

public static class Program
{
    public static void Main(string[] args)
    {
        int? seed = null;
        for (int i = 0; i < args.Length; i++)
        {
            if (args[i] == "--seed" && i + 1 < args.Length && int.TryParse(args[i + 1], out var s))
            {
                seed = s;
                break;
            }
        }

        var config = new Configuration
        {
            WindowTitle = "RogueBit",
            ScreenWidth = GameConstants.ScreenWidth,
            ScreenHeight = GameConstants.ScreenHeight
        };

        Game.Create(config);
        Game.Instance.OnStart = () =>
        {
            var game = new Game(seed);
            game.Init();
        };
        Game.Instance.Run();
        Game.Instance.Dispose();
    }
}
