namespace RogueBit.Console;

using RogueBit.Core;

/// <summary>What the player asked for on the command line.</summary>
public sealed record Options(
    int? Seed,
    bool ColourBlind,
    bool ShowHelp,
    bool NoEffects = false,
    bool Resume = false,
    bool PrintFloor = false,
    int Depth = 1)
{
    public const string Usage = """
        RogueBit, a turn-based ASCII roguelike.

        Usage:
          RogueBit [options]

        Options:
          --seed <number>   Play a named dungeon. The same seed replays the same run.
          --continue        Pick up the saved run, if there is one.
          --print-floor     Print a floor as text and exit. No window opens.
          --depth <number>  Which floor to print, from 1 to 10. Only with --print-floor.
          --colour-blind    Use a palette that does not rely on red against green.
          --no-effects      Turn off the particles and the screen shake.
          --help            Show this text.

        The run is saved when you press F5, and when you leave with Escape.
        A run that ends is removed, so dying cannot be undone by loading.

        Printing a floor is deterministic: the same seed and the same depth
        print the same floor every time.
        """;

    /// <summary>
    /// Reads the arguments. An unreadable seed is reported rather than being
    /// dropped in silence, because a player who mistypes one would otherwise
    /// get a different dungeon and no explanation.
    /// </summary>
    public static Options Parse(string[] arguments, out string? error)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        error = null;
        int? seed = null;
        bool colourBlind = false;
        bool help = false;
        bool noEffects = false;
        bool resume = false;
        bool printFloor = false;
        int depth = 1;

        for (int i = 0; i < arguments.Length; i++)
        {
            switch (arguments[i])
            {
                case "--seed":
                    if (i + 1 >= arguments.Length)
                    {
                        error = "--seed needs a number after it.";
                        return new Options(null, colourBlind, true);
                    }

                    if (!int.TryParse(arguments[i + 1], out int parsed))
                    {
                        error = $"'{arguments[i + 1]}' is not a whole number.";
                        return new Options(null, colourBlind, true);
                    }

                    seed = parsed;
                    i++;
                    break;

                case "--depth":
                    if (i + 1 >= arguments.Length)
                    {
                        error = "--depth needs a number after it.";
                        return new Options(null, colourBlind, true);
                    }

                    if (!int.TryParse(arguments[i + 1], out int wanted))
                    {
                        error = $"'{arguments[i + 1]}' is not a whole number.";
                        return new Options(null, colourBlind, true);
                    }

                    if (wanted < 1 || wanted > GameRules.FinalDepth)
                    {
                        error = $"The dungeon has floors 1 to {GameRules.FinalDepth}. There is no floor {wanted}.";
                        return new Options(null, colourBlind, true);
                    }

                    depth = wanted;
                    i++;
                    break;

                case "--print-floor":
                    printFloor = true;
                    break;

                case "--colour-blind":
                case "--color-blind":
                    colourBlind = true;
                    break;

                case "--no-effects":
                    noEffects = true;
                    break;

                case "--continue":
                    resume = true;
                    break;

                case "--help":
                case "-h":
                    help = true;
                    break;

                default:
                    error = $"'{arguments[i]}' is not an option this game knows.";
                    return new Options(null, colourBlind, true);
            }
        }

        if (seed is not null && resume)
        {
            error = "--seed and --continue ask for different runs. Choose one.";
            return new Options(null, colourBlind, true);
        }

        if (depth > 1 && !printFloor)
        {
            error = "--depth only means something with --print-floor.";
            return new Options(null, colourBlind, true);
        }

        if (printFloor && resume)
        {
            error = "--print-floor builds a floor from a seed. It cannot read one out of a save.";
            return new Options(null, colourBlind, true);
        }

        return new Options(seed, colourBlind, help, noEffects, resume, printFloor, depth);
    }
}
