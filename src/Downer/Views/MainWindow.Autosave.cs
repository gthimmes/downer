using Avalonia.Interactivity;
using Avalonia.Threading;

namespace Downer.Views;

public partial class MainWindow
{
    private DispatcherTimer? _autosaveTimer;
    private bool _autosaveEnabled;

    /// <summary>Autosave only ever writes to a document that already has a path.</summary>
    private bool AutosaveApplies => _autosaveEnabled && _currentFilePath is not null;

    private void SetUpAutosave()
    {
        _autosaveTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(2) };
        _autosaveTimer.Tick += async (_, _) =>
        {
            _autosaveTimer!.Stop();
            await AutosaveNowAsync();
        };
    }

    private void OnToggleAutosave(object? sender, RoutedEventArgs e) => SetAutosaveEnabled(!_autosaveEnabled);

    internal void SetAutosaveEnabled(bool enabled)
    {
        _autosaveEnabled = enabled;
        MenuAutosave.IsChecked = enabled;

        if (enabled)
            ScheduleAutosave();
        else
            _autosaveTimer?.Stop();
    }

    /// <summary>Restarts the idle timer; called on every edit so saves happen after typing pauses.</summary>
    private void ScheduleAutosave()
    {
        if (!AutosaveApplies || _autosaveTimer is null)
            return;

        _autosaveTimer.Stop();
        _autosaveTimer.Start();
    }

    /// <summary>Quiet save for autosave: failures keep the document dirty and retry on the next edit.</summary>
    internal async Task AutosaveNowAsync()
    {
        if (!AutosaveApplies || !IsDirty)
            return;

        var path = _currentFilePath!;
        var text = Editor.Text;
        try
        {
            await File.WriteAllTextAsync(path, text);

            // Only mark clean if nothing changed while the write was in flight.
            if (path == _currentFilePath && string.Equals(text, Editor.Text, StringComparison.Ordinal))
            {
                Editor.Document.UndoStack.MarkAsOriginalFile();
                UpdateWindowChrome();
            }
        }
        catch
        {
            // Best-effort by design; a later edit reschedules the save.
        }
    }
}
