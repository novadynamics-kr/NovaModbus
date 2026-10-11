using System.Windows;
using NovaModbus.Slave.ViewModels;

namespace NovaModbus.Slave;

/// <summary>Interaction logic for MainWindow.xaml.</summary>
public partial class MainWindow : Window
{
    /// <summary>Initializes the window with its root view model.</summary>
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
        Closed += OnClosed;
    }

    private async void OnClosed(object? sender, EventArgs e)
    {
        if (DataContext is IAsyncDisposable disposable)
        {
            await disposable.DisposeAsync().ConfigureAwait(true);
        }
    }
}
