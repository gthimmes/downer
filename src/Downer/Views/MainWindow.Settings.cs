using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Styling;
using Downer.Core;
using Downer.Services;
using TextMateSharp.Grammars;

namespace Downer.Views;

public enum ThemePreference
{
    System,
    Light,
    Dark,
}

public partial class MainWindow
{
    private SettingsService _settingsService = null!;
    private ThemePreference _themePreference = ThemePreference.System;
    private bool _reopenLastFile = true;

    private void SetUpSettings()
    {
        _settingsService = new SettingsService();
        _settingsService.Load();
        var s = _settingsService.Settings;

        SetFontSize(s.FontSize);
        if (Editor.WordWrap != s.WordWrap)
            OnToggleWordWrap(null, null!);
        SetAutosaveEnabled(s.Autosave);
        _reopenLastFile = s.ReopenLastFile;
        MenuReopenLast.IsChecked = _reopenLastFile;
        _lineNumbersPreference = s.ShowLineNumbers;
        MenuLineNumbers.IsChecked = _lineNumbersPreference;

        if (Enum.TryParse<ViewMode>(s.ViewMode, out var mode))
            SetViewMode(mode);

        SetEditorMode(Enum.TryParse<EditorSurfaceMode>(s.EditorMode, out var editorMode)
            ? editorMode
            : EditorSurfaceMode.Rich);

        if (Enum.TryParse<ThemePreference>(s.Theme, out var theme))
            ApplyThemePreference(theme);

        ActualThemeVariantChanged += (_, _) => UpdateEditorTheme();

        RebuildRecentMenu();
    }

    protected override void OnClosed(EventArgs e)
    {
        var s = _settingsService.Settings;
        s.Theme = _themePreference.ToString();
        s.ViewMode = _viewMode.ToString();
        s.EditorMode = _editorMode.ToString();
        s.WordWrap = Editor.WordWrap;
        s.Autosave = _autosaveEnabled;
        s.ReopenLastFile = _reopenLastFile;
        s.LastFilePath = _currentFilePath;
        s.ShowLineNumbers = _lineNumbersPreference;
        s.FontSize = Editor.FontSize;
        _settingsService.Save();

        base.OnClosed(e);
    }

    private void OnToggleReopenLast(object? sender, RoutedEventArgs e)
    {
        _reopenLastFile = !_reopenLastFile;
        MenuReopenLast.IsChecked = _reopenLastFile;
    }

    // ---- Theme ----

    private void OnThemeSystem(object? s, RoutedEventArgs e) => ApplyThemePreference(ThemePreference.System);
    private void OnThemeLight(object? s, RoutedEventArgs e) => ApplyThemePreference(ThemePreference.Light);
    private void OnThemeDark(object? s, RoutedEventArgs e) => ApplyThemePreference(ThemePreference.Dark);

    private void ApplyThemePreference(ThemePreference preference)
    {
        _themePreference = preference;

        Application.Current!.RequestedThemeVariant = preference switch
        {
            ThemePreference.Light => ThemeVariant.Light,
            ThemePreference.Dark => ThemeVariant.Dark,
            _ => ThemeVariant.Default,
        };

        MenuThemeSystem.IsChecked = preference == ThemePreference.System;
        MenuThemeLight.IsChecked = preference == ThemePreference.Light;
        MenuThemeDark.IsChecked = preference == ThemePreference.Dark;

        UpdateEditorTheme();
    }

    /// <summary>Effective darkness; trusts the explicit preference because ActualThemeVariant can lag a switch.</summary>
    private bool IsDarkNow => _themePreference switch
    {
        ThemePreference.Dark => true,
        ThemePreference.Light => false,
        _ => ActualThemeVariant == ThemeVariant.Dark,
    };

    private void UpdateEditorTheme()
    {
        var dark = IsDarkNow;
        _textMate?.SetTheme(_registryOptions.LoadTheme(dark ? ThemeName.DarkPlus : ThemeName.LightPlus));

        Editor.TextArea.TextView.CurrentLineBackground = new Avalonia.Media.SolidColorBrush(
            Avalonia.Media.Color.Parse(dark ? "#0DFFFFFF" : "#08000000"));
        Editor.TextArea.TextView.CurrentLineBorder = new Avalonia.Media.Pen(Avalonia.Media.Brushes.Transparent, 0);

        RefreshRichStyling();
    }

    // ---- Recent files ----

    private void RememberRecentFile(string path)
    {
        var s = _settingsService.Settings;
        s.RecentFiles = RecentFiles.Add(s.RecentFiles, path);
        _settingsService.Save();
        RebuildRecentMenu();
    }

    private void ForgetRecentFile(string path)
    {
        var s = _settingsService.Settings;
        s.RecentFiles = RecentFiles.Remove(s.RecentFiles, path);
        _settingsService.Save();
        RebuildRecentMenu();
    }

    private void RebuildRecentMenu()
    {
        RecentMenu.Items.Clear();

        foreach (var path in _settingsService.Settings.RecentFiles)
        {
            var captured = path;
            var item = new MenuItem { Header = path.Replace("_", "__") };
            item.Click += async (_, _) =>
            {
                if (await ConfirmLoseChangesAsync())
                    await LoadFileAsync(captured);
            };
            RecentMenu.Items.Add(item);
        }

        if (RecentMenu.Items.Count > 0)
        {
            RecentMenu.Items.Add(new Separator());
            var clear = new MenuItem { Header = "_Clear Recent Files" };
            clear.Click += (_, _) =>
            {
                _settingsService.Settings.RecentFiles = new List<string>();
                _settingsService.Save();
                RebuildRecentMenu();
            };
            RecentMenu.Items.Add(clear);
        }

        RecentMenu.IsEnabled = RecentMenu.Items.Count > 0;
    }
}
