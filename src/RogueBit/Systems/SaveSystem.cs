using System;
using System.IO;
using Newtonsoft.Json;

namespace RogueBit.Systems;

public class SaveSystem
{
    private record HighScoreData(int MaxScore);

    public string GetHighScorePath()
    {
        if (OperatingSystem.IsWindows())
        {
            var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            return Path.Combine(appData, "RogueBit", "highscore.json");
        }
        else
        {
            var xdg = Environment.GetEnvironmentVariable("XDG_DATA_HOME");
            string baseDir = !string.IsNullOrWhiteSpace(xdg) ? xdg : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".local", "share");
            return Path.Combine(baseDir, "RogueBit", "highscore.json");
        }
    }

    public bool TryLoadHighScore(out int maxScore)
    {
        try
        {
            var path = GetHighScorePath();
            if (!File.Exists(path)) { maxScore = 0; return true; }
            var json = File.ReadAllText(path);
            var data = JsonConvert.DeserializeObject<HighScoreData>(json);
            maxScore = data?.MaxScore ?? 0;
            return true;
        }
        catch
        {
            maxScore = 0;
            return false;
        }
    }

    public bool SaveHighScore(int score)
    {
        try
        {
            var path = GetHighScorePath();
            var dir = Path.GetDirectoryName(path)!;
            Directory.CreateDirectory(dir);
            var data = new HighScoreData(score);
            var json = JsonConvert.SerializeObject(data, Formatting.Indented);
            File.WriteAllText(path, json);
            return true;
        }
        catch
        {
            return false;
        }
    }
}

