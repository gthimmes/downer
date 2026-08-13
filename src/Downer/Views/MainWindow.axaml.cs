using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Styling;
using Avalonia.Threading;
using AvaloniaEdit.TextMate;
using Downer.Dialogs;
using TextMateSharp.Grammars;

namespace Downer.Views;

public partial class MainWindow : Window
{
    private readonly string[] _startupArgs;

    private RegistryOptions _registryOptions = null!;
    private TextMate.Installation? _textMate;

    private bool _forceClose;

    public MainWindow() : this(Array.Empty<string>())
    {
    }

    public MainWindow(string[] args)
    {
        _startupArgs = args;
        InitializeComponent();

        SetUpTabs();
        SetUpEditor();
        SetUpEditingBehaviors();
        SetUpAutosave();
        SetUpSpellCheck();
        SetUpPreview();
        SetUpViewOptions();
        SetUpSettings();
        SetUpDragDrop();

        Loaded += OnWindowLoaded;
    }

    private bool IsDirty => !Editor.Document.UndoStack.IsOriginalFile;

    private string DisplayFileName => _activeTab.Title;

    private void SetUpEditor()
    {
        Editor.Options.EnableHyperlinks = false;
        Editor.Options.AllowScrollBelowDocument = true;
        Editor.Options.HighlightCurrentLine = true;

        var isDark = ActualThemeVariant == ThemeVariant.Dark;
        _registryOptions = new RegistryOptions(isDark ? ThemeName.DarkPlus : ThemeName.LightPlus);

        Editor.TextChanged += (_, _) => OnDocumentTextChanged();
        Editor.TextArea.Caret.PositionChanged += (_, _) =>
        {
            UpdateCaretStatus();
            TrackRevealedLine();
        };

        Editor.Document.UndoStack.MarkAsOriginalFile();
        UpdateWindowChrome();
    }

    private async void OnWindowLoaded(object? sender, RoutedEventArgs e)
    {
        SetUpShortcuts();
        Editor.Focus();

        var fileArg = _startupArgs.FirstOrDefault(File.Exists);
        if (fileArg is not null)
        {
            await LoadFileAsync(Path.GetFullPath(fileArg));
            return;
        }

        if (_reopenLastFile)
        {
            var s = _settingsService.Settings;
            var files = s.OpenFiles.Where(File.Exists).ToList();
            if (files.Count == 0 && s.LastFilePath is not null && File.Exists(s.LastFilePath))
                files.Add(s.LastFilePath); // settings written before tabs existed

            foreach (var file in files)
                await LoadFileAsync(file);
            if (s.LastFilePath is not null && File.Exists(s.LastFilePath))
                await LoadFileAsync(s.LastFilePath); // re-activates the tab that was in front

            if (files.Count > 0)
                return;
        }

        ShowWelcomeDocument();
    }

    private void SetUpShortcuts()
    {
        var cmd = PlatformSettings?.HotkeyConfiguration?.CommandModifiers ?? KeyModifiers.Control;

        // Display-only gestures: AvaloniaEdit already binds these keys internally.
        MenuUndo.InputGesture = new KeyGesture(Key.Z, cmd);
        MenuRedo.InputGesture = cmd == KeyModifiers.Meta
            ? new KeyGesture(Key.Z, cmd | KeyModifiers.Shift)
            : new KeyGesture(Key.Y, cmd);
        MenuCut.InputGesture = new KeyGesture(Key.X, cmd);
        MenuCopy.InputGesture = new KeyGesture(Key.C, cmd);
        MenuPaste.InputGesture = new KeyGesture(Key.V, cmd);
        MenuSelectAll.InputGesture = new KeyGesture(Key.A, cmd);

        Shortcut(MenuNew, Key.N, cmd, () => _ = NewFileAsync());
        Shortcut(MenuOpen, Key.O, cmd, () => _ = OpenFileAsync());
        Shortcut(MenuCloseTab, Key.W, cmd, () => _ = CloseTabAsync(_activeTab));
        Shortcut(MenuSave, Key.S, cmd, () => _ = SaveAsync());
        Shortcut(MenuSaveAs, Key.S, cmd | KeyModifiers.Shift, () => _ = SaveAsAsync());

        // Tab cycling has no menu items; bind the gestures directly.
        KeyBindings.Add(new KeyBinding
        {
            Gesture = new KeyGesture(Key.Tab, KeyModifiers.Control),
            Command = new SimpleCommand(() => CycleTab(1)),
        });
        KeyBindings.Add(new KeyBinding
        {
            Gesture = new KeyGesture(Key.Tab, KeyModifiers.Control | KeyModifiers.Shift),
            Command = new SimpleCommand(() => CycleTab(-1)),
        });

        Shortcut(MenuModeSource, Key.E, cmd, ToggleEditorMode);
        Shortcut(MenuViewEditor, Key.D1, cmd, () => SetViewMode(ViewMode.EditorOnly));
        Shortcut(MenuViewSplit, Key.D2, cmd, () => SetViewMode(ViewMode.Split));
        Shortcut(MenuViewPreview, Key.D3, cmd, () => SetViewMode(ViewMode.PreviewOnly));

        Shortcut(MenuBold, Key.B, cmd, () => OnBold(null, null!));
        Shortcut(MenuItalic, Key.I, cmd, () => OnItalic(null, null!));
        Shortcut(MenuStrike, Key.X, cmd | KeyModifiers.Shift, () => OnStrikethrough(null, null!));
        Shortcut(MenuInlineCode, Key.C, cmd | KeyModifiers.Shift, () => OnInlineCode(null, null!));
        Shortcut(MenuLink, Key.K, cmd, () => OnInsertLink(null, null!));
        Shortcut(MenuImage, Key.K, cmd | KeyModifiers.Shift, () => OnInsertImage(null, null!));
        Shortcut(MenuBullet, Key.D8, cmd | KeyModifiers.Shift, () => OnBulletList(null, null!));
        Shortcut(MenuOrdered, Key.D7, cmd | KeyModifiers.Shift, () => OnOrderedList(null, null!));
        Shortcut(MenuTask, Key.D9, cmd | KeyModifiers.Shift, () => OnTaskList(null, null!));
        Shortcut(MenuQuote, Key.Q, cmd | KeyModifiers.Shift, () => OnBlockquote(null, null!));
        Shortcut(MenuFormatTable, Key.T, cmd | KeyModifiers.Shift, () => OnFormatTable(null, null!));

        Shortcut(MenuFind, Key.F, cmd, () => OpenSearch(replaceMode: false));
        Shortcut(MenuReplace, Key.H, cmd, () => OpenSearch(replaceMode: true));
        Shortcut(MenuZoomIn, Key.OemPlus, cmd, () => OnZoomIn(null, null!));
        Shortcut(MenuZoomOut, Key.OemMinus, cmd, () => OnZoomOut(null, null!));
        Shortcut(MenuZoomReset, Key.D0, cmd, () => OnZoomReset(null, null!));
    }

    private void Shortcut(MenuItem item, Key key, KeyModifiers modifiers, Action action)
    {
        var gesture = new KeyGesture(key, modifiers);
        item.InputGesture = gesture;
        KeyBindings.Add(new KeyBinding { Gesture = gesture, Command = new SimpleCommand(action) });
    }

    private void OnDocumentTextChanged()
    {
        UpdateWindowChrome();
        RefreshFenceStates();
        SchedulePreviewRefresh();
        ScheduleAutosave();
    }

    private void UpdateWindowChrome()
    {
        var dirtyMarker = IsDirty ? "● " : "";
        Title = $"{dirtyMarker}{DisplayFileName} — Downer";
        FileText.Text = CurrentFilePath ?? "Untitled";
        RebuildTabStrip();
    }

    private void UpdateCaretStatus()
    {
        var caret = Editor.TextArea.Caret;
        PositionText.Text = $"Ln {caret.Line}, Col {caret.Column}";
    }

    // ---- Menu handlers ----

    private void OnNew(object? sender, RoutedEventArgs e) => _ = NewFileAsync();
    private void OnOpen(object? sender, RoutedEventArgs e) => _ = OpenFileAsync();
    private void OnSave(object? sender, RoutedEventArgs e) => _ = SaveAsync();
    private void OnSaveAs(object? sender, RoutedEventArgs e) => _ = SaveAsAsync();
    private void OnExit(object? sender, RoutedEventArgs e) => Close();

    private void OnUndo(object? sender, RoutedEventArgs e) => Editor.Undo();
    private void OnRedo(object? sender, RoutedEventArgs e) => Editor.Redo();
    private void OnCut(object? sender, RoutedEventArgs e) => Editor.Cut();
    private void OnCopy(object? sender, RoutedEventArgs e) => Editor.Copy();
    private void OnPaste(object? sender, RoutedEventArgs e) => Editor.Paste();
    private void OnSelectAll(object? sender, RoutedEventArgs e) => Editor.SelectAll();

    // ---- Close guard ----

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        if (!_forceClose && _tabs.Any(t => t.IsDirty))
        {
            e.Cancel = true;
            Dispatcher.UIThread.Post(async () =>
            {
                foreach (var tab in _tabs.Where(t => t.IsDirty).ToList())
                {
                    ActivateTab(tab); // show the user what they are deciding about

                    // With autosave on, a titled document just saves instead of prompting.
                    if (AutosaveApplies)
                    {
                        if (!await SaveAsync())
                            return;
                        continue;
                    }

                    var choice = await AppDialogs.ConfirmUnsavedAsync(this, tab.Title);
                    if (choice == UnsavedChoice.Cancel)
                        return;
                    if (choice == UnsavedChoice.Save && !await SaveAsync())
                        return;
                }

                _forceClose = true;
                Close();
            });
        }

        base.OnClosing(e);
    }
}

internal sealed class SimpleCommand(Action action) : System.Windows.Input.ICommand
{
    public event EventHandler? CanExecuteChanged { add { } remove { } }

    public bool CanExecute(object? parameter) => true;

    public void Execute(object? parameter) => action();
}
