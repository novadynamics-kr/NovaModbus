using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using NovaModbus.Client.ViewModels;
using Windows.Storage.Pickers;
using WinRT.Interop;

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

    private MainViewModel? ViewModel => Root.DataContext as MainViewModel;

    private async void OnClosed(object? sender, WindowEventArgs args)
    {
        if (ViewModel is IAsyncDisposable disposable)
        {
            await disposable.DisposeAsync();
        }
    }

    private void OnWatchSelectAll(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        ViewModel?.Watch.SelectAll();
        args.Handled = true;
    }

    private void OnWatchDeselectAll(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        ViewModel?.Watch.DeselectAll();
        args.Handled = true;
    }

    private void OnWatchDeleteKey(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
    {
        DeleteSelectedWatchEntries();
        args.Handled = true;
    }

    private void OnWatchDelete(object sender, RoutedEventArgs e) => DeleteSelectedWatchEntries();

    private void DeleteSelectedWatchEntries()
    {
        if (ViewModel is MainViewModel vm)
        {
            var doomed = vm.Watch.Entries.Where(x => x.IsSelected).ToArray();
            if (doomed.Length > 0)
            {
                vm.Watch.RemoveEntries(doomed);
            }
        }
    }

    private async void OnWatchSave(object sender, RoutedEventArgs e)
    {
        if (ViewModel is null)
        {
            return;
        }

        var picker = new FileSavePicker
        {
            SuggestedFileName = "watchlist",
        };
        picker.FileTypeChoices.Add("Watch list", [".json"]);
        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(this));
        var file = await picker.PickSaveFileAsync();
        if (file is not null)
        {
            ViewModel.Watch.SaveWatchTo(file.Path);
        }
    }

    private async void OnWatchLoad(object sender, RoutedEventArgs e)
    {
        if (ViewModel is null)
        {
            return;
        }

        var picker = new FileOpenPicker();
        picker.FileTypeFilter.Add(".json");
        InitializeWithWindow.Initialize(picker, WindowNative.GetWindowHandle(this));
        var file = await picker.PickSingleFileAsync();
        if (file is not null)
        {
            ViewModel.Watch.LoadWatchFrom(file.Path);
        }
    }
}
