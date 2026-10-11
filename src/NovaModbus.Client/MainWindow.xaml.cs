using System.Windows;
using System.Windows.Input;
using NovaModbus.Client.ViewModels;

namespace NovaModbus.Client;

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

    private void OnWatchDeleteKey(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.Delete)
        {
            return;
        }

        OnWatchDelete(sender, e);
    }

    private void OnWatchDelete(object sender, RoutedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.Watch.RemoveEntries(WatchGrid.SelectedItems.OfType<WatchEntry>());
            e.Handled = true;
        }
    }
}
