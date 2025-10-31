using System;
using System.IO;
using System.Text.Json;

namespace GuacClient;
public sealed class JsonFileStartUrlStore : IStartUrlStore
{
    private readonly string _file;

    public JsonFileStartUrlStore()
    {
        var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var appDir = Path.Combine(baseDir, "CompuMaster", "GuacamoleLauncher");
        Directory.CreateDirectory(appDir);
        _file = Path.Combine(appDir, "config.json");
    }

    public string? Load()
    {
        if (!File.Exists(_file)) return null;
        try
        {
            var doc = JsonSerializer.Deserialize<Config>(File.ReadAllText(_file));
            return string.IsNullOrWhiteSpace(doc?.StartUrl) ? null : doc!.StartUrl;
        }
        catch { return null; }
    }

    public void Save(string url)
    {
        var json = JsonSerializer.Serialize(new Config { StartUrl = url },
            new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(_file, json);
    }

    public void Delete()
    {
        try
        {
            if (File.Exists(_file))
            {
                var cfg = new Config(); // leeren
                File.WriteAllText(_file, JsonSerializer.Serialize(cfg));
            }
        }
        catch { /* ignore */ }
    }

    private sealed class Config { public string? StartUrl { get; set; } }
}
