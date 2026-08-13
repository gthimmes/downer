using Avalonia.Interactivity;
using Downer.Services;

namespace Downer.Views;

public partial class MainWindow
{
    private readonly SpellCheckTransformer _spellTransformer = new();
    private readonly SpellChecker _spellChecker = new();
    private bool _spellCheckEnabled;

    private void SetUpSpellCheck()
    {
        _spellTransformer.IsCorrect = word => _spellChecker.IsCorrect(word);
        Editor.TextArea.TextView.LineTransformers.Add(_spellTransformer);
    }

    private void OnToggleSpellCheck(object? sender, RoutedEventArgs e) =>
        SetSpellCheckEnabled(!_spellCheckEnabled);

    internal void SetSpellCheckEnabled(bool enabled)
    {
        _spellCheckEnabled = enabled;
        MenuSpellCheck.IsChecked = enabled;
        _spellTransformer.Enabled = enabled;

        if (enabled && !_spellChecker.IsLoaded)
            _ = LoadSpellDictionaryAsync();

        Editor.TextArea.TextView.Redraw();
    }

    private async Task LoadSpellDictionaryAsync()
    {
        await _spellChecker.LoadAsync();
        if (_spellCheckEnabled)
            Editor.TextArea.TextView.Redraw();
    }
}
