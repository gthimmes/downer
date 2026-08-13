using Downer.Services;

namespace Downer.Tests;

public sealed class SettingsServiceTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "downer-tests-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        if (Directory.Exists(_dir))
            Directory.Delete(_dir, recursive: true);
    }

    [Fact]
    public void Missing_file_yields_defaults()
    {
        var service = new SettingsService(_dir);

        service.Load();

        Assert.Equal("System", service.Settings.Theme);
        Assert.True(service.Settings.WordWrap);
        Assert.False(service.Settings.Autosave);
        Assert.True(service.Settings.ReopenLastFile);
        Assert.Null(service.Settings.LastFilePath);
        Assert.Empty(service.Settings.RecentFiles);
    }

    [Fact]
    public void Settings_round_trip()
    {
        var writer = new SettingsService(_dir);
        writer.Load();
        writer.Settings.Theme = "Dark";
        writer.Settings.ViewMode = "PreviewOnly";
        writer.Settings.EditorMode = "Source";
        writer.Settings.WordWrap = false;
        writer.Settings.Autosave = true;
        writer.Settings.ReopenLastFile = false;
        writer.Settings.LastFilePath = "C:\\notes\\last.md";
        writer.Settings.FontSize = 18;
        writer.Settings.RecentFiles.Add("C:\\notes\\a.md");
        writer.Save();

        var reader = new SettingsService(_dir);
        reader.Load();

        Assert.Equal("Dark", reader.Settings.Theme);
        Assert.Equal("PreviewOnly", reader.Settings.ViewMode);
        Assert.Equal("Source", reader.Settings.EditorMode);
        Assert.False(reader.Settings.WordWrap);
        Assert.True(reader.Settings.Autosave);
        Assert.False(reader.Settings.ReopenLastFile);
        Assert.Equal("C:\\notes\\last.md", reader.Settings.LastFilePath);
        Assert.Equal(18, reader.Settings.FontSize);
        Assert.Equal(new[] { "C:\\notes\\a.md" }, reader.Settings.RecentFiles);
    }

    [Fact]
    public void Corrupt_file_falls_back_to_defaults()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(Path.Combine(_dir, "settings.json"), "{not valid json!!");

        var service = new SettingsService(_dir);
        service.Load();

        Assert.Equal("System", service.Settings.Theme);
    }

    [Fact]
    public void Save_creates_missing_directory()
    {
        var service = new SettingsService(Path.Combine(_dir, "nested", "deeper"));
        service.Load();

        service.Save();

        Assert.True(File.Exists(Path.Combine(_dir, "nested", "deeper", "settings.json")));
    }
}
