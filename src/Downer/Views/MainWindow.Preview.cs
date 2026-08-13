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

    internal void RefreshPreview()
    {
        if (_viewMode != ViewMode.EditorOnly)
            Preview.Markdown = Editor.Text;
        UpdateCountsStatus();
    }

    // ---- Scroll sync (bidirectional, proportional) ----
    // The pane under the pointer is the driver: the editor pushes to the preview
    // unless the pointer sits over the preview, and vice versa. A reentrancy flag
    // stops the panes from ping-ponging within one sync.

    private bool _syncingScroll;

    private void EnsurePreviewScroller()
    {
        if (_previewScroller is not null)
            return;

        _previewScroller = Preview.GetVisualDescendants().OfType<ScrollViewer>().FirstOrDefault();
        if (_previewScroller is not null)
            _previewScroller.ScrollChanged += OnPreviewScrolled;
    }

    internal void SyncPreviewScroll()
    {
        if (_viewMode != ViewMode.Split || _syncingScroll || Preview.IsPointerOver)
            return;

        EnsurePreviewScroller();
        if (_previewScroller is null)
            return;

        var textView = Editor.TextArea.TextView;
        var target = Core.ScrollSync.MapOffset(
            textView.ScrollOffset.Y,
            textView.DocumentHeight - textView.Bounds.Height,
            _previewScroller.Extent.Height - _previewScroller.Viewport.Height);
        if (target is null || Math.Abs(target.Value - _previewScroller.Offset.Y) < 0.5)
            return;

        _syncingScroll = true;
        try
        {
            _previewScroller.Offset = new Vector(_previewScroller.Offset.X, target.Value);
        }
        finally
        {
            _syncingScroll = false;
        }
    }

    private void OnPreviewScrolled(object? sender, ScrollChangedEventArgs e)
    {
        if (_viewMode != ViewMode.Split || _syncingScroll || !Preview.IsPointerOver)
            return;

        SyncEditorToPreview();
    }

    /// <summary>Mirrors the preview's scroll fraction onto the editor.</summary>
    internal void SyncEditorToPreview()
    {
        if (_previewScroller is null)
            return;

        var textView = Editor.TextArea.TextView;
        var target = Core.ScrollSync.MapOffset(
            _previewScroller.Offset.Y,
            _previewScroller.Extent.Height - _previewScroller.Viewport.Height,
            textView.DocumentHeight - textView.Bounds.Height);
        if (target is null || Math.Abs(target.Value - textView.ScrollOffset.Y) < 0.5)
            return;

        _syncingScroll = true;
        try
        {
            Editor.ScrollToVerticalOffset(target.Value);
        }
        finally
        {
            _syncingScroll = false;
        }
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
        if (mode == ViewMode.Split)
            Dispatcher.UIThread.Post(EnsurePreviewScroller, DispatcherPriority.Loaded);
        if (mode != ViewMode.PreviewOnly)
            Editor.Focus();
    }
}
