namespace RogueBit.Console;

/// <summary>What the player asked for on the command line.</summary>
public sealed record Options(int? Seed, bool ColourBlind, bool ShowHelp, bool NoEffects = false, bool Resume = false)
{
    public const string Usage = """
        RogueBit, a turn-based ASCII roguelike.

        Usage:
          RogueBit [options]

        Options:
          --seed <number>   Play a named dungeon. The same seed replays the same run.
          --continue        Pick up the saved run, if there is one.
          --colour-blind    Use a palette that does not rely on red against green.
          --no-effects      Turn off the particles and the screen shake.
          --help            Show this text.

        The run is saved when you press S, and when you leave with Escape.
        A run that ends is removed, so dying cannot be undone by loading.
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

        return new Options(seed, colourBlind, help, noEffects, resume);
    }
}
