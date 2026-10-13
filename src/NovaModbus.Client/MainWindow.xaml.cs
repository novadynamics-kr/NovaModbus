using Microsoft.UI.Xaml;
using NovaModbus.Client.ViewModels;

namespace NovaModbus.Client;

/// <summary>Main window: serial settings + function tabs.</summary>
/// <remarks>WinUI Window has no DataContext; the root element carries it.</remarks>
public sealed partial class MainWindow : Window
{
    /// <summary>Initializes the window with its root view model.</summary>
    public MainWindow()
    {
        InitializeComponent();
        Root.DataContext = new MainViewModel();
        Closed += OnClosed;
    }

    private async void OnClosed(object? sender, WindowEventArgs args)
    {
        if (Root.DataContext is IAsyncDisposable disposable)
        {
            await disposable.DisposeAsync();
        }
    }
}
