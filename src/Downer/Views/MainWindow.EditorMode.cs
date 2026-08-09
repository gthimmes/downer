using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Styling;
using AvaloniaEdit.TextMate;
using Downer.Core;
using TextMateSharp.Grammars;

namespace Downer.Views;

public enum EditorSurfaceMode
{
    Rich,
    Source,
}

public partial class MainWindow
{
    private static readonly FontFamily SansFont = new("Inter, Segoe UI, Helvetica Neue, Helvetica, Arial, sans-serif");
    private static readonly FontFamily MonoFont = new("Cascadia Code,Consolas,Menlo,DejaVu Sans Mono,monospace");

    private readonly RichMarkdownTransformer _richTransformer = new();
    private EditorSurfaceMode _editorMode = EditorSurfaceMode.Rich;
    private FenceLineState[] _lastFenceStates = Array.Empty<FenceLineState>();

    private void OnModeRich(object? sender, RoutedEventArgs e) => SetEditorMode(EditorSurfaceMode.Rich);
    private void OnModeSource(object? sender, RoutedEventArgs e) => SetEditorMode(EditorSurfaceMode.Source);

    private void ToggleEditorMode() =>
        SetEditorMode(_editorMode == EditorSurfaceMode.Rich ? EditorSurfaceMode.Source : EditorSurfaceMode.Rich);

    private void SetEditorMode(EditorSurfaceMode mode)
    {
        _editorMode = mode;

        if (mode == EditorSurfaceMode.Rich)
        {
            _textMate?.Dispose();
            _textMate = null;

            Editor.FontFamily = SansFont;
            _richTransformer.MonoFont = MonoFont;
            _richTransformer.BaseFontSize = Editor.FontSize;
            _richTransformer.Palette = ActualThemeVariant == ThemeVariant.Dark ? RichPalette.Dark : RichPalette.Light;
            RefreshFenceStates(force: true);

            if (!Editor.TextArea.TextView.LineTransformers.Contains(_richTransformer))
                Editor.TextArea.TextView.LineTransformers.Add(_richTransformer);
        }
        else
        {
            Editor.TextArea.TextView.LineTransformers.Remove(_richTransformer);
            Editor.FontFamily = MonoFont;

            if (_textMate is null)
            {
                _textMate = Editor.InstallTextMate(_registryOptions);
                _textMate.SetGrammar(_registryOptions.GetScopeByLanguageId("markdown"));
                UpdateEditorTheme();
            }
        }

        MenuModeRich.IsChecked = mode == EditorSurfaceMode.Rich;
        MenuModeSource.IsChecked = mode == EditorSurfaceMode.Source;
        ModeText.Text = mode == EditorSurfaceMode.Rich ? "Formatted" : "Source";
        ApplyLineNumberVisibility();

        Editor.TextArea.TextView.Redraw();
        Editor.Focus();
    }

    /// <summary>Recomputes the fence map; redraws fully only when regions actually moved.</summary>
    private void RefreshFenceStates(bool force = false)
    {
        if (_editorMode != EditorSurfaceMode.Rich && !force)
            return;

        var states = CodeFences.Analyze(Editor.Text);
        var changed = !states.AsSpan().SequenceEqual(_lastFenceStates);
        _lastFenceStates = states;
        _richTransformer.FenceStates = states;

        if (changed && !force)
            Editor.TextArea.TextView.Redraw();
    }

    /// <summary>Rich-mode colors and metrics that depend on theme or zoom.</summary>
    private void RefreshRichStyling()
    {
        _richTransformer.Palette = ActualThemeVariant == ThemeVariant.Dark ? RichPalette.Dark : RichPalette.Light;
        _richTransformer.BaseFontSize = Editor.FontSize;
        if (_editorMode == EditorSurfaceMode.Rich)
            Editor.TextArea.TextView.Redraw();
    }
}
