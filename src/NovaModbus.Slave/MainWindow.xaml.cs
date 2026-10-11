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

    private void OnHoldingDelete(object sender, RoutedEventArgs e) =>
        DeleteFrom(HoldingGrid, ViewModel.RemoveHoldings, item => ((RegisterRow)item).Address);

    private void OnCoilDelete(object sender, RoutedEventArgs e) =>
        DeleteFrom(CoilGrid, ViewModel.RemoveCoils, item => ((CoilRow)item).Address);

    private static void DeleteFrom(DataGrid grid, Action<IEnumerable<ushort>> remove, Func<object, ushort> addressOf)
    {
        ushort[] addresses = grid.SelectedItems.Cast<object>().Select(addressOf).ToArray();
        if (addresses.Length > 0)
        {
            remove(addresses);
        }
    }

    private void OnGridDeleteKey(object sender, KeyEventArgs e)
    {
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
