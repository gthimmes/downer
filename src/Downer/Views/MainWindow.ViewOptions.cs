using Avalonia.Interactivity;
using AvaloniaEdit.Search;
using Downer.Core;

namespace Downer.Views;

public partial class MainWindow
{
    private const double DefaultFontSize = 14;
    private const double MinFontSize = 8;
    private const double MaxFontSize = 40;

    private SearchPanel _searchPanel = null!;

    private void SetUpViewOptions()
    {
        _searchPanel = SearchPanel.Install(Editor);
        UpdateCountsStatus();
    }

    // ---- Find / replace ----

    private void OnFind(object? sender, RoutedEventArgs e) => OpenSearch(replaceMode: false);
    private void OnReplace(object? sender, RoutedEventArgs e) => OpenSearch(replaceMode: true);

    private void OpenSearch(bool replaceMode)
    {
        _searchPanel.IsReplaceMode = replaceMode;

        var selection = Editor.SelectedText;
        if (!string.IsNullOrEmpty(selection) && !selection.Contains('\n'))
            _searchPanel.SearchPattern = selection;

        _searchPanel.Open();
    }

    // ---- Word wrap / line numbers ----

    private void OnToggleWordWrap(object? sender, RoutedEventArgs e)
    {
        Editor.WordWrap = !Editor.WordWrap;
        Editor.HorizontalScrollBarVisibility = Editor.WordWrap
            ? Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled
            : Avalonia.Controls.Primitives.ScrollBarVisibility.Auto;
        MenuWordWrap.IsChecked = Editor.WordWrap;
    }

    private void OnToggleLineNumbers(object? sender, RoutedEventArgs e)
    {
        Editor.ShowLineNumbers = !Editor.ShowLineNumbers;
        MenuLineNumbers.IsChecked = Editor.ShowLineNumbers;
    }

    // ---- Zoom ----

    private void OnZoomIn(object? sender, RoutedEventArgs e) => SetFontSize(Editor.FontSize + 1);
    private void OnZoomOut(object? sender, RoutedEventArgs e) => SetFontSize(Editor.FontSize - 1);
    private void OnZoomReset(object? sender, RoutedEventArgs e) => SetFontSize(DefaultFontSize);

    private void SetFontSize(double size) =>
        Editor.FontSize = Math.Clamp(size, MinFontSize, MaxFontSize);

    // ---- Status bar counts ----

    private void UpdateCountsStatus()
    {
        var stats = DocumentStats.Count(Editor.Text);
        CountsText.Text = $"{stats.Words} words · {stats.Characters} chars · {stats.Lines} lines";
    }
}
