namespace RogueBit.Core.Saves;

using System.Text;
using System.Text.Json;

/// <summary>Reads and writes a run to a file.</summary>
public sealed class SaveSystem
{
    private static readonly JsonSerializerOptions Format = new()
    {
        WriteIndented = true,
    };

    private readonly string directory;

    /// <summary>Uses the place this system belongs on this machine.</summary>
    public SaveSystem()
        : this(DefaultDirectory())
    {
    }

    /// <summary>Uses a directory chosen by the caller, which is what a test does.</summary>
    public SaveSystem(string directory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(directory);
        this.directory = directory;
    }

    public string SavePath => Path.Combine(directory, "run.json");

    /// <summary>Where the record of finished runs lives.</summary>
    public string HistoryPath => Path.Combine(directory, "runs.json");

    /// <summary>The record of every run finished on this machine.</summary>
    public RunHistory History => new(HistoryPath);

    public bool SaveExists => File.Exists(SavePath);

    /// <summary>
    /// Writes a run. The file is written beside the target and then moved over
    /// it, so a crash midway leaves the previous save intact rather than half
    /// of a new one.
    /// </summary>
    public void Write(SaveData data)
    {
        ArgumentNullException.ThrowIfNull(data);

        Directory.CreateDirectory(directory);

        string temporary = SavePath + ".writing";
        File.WriteAllText(temporary, JsonSerializer.Serialize(data, Format), Encoding.UTF8);
        File.Move(temporary, SavePath, overwrite: true);
    }

    /// <summary>
    /// Reads a run, or returns nothing when there is none to read, when the
    /// file is damaged, or when it was written by a version this one does not
    /// understand. A save that cannot be trusted is not loaded.
    /// </summary>
    public SaveData? Read()
    {
        try
        {
            if (!File.Exists(SavePath)) return null;

            SaveData? data = JsonSerializer.Deserialize<SaveData>(File.ReadAllText(SavePath, Encoding.UTF8), Format);

            if (data is null) return null;
            if (data.Version != SaveData.CurrentVersion) return null;
            if (data.Floors.Count == 0) return null;
            if (data.Floors.All(f => f.Depth != data.Depth)) return null;

            foreach (SavedFloor floor in data.Floors)
            {
                int cells = floor.Width * floor.Height;
                if (floor.Tiles.Length != cells) return null;
                if (floor.Explored.Length != cells) return null;
            }

            return data;
        }
        catch (Exception exception) when (exception is JsonException or IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    /// <summary>Removes the save, which is what happens when a run ends.</summary>
    public void Delete()
    {
        try
        {
            if (File.Exists(SavePath)) File.Delete(SavePath);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // A save that will not go away is not worth stopping the game for.
        }
    }

    /// <summary>Where saves belong on this machine.</summary>
    public static string DefaultDirectory()
    {
        if (OperatingSystem.IsWindows())
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "RogueBit");
        }

        string configured = Environment.GetEnvironmentVariable("XDG_DATA_HOME") ?? string.Empty;

        string root = configured.Length > 0
            ? configured
            : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share");

        return Path.Combine(root, "RogueBit");
    }
}
