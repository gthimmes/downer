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
    private TextMate.Installation _textMate = null!;

    private string? _currentFilePath;
    private bool _forceClose;

    public MainWindow() : this(Array.Empty<string>())
    {
    }

    public MainWindow(string[] args)
    {
        _startupArgs = args;
        InitializeComponent();

        SetUpEditor();

        Loaded += OnWindowLoaded;
    }

    private bool IsDirty => !Editor.Document.UndoStack.IsOriginalFile;

    private string DisplayFileName =>
        _currentFilePath is null ? "Untitled" : Path.GetFileName(_currentFilePath);

    private void SetUpEditor()
    {
        Editor.Options.EnableHyperlinks = false;
        Editor.Options.AllowScrollBelowDocument = true;
        Editor.Options.HighlightCurrentLine = true;

        var isDark = ActualThemeVariant == ThemeVariant.Dark;
        _registryOptions = new RegistryOptions(isDark ? ThemeName.DarkPlus : ThemeName.LightPlus);
        _textMate = Editor.InstallTextMate(_registryOptions);
        _textMate.SetGrammar(_registryOptions.GetScopeByLanguageId("markdown"));

        Editor.TextChanged += (_, _) => OnDocumentTextChanged();
        Editor.TextArea.Caret.PositionChanged += (_, _) => UpdateCaretStatus();

        Editor.Document.UndoStack.MarkAsOriginalFile();
        UpdateWindowChrome();
    }

    private async void OnWindowLoaded(object? sender, RoutedEventArgs e)
    {
        SetUpShortcuts();
        Editor.Focus();

        var fileArg = _startupArgs.FirstOrDefault(File.Exists);
        if (fileArg is not null)
            await LoadFileAsync(Path.GetFullPath(fileArg));
    }

    private void SetUpShortcuts()
    {
        var cmd = PlatformSettings?.HotkeyConfiguration?.CommandModifiers ?? KeyModifiers.Control;

        Shortcut(MenuNew, Key.N, cmd, () => _ = NewFileAsync());
        Shortcut(MenuOpen, Key.O, cmd, () => _ = OpenFileAsync());
        Shortcut(MenuSave, Key.S, cmd, () => _ = SaveAsync());
        Shortcut(MenuSaveAs, Key.S, cmd | KeyModifiers.Shift, () => _ = SaveAsAsync());
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
    }

    private void UpdateWindowChrome()
    {
        var dirtyMarker = IsDirty ? "● " : "";
        Title = $"{dirtyMarker}{DisplayFileName} — Downer";
        FileText.Text = _currentFilePath ?? "Untitled";
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
        if (!_forceClose && IsDirty)
        {
            e.Cancel = true;
            Dispatcher.UIThread.Post(async () =>
            {
                var choice = await AppDialogs.ConfirmUnsavedAsync(this, DisplayFileName);
                if (choice == UnsavedChoice.Cancel)
                    return;
                if (choice == UnsavedChoice.Save && !await SaveAsync())
                    return;

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
