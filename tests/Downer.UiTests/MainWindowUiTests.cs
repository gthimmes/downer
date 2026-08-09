using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Downer.Services;
using Downer.Views;
using Xunit;

namespace Downer.UiTests;

/// <summary>
/// Headless UI tests: the whole window boots, real keyboard input drives the
/// editor, and rendered frames are captured â€” Avalonia's Playwright-equivalent.
/// </summary>
public class MainWindowUiTests
{
    static MainWindowUiTests()
    {
        // Never touch the user's real settings from tests.
        SettingsService.OverrideDirectory =
            Path.Combine(Path.GetTempPath(), "downer-uitests-" + Guid.NewGuid().ToString("N"));
    }

    private static MainWindow OpenWindow()
    {
        var window = new MainWindow();
        window.Show();
        // Flush the Loaded event (welcome document, shortcuts) so tests start deterministic.
        Dispatcher.UIThread.RunJobs();
        return window;
    }

    private static void CleanClose(MainWindow window)
    {
        // Bypass the unsaved-changes dialog: tests dirty the document on purpose.
        window.Editor.Document.UndoStack.MarkAsOriginalFile();
        window.Close();
    }

    [AvaloniaFact]
    public void Boots_into_formatted_mode_with_welcome_document()
    {
        var window = OpenWindow();

        Assert.Equal("Formatted", window.ModeText.Text);
        Assert.False(window.Editor.ShowLineNumbers);
        Assert.Contains("Welcome to Downer", window.Editor.Text);

        CleanClose(window);
    }

    [AvaloniaFact]
    public void Ctrl_E_toggles_between_formatted_and_source()
    {
        var window = OpenWindow();

        window.KeyPressQwerty(PhysicalKey.E, RawInputModifiers.Control);
        Assert.Equal("Source", window.ModeText.Text);
        Assert.True(window.Editor.ShowLineNumbers);

        window.KeyPressQwerty(PhysicalKey.E, RawInputModifiers.Control);
        Assert.Equal("Formatted", window.ModeText.Text);
        Assert.False(window.Editor.ShowLineNumbers);

        CleanClose(window);
    }

    [AvaloniaFact]
    public void Ctrl_B_wraps_the_selection_in_bold()
    {
        var window = OpenWindow();
        window.Editor.Document.Text = "hello world";
        window.Editor.Select(0, 5);

        window.KeyPressQwerty(PhysicalKey.B, RawInputModifiers.Control);

        Assert.Equal("**hello** world", window.Editor.Text);

        CleanClose(window);
    }

    [AvaloniaFact]
    public void Typing_flows_into_the_document()
    {
        var window = OpenWindow();
        window.Editor.Document.Text = "";
        window.Editor.TextArea.Focus();
        Dispatcher.UIThread.RunJobs();

        window.KeyTextInput("# Hi there");

        Assert.Equal("# Hi there", window.Editor.Text);

        CleanClose(window);
    }

    [AvaloniaFact]
    public void Enter_continues_a_bullet_list()
    {
        var window = OpenWindow();
        window.Editor.Document.Text = "- item";
        window.Editor.CaretOffset = 6;
        window.Editor.TextArea.Focus();
        Dispatcher.UIThread.RunJobs();

        window.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);

        Assert.Equal("- item\n- ", window.Editor.Text);
        Assert.Equal(9, window.Editor.CaretOffset);

        CleanClose(window);
    }

    [AvaloniaFact]
    public void View_shortcuts_switch_layouts()
    {
        var window = OpenWindow();

        window.KeyPressQwerty(PhysicalKey.Digit2, RawInputModifiers.Control);
        Assert.True(window.Preview.IsVisible);

        window.KeyPressQwerty(PhysicalKey.Digit1, RawInputModifiers.Control);
        Assert.False(window.Preview.IsVisible);

        CleanClose(window);
    }

    [AvaloniaFact]
    public void Ctrl_K_inserts_a_link_template()
    {
        var window = OpenWindow();
        window.Editor.Document.Text = "";
        window.Editor.CaretOffset = 0;

        window.KeyPressQwerty(PhysicalKey.K, RawInputModifiers.Control);

        Assert.Equal("[text](url)", window.Editor.Text);

        CleanClose(window);
    }

    [AvaloniaFact]
    public void Ctrl_Shift_T_formats_the_table_at_the_caret()
    {
        var window = OpenWindow();
        window.Editor.Document.Text = "|a|b|\n|-|-|\n|1|22|";
        window.Editor.CaretOffset = 0;

        window.KeyPressQwerty(PhysicalKey.T, RawInputModifiers.Control | RawInputModifiers.Shift);

        Assert.Equal("| a   | b   |\n| --- | --- |\n| 1   | 22  |", window.Editor.Text);

        CleanClose(window);
    }

    [AvaloniaFact]
    public void Window_renders_a_real_frame()
    {
        var window = OpenWindow();

        var frame = window.CaptureRenderedFrame();

        Assert.NotNull(frame);
        var shotsDir = Path.Combine(AppContext.BaseDirectory, "ui-shots");
        Directory.CreateDirectory(shotsDir);
        var path = Path.Combine(shotsDir, "main-window.png");
        frame.Save(path);
        Assert.True(new FileInfo(path).Length > 0);

        CleanClose(window);
    }
}

