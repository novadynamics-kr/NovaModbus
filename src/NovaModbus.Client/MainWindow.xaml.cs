using System.Collections.Specialized;
using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
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
        EntriesList.PointerWheelChanged += OnWatchWheel;
        ResultsList.PointerWheelChanged += OnWatchWheel;
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
        if (e.PropertyName != nameof(WatchEntry.IsSelected)
            || sender is not WatchEntry entry
            || ViewModel is not MainViewModel vm)
        {
            return;
        }

        // O(1) per item: full scans freeze the UI at 65k rows.
        SyncOne(EntriesList, entry);
        foreach (WatchResult result in vm.Watch.Results)
        {
            if (ReferenceEquals(result.Entry, entry))
            {
                SyncOne(ResultsList, result);
                break;
            }
        }
    }

    private void OnWatchBulkChanged(object? sender, EventArgs e)
    {
        // O(n): native batch selection instead of per-item scans (freezes at 65k rows).
        if (ViewModel is not MainViewModel vm)
        {
            return;
        }

        _syncingSelection = true;
        try
        {
            if (vm.Watch.Entries.Count > 0 && vm.Watch.Entries[0].IsSelected)
            {
                EntriesList.SelectAll();
                ResultsList.SelectAll();
            }
            else
            {
                EntriesList.SelectedItems.Clear();
                ResultsList.SelectedItems.Clear();
            }
        }
        finally
        {
            _syncingSelection = false;
        }
    }

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

    private static void SyncOne<T>(ListView list, T item)
        where T : ISelectable
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

    private MainViewModel? ViewModel => Root.DataContext as MainViewModel;

    /// <summary>Route wheel input to the hovered list's own scroller (deterministic).</summary>
    /// <remarks>Connected directly on each list element. Skips if native scrolling
    /// already consumed the event, otherwise scrolls this list instantly
    /// (no animation, so rapid notches never fight stale offsets).</remarks>
    private void OnWatchWheel(object sender, PointerRoutedEventArgs e)
    {
        if (e.Handled
            || sender is not ListView list
            || FindScrollViewer(list) is not ScrollViewer viewer)
        {
            return;
        }

        int delta = e.GetCurrentPoint(list).Properties.MouseWheelDelta;
        viewer.ChangeView(null, viewer.VerticalOffset - delta, null, true);
        e.Handled = true;
    }

    private static ScrollViewer? FindScrollViewer(DependencyObject root)
    {
        int count = VisualTreeHelper.GetChildrenCount(root);
        for (int i = 0; i < count; i++)
        {
            DependencyObject child = VisualTreeHelper.GetChild(root, i);
            if (child is ScrollViewer sv)
            {
                return sv;
            }

            ScrollViewer? inner = FindScrollViewer(child);
            if (inner is not null)
            {
                return inner;
            }
        }

        return null;
    }

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
