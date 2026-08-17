namespace RogueBit.BannerFrame;

using System.Text.Json;

/// <summary>
/// Writes assets/banner-frame.json, which scripts/make_banner.py turns into
/// the banner.
///
///     dotnet run --project tools/RogueBit.BannerFrame -- --seed 7
///     dotnet run --project tools/RogueBit.BannerFrame -- --search 0 40
///
/// With --search it plays a range of seeds and keeps the best frame any of them
/// produced. Most seeds give nothing, because the bot dies before the floor is
/// worth looking at.
/// </summary>
public static class Program
{
    private const string Usage = """
        Captures the dungeon that appears in the banner.

        Usage:
          RogueBit.BannerFrame [options]

        Options:
          --seed <number>          Capture one seed. The default is 7.
          --search <from> <to>     Try every seed in the range and keep the best.
          --out <path>             Where to write. Defaults to assets/banner-frame.json.
          --size <width> <height>  Viewport in cells. Defaults to 36 by 17.
          --help                   Show this text.
        """;

    public static int Main(string[] arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        int seed = 7;
        int? searchFrom = null;
        int searchTo = 0;
        string output = Path.Combine("assets", "banner-frame.json");
        int width = 36;
        int height = 17;

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

                    case "--out":
                        output = Next(arguments, ref i);
                        break;

                    case "--size":
                        width = int.Parse(Next(arguments, ref i));
                        height = int.Parse(Next(arguments, ref i));
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

        FrameCapture capture = new(width, height);
        Frame? best = null;
        int bestSeed = seed;

        if (searchFrom is { } from)
        {
            for (int candidateSeed = from; candidateSeed <= searchTo; candidateSeed++)
            {
                Frame? found = capture.Capture(candidateSeed);
                Console.WriteLine(
                    found is null
                        ? $"seed {candidateSeed,-6} no frame"
                        : $"seed {candidateSeed,-6} score {found.Score}");

                if (found is null || (best is not null && found.Score <= best.Score)) continue;

                best = found;
                bestSeed = candidateSeed;
            }
        }
        else
        {
            best = capture.Capture(seed);
        }

        if (best is null)
        {
            Console.Error.WriteLine("No seed produced a frame that met the requirements.");
            return 1;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);
        File.WriteAllText(
            output,
            JsonSerializer.Serialize(
                new { rows = best.Rows, kinds = best.Kinds, meta = best.Meta },
                new JsonSerializerOptions
                {
                    WriteIndented = true,

                    // The drawing script reads these names, so the casing here
                    // is a contract rather than a preference.
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                }) + Environment.NewLine);

        Console.WriteLine();
        foreach (string row in best.Rows) Console.WriteLine(row);
        Console.WriteLine();
        Console.WriteLine($"seed {bestSeed}, turn {best.Meta.Turns}, floor {best.Meta.Depth}, score {best.Score}");
        Console.WriteLine($"written to {output}");

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
