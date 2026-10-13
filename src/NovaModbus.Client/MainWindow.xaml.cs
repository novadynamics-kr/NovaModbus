using System.Collections.Specialized;
using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using NovaModbus.Client.ViewModels;
using NovaModbus.Rtu.Collections;
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
        EntriesList.SelectionChanged += OnWatchSelectionChanged;
        ResultsList.SelectionChanged += OnWatchSelectionChanged;
        if (ViewModel is MainViewModel vm)
        {
            vm.Watch.Entries.CollectionChanged += OnWatchCollectionChanged;
            AttachSelectionForwarding();
            vm.Watch.SelectionBulkChanged += OnWatchBulkChanged;
        }
    }

    private bool _syncingSelection;

    private void AttachSelectionForwarding()
    {
        if (ViewModel is not MainViewModel vm)
        {
            return;
        }

        foreach (WatchEntry entry in vm.Watch.Entries)
        {
            entry.PropertyChanged += OnWatchItemSelection;
        }
    }

    private void OnWatchCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action == NotifyCollectionChangedAction.Reset)
        {
            AttachSelectionForwarding();
        }
        else if (e.NewItems is not null)
        {
            foreach (WatchEntry entry in e.NewItems.OfType<WatchEntry>())
            {
                entry.PropertyChanged += OnWatchItemSelection;
            }
        }
    }

    private void OnWatchItemSelection(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(WatchEntry.IsSelected) || sender is not WatchEntry entry)
        {
            return;
        }

        SyncListsFromVm();
    }

    private void OnWatchBulkChanged(object? sender, EventArgs e) => SyncListsFromVm();

    private void OnWatchSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_syncingSelection || ViewModel is null)
        {
            return;
        }

        _syncingSelection = true;
        try
        {
            foreach (WatchEntry entry in e.AddedItems.OfType<WatchEntry>().Concat(e.AddedItems.OfType<WatchResult>().Select(r => r.Entry)))
            {
                entry.IsSelected = true;
            }
            foreach (WatchEntry entry in e.RemovedItems.OfType<WatchEntry>().Concat(e.RemovedItems.OfType<WatchResult>().Select(r => r.Entry)))
            {
                entry.IsSelected = false;
            }
        }
        finally
        {
            _syncingSelection = false;
        }
    }

    private void SyncListsFromVm()
    {
        if (_syncingSelection || ViewModel is not MainViewModel vm)
        {
            return;
        }

        _syncingSelection = true;
        try
        {
            SyncList(EntriesList, vm.Watch.Entries);
            SyncList(ResultsList, vm.Watch.Results);
        }
        finally
        {
            _syncingSelection = false;
        }
    }

    private static void SyncList<T>(ListView list, IEnumerable<T> items)
        where T : ISelectable
    {
        foreach (T item in items)
        {
            bool listed = list.SelectedItems.Contains(item);
            if (item.IsSelected && !listed)
            {
                list.SelectedItems.Add(item);
            }
            else if (!item.IsSelected && listed)
            {
                list.SelectedItems.Remove(item);
            }
        }
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
