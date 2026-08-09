using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Downer.Views;

public enum ViewMode
{
    EditorOnly,
    Split,
    PreviewOnly,
}

public partial class MainWindow
{
    private DispatcherTimer _previewTimer = null!;
    private ScrollViewer? _previewScroller;
    private ViewMode _viewMode = ViewMode.Split;

    private void SetUpPreview()
    {
        _previewTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(300) };
        _previewTimer.Tick += (_, _) =>
        {
            _previewTimer.Stop();
            RefreshPreview();
        };

        Editor.TextArea.TextView.ScrollOffsetChanged += (_, _) => SyncPreviewScroll();
        RefreshPreview();
    }

    /// <summary>Called on every text change; restarts the debounce timer.</summary>
    private void SchedulePreviewRefresh()
    {
        _previewTimer.Stop();
        _previewTimer.Start();
    }

    private void RefreshPreview()
    {
        if (_viewMode != ViewMode.EditorOnly)
            Preview.Markdown = Editor.Text;
    }

    // ---- Scroll sync (editor -> preview, proportional) ----

    private void SyncPreviewScroll()
    {
        if (_viewMode != ViewMode.Split)
            return;

        _previewScroller ??= Preview.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault();
        if (_previewScroller is null)
            return;

        var textView = Editor.TextArea.TextView;
        var scrollableHeight = textView.DocumentHeight - textView.Bounds.Height;
        if (scrollableHeight <= 0)
            return;

        var fraction = Math.Clamp(textView.ScrollOffset.Y / scrollableHeight, 0, 1);
        var target = fraction * Math.Max(0, _previewScroller.Extent.Height - _previewScroller.Viewport.Height);
        _previewScroller.Offset = new Vector(_previewScroller.Offset.X, target);
    }

    // ---- View modes ----

    private void OnViewEditor(object? sender, RoutedEventArgs e) => SetViewMode(ViewMode.EditorOnly);
    private void OnViewSplit(object? sender, RoutedEventArgs e) => SetViewMode(ViewMode.Split);
    private void OnViewPreview(object? sender, RoutedEventArgs e) => SetViewMode(ViewMode.PreviewOnly);

    private void SetViewMode(ViewMode mode)
    {
        _viewMode = mode;

        var columns = EditorGrid.ColumnDefinitions;
        switch (mode)
        {
            case ViewMode.EditorOnly:
                columns[0].Width = new GridLength(1, GridUnitType.Star);
                columns[1].Width = new GridLength(0);
                columns[2].Width = new GridLength(0);
                break;
            case ViewMode.Split:
                columns[0].Width = new GridLength(1, GridUnitType.Star);
                columns[1].Width = new GridLength(4);
                columns[2].Width = new GridLength(1, GridUnitType.Star);
                break;
            case ViewMode.PreviewOnly:
                columns[0].Width = new GridLength(0);
                columns[1].Width = new GridLength(0);
                columns[2].Width = new GridLength(1, GridUnitType.Star);
                break;
        }

        Editor.IsVisible = mode != ViewMode.PreviewOnly;
        PaneSplitter.IsVisible = mode == ViewMode.Split;
        Preview.IsVisible = mode != ViewMode.EditorOnly;

        MenuViewEditor.IsChecked = mode == ViewMode.EditorOnly;
        MenuViewSplit.IsChecked = mode == ViewMode.Split;
        MenuViewPreview.IsChecked = mode == ViewMode.PreviewOnly;

        if (mode != ViewMode.EditorOnly)
            RefreshPreview();
        if (mode != ViewMode.PreviewOnly)
            Editor.Focus();
    }
}
