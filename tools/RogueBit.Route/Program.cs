namespace RogueBit.Route;

using RogueBit.Core.Saves;

/// <summary>
/// Writes the run that assets/gameplay.gif is recorded from, and prints the
/// keys to replay into the window.
///
///     dotnet run --project tools/RogueBit.Route -- --search 1 200
///     dotnet run --project tools/RogueBit.Route -- --seed 42 --save /tmp/rec
///
/// The whole recipe is in the docstring of scripts/make_gameplay_gif.py. This
/// tool is the first step of it.
/// </summary>
public static class Program
{
    private const string Usage = """
        Finds the walk that the gameplay recording follows.

        Usage:
          RogueBit.Route [options]

        Options:
          --seed <number>       Look at one seed. The default is 1.
          --search <from> <to>  Look at every seed in the range and keep the first that works.
          --save <path>         Where to write the run. Defaults to where the game keeps its saves.
          --keys <number>       How many keys the walk may be, at most. The default is 26.
          --explored <percent>  How much of the floor to walk first. The default is 30.
          --help                Show this text.

        The run is written as run.json in the save directory, so the window
        picks it up with --continue. On Linux, point both at the same place
        with XDG_DATA_HOME.
        """;

    public static int Main(string[] arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        int seed = 1;
        int? searchFrom = null;
        int searchTo = 0;
        string directory = SaveSystem.DefaultDirectory();
        int keys = 26;
        int explored = 30;

        try
        {
            for (int i = 0; i < arguments.Length; i++)
            {
                switch (arguments[i])
                {
                    case "--seed":
                        seed = int.Parse(Next(arguments, ref i));
                        break;

                    case "--search":
                        searchFrom = int.Parse(Next(arguments, ref i));
                        searchTo = int.Parse(Next(arguments, ref i));
                        break;

                    case "--save":
                        directory = Next(arguments, ref i);
                        break;

                    case "--keys":
                        keys = int.Parse(Next(arguments, ref i));
                        break;

                    case "--explored":
                        explored = int.Parse(Next(arguments, ref i));
                        break;

                    case "--help":
                    case "-h":
                        Console.WriteLine(Usage);
                        return 0;

                    default:
                        Console.Error.WriteLine($"'{arguments[i]}' is not an option this tool knows.");
                        Console.Error.WriteLine();
                        Console.Error.WriteLine(Usage);
                        return 1;
                }
            }
        }
        catch (Exception exception) when (exception is FormatException or ArgumentException)
        {
            Console.Error.WriteLine(exception.Message);
            Console.Error.WriteLine();
            Console.Error.WriteLine(Usage);
            return 1;
        }

        RouteFinder finder = new(keys, explored: explored);
        Route? found = null;

        for (int candidate = searchFrom ?? seed; candidate <= (searchFrom is null ? seed : searchTo); candidate++)
        {
            found = finder.Find(candidate);

            // Most seeds give nothing. A doorway and a trap have to be close
            // enough together to fit inside one short walk, and the player has
            // to live long enough to make it.
            Console.WriteLine(found is null ? $"seed {candidate,-6} no walk" : $"seed {candidate,-6} {found.Keys.Count} keys");

            if (found is not null) break;
        }

        if (found is null)
        {
            Console.Error.WriteLine("No seed offered a walk that met the requirements.");
            return 1;
        }

        new SaveSystem(directory).Write(found.Save);

        Console.WriteLine();
        Console.WriteLine($"seed {found.Seed}, saved on turn {found.LeadTurns}");
        Console.WriteLine($"doorway at {found.Doorway}, trap at {found.Trap}");
        Console.WriteLine($"run written to {Path.Combine(directory, "run.json")}");
        Console.WriteLine();
        Console.WriteLine(string.Join(' ', found.Keys));

        return 0;
    }

    private static string Next(string[] arguments, ref int index)
    {
        if (index + 1 >= arguments.Length)
        {
            throw new ArgumentException($"{arguments[index]} needs a value after it.");
        }

        index++;
        return arguments[index];
    }
}
