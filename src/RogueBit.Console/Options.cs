namespace RogueBit.Console;

/// <summary>What the player asked for on the command line.</summary>
public sealed record Options(int? Seed, bool ColourBlind, bool ShowHelp)
{
    public const string Usage = """
        RogueBit, a turn-based ASCII roguelike.

        Usage:
          RogueBit [options]

        Options:
          --seed <number>   Play a named dungeon. The same seed replays the same run.
          --colour-blind    Use a palette that does not rely on red against green.
          --help            Show this text.
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

                case "--help":
                case "-h":
                    help = true;
                    break;

                default:
                    error = $"'{arguments[i]}' is not an option this game knows.";
                    return new Options(null, colourBlind, true);
            }
        }

        return new Options(seed, colourBlind, help);
    }
}
