using System.IO;
using System.Text.Json;

namespace PokajanRandomizer;

internal sealed class SettingsStore
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string settingsPath;

    public SettingsStore()
    {
        var root = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PokajanRandomizer");
        Directory.CreateDirectory(root);
        settingsPath = Path.Combine(root, "settings.json");
    }

    public AppSettings Load()
    {
        if (!File.Exists(settingsPath))
        {
            return new AppSettings();
        }

        try
        {
            var json = File.ReadAllText(settingsPath);
            return JsonSerializer.Deserialize<AppSettings>(json) ?? new AppSettings();
        }
        catch
        {
            return new AppSettings();
        }
    }

    public void Save(AppSettings settings)
    {
        var json = JsonSerializer.Serialize(settings, JsonOptions);
        File.WriteAllText(settingsPath, json);
    }
}

internal sealed class AppSettings
{
    public bool InfoHintShown { get; set; }
}
