using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using NovaModbus.Slave.ViewModels;
using NovaModbus.Slave.Views;

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

    private MainViewModel ViewModel => (MainViewModel)DataContext;

    private async void OnClosed(object? sender, EventArgs e)
    {
        if (DataContext is IAsyncDisposable disposable)
        {
            await disposable.DisposeAsync().ConfigureAwait(true);
        }
    }

    private void OnHoldingDelete(object sender, RoutedEventArgs e)
    {
        // Use IsSelected property instead of DataGrid SelectedItems
        var toDelete = ViewModel.HoldingRows.Where(x => x.IsSelected).Select(x => x.Address).ToArray();
        if (toDelete.Length > 0)
        {
            ViewModel.RemoveHoldings(toDelete);
        }
    }

    private void OnCoilDelete(object sender, RoutedEventArgs e)
    {
        var toDelete = ViewModel.CoilRows.Where(x => x.IsSelected).Select(x => x.Address).ToArray();
        if (toDelete.Length > 0)
        {
            ViewModel.RemoveCoils(toDelete);
        }
    }

    private void OnGridDeleteKey(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.A && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
        {
            // Ctrl+A: use ViewModel command (instant, no UI freeze)
            if (sender == HoldingGrid)
                ViewModel.SelectAllHoldingsCommand.Execute(null);
            else if (sender == CoilGrid)
                ViewModel.SelectAllCoilsCommand.Execute(null);
            e.Handled = true;
            return;
        }

        if (e.Key != Key.Delete)
        {
            return;
        }

        if (sender == HoldingGrid)
        {
            OnHoldingDelete(sender, e);
        }
        else if (sender == CoilGrid)
        {
            OnCoilDelete(sender, e);
        }

        e.Handled = true;
    }

    private void OnHoldingRightClick(object sender, MouseButtonEventArgs e)
    {
        DataGridRow? row = FindRow(e.OriginalSource as DependencyObject);
        if (row?.Item is not RegisterRow register)
        {
            return;
        }

        FloatInputDialog dialog = new(register.Address) { Owner = this };
        if (dialog.ShowDialog() == true)
        {
            ViewModel.WriteFloatAt(register.Address, dialog.Value, dialog.Order.Order);
        }
    }

    private static DataGridRow? FindRow(DependencyObject? source)
    {
        DependencyObject? current = source;
        while (current is not null && current is not DataGridRow)
        {
            current = VisualTreeHelper.GetParent(current);
        }

        return current as DataGridRow;
    }
}