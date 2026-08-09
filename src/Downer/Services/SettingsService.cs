using System.Text.Json;

namespace Downer.Services;

public sealed class AppSettings
{
    public string Theme { get; set; } = "System";
    public string ViewMode { get; set; } = "Split";
    public bool WordWrap { get; set; } = true;
    public bool ShowLineNumbers { get; set; } = true;
    public double FontSize { get; set; } = 14;
    public List<string> RecentFiles { get; set; } = new();
}

/// <summary>Loads and saves settings as JSON in the per-user application data directory.</summary>
public sealed class SettingsService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly string _filePath;

    public AppSettings Settings { get; private set; } = new();

    public static string DefaultDirectory =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Downer");

    public SettingsService(string? directory = null)
    {
        _filePath = Path.Combine(directory ?? DefaultDirectory, "settings.json");
    }

    public void Load()
    {
        try
        {
            if (File.Exists(_filePath))
                Settings = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(_filePath)) ?? new AppSettings();
        }
        catch
        {
            // Corrupt or unreadable settings fall back to defaults.
            Settings = new AppSettings();
        }
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
            File.WriteAllText(_filePath, JsonSerializer.Serialize(Settings, JsonOptions));
        }
        catch
        {
            // Settings persistence is best-effort; never take the app down for it.
        }
    }
}
