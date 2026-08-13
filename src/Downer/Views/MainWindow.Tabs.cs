using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Downer.Dialogs;

namespace Downer.Views;

public partial class MainWindow
{
    private readonly List<DocumentTab> _tabs = new();
    private DocumentTab _activeTab = null!;

    internal IReadOnlyList<DocumentTab> Tabs => _tabs;
    internal DocumentTab ActiveTab => _activeTab;

    /// <summary>The active tab's file path; all single-document code routes through this.</summary>
    private string? CurrentFilePath
    {
        get => _activeTab.FilePath;
        set => _activeTab.FilePath = value;
    }

    private void SetUpTabs()
    {
        var tab = new DocumentTab();
        tab.Document.UndoStack.MarkAsOriginalFile();
        _tabs.Add(tab);
        _activeTab = tab;
        Editor.Document = tab.Document;
    }

    internal DocumentTab AddTab()
    {
        var tab = new DocumentTab();
        tab.Document.UndoStack.MarkAsOriginalFile();
        _tabs.Add(tab);
        return tab;
    }

    internal void ActivateTab(DocumentTab tab)
    {
        if (!ReferenceEquals(_activeTab, tab))
        {
            _activeTab.CaretOffset = Editor.CaretOffset;
            _activeTab = tab;
            Editor.Document = tab.Document;
            Editor.CaretOffset = Math.Clamp(tab.CaretOffset, 0, tab.Document.TextLength);
        }

        UpdateWindowChrome();
        UpdateCaretStatus();
        RefreshFenceStates(force: true);
        Editor.TextArea.TextView.Redraw();
        SchedulePreviewRefresh();
        Editor.Focus();
    }

    /// <summary>Reuses the active tab when it holds nothing worth keeping; otherwise opens a new one.</summary>
    private DocumentTab TabForNewContent()
    {
        if (!_activeTab.IsDirty && _activeTab.FilePath is null
            && (_activeTab.Document.TextLength == 0 || _activeTab.IsWelcome))
            return _activeTab;

        return AddTab();
    }

    private void OnCloseTab(object? sender, Avalonia.Interactivity.RoutedEventArgs e) =>
        _ = CloseTabAsync(_activeTab);

    internal async Task CloseTabAsync(DocumentTab tab)
    {
        if (tab.IsDirty)
        {
            ActivateTab(tab); // show the user what they are being asked about

            if (AutosaveApplies)
            {
                if (!await SaveAsync())
                    return;
            }
            else
            {
                var choice = await AppDialogs.ConfirmUnsavedAsync(this, tab.Title);
                if (choice == UnsavedChoice.Cancel)
                    return;
                if (choice == UnsavedChoice.Save && !await SaveAsync())
                    return;
            }
        }

        RemoveTab(tab);
    }

    internal void RemoveTab(DocumentTab tab)
    {
        var index = _tabs.IndexOf(tab);
        if (index < 0)
            return;

        _tabs.RemoveAt(index);

        if (_tabs.Count == 0)
        {
            ActivateTab(AddTab()); // the window always shows a document
            return;
        }

        if (ReferenceEquals(_activeTab, tab))
            ActivateTab(_tabs[Math.Min(index, _tabs.Count - 1)]);
        else
            RebuildTabStrip();
    }

    internal void CycleTab(int direction)
    {
        if (_tabs.Count < 2)
            return;

        var index = (_tabs.IndexOf(_activeTab) + direction + _tabs.Count) % _tabs.Count;
        ActivateTab(_tabs[index]);
    }

    // ---- Tab strip rendering (visible only with 2+ tabs) ----

    private void RebuildTabStrip()
    {
        TabStripBorder.IsVisible = _tabs.Count > 1;
        TabStrip.Children.Clear();
        if (_tabs.Count <= 1)
            return;

        foreach (var tab in _tabs)
        {
            var captured = tab;
            var isActive = ReferenceEquals(tab, _activeTab);

            var label = new TextBlock
            {
                Text = (tab.IsDirty ? "● " : "") + tab.Title,
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 12,
                FontWeight = isActive ? FontWeight.SemiBold : FontWeight.Normal,
                Foreground = ChromeResourceBrush(isActive ? "ToolIconBrush" : "MutedForeground"),
            };

            var close = new Button
            {
                Content = new TextBlock { Text = "×", FontSize = 13 },
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Padding = new Thickness(5, 0),
                Focusable = false,
                VerticalAlignment = VerticalAlignment.Center,
            };
            close.Click += async (_, _) => await CloseTabAsync(captured);

            var panel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 2 };
            panel.Children.Add(label);
            panel.Children.Add(close);

            var host = new Border
            {
                Child = panel,
                Padding = new Thickness(10, 4, 3, 4),
                CornerRadius = new CornerRadius(6),
                Background = isActive ? ChromeResourceBrush("ChromeHover") : Brushes.Transparent,
            };
            host.PointerPressed += (_, e) =>
            {
                ActivateTab(captured);
                e.Handled = true;
            };

            TabStrip.Children.Add(host);
        }
    }

    private IBrush ChromeResourceBrush(string key) =>
        this.TryFindResource(key, ActualThemeVariant, out var value) && value is IBrush brush
            ? brush
            : Brushes.Transparent;
}
