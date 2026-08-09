using Avalonia.Controls;

namespace Downer.Views;

public partial class MainWindow : Window
{
    private readonly string[] _startupArgs;

    public MainWindow() : this(Array.Empty<string>())
    {
    }

    public MainWindow(string[] args)
    {
        _startupArgs = args;
        InitializeComponent();
    }
}
