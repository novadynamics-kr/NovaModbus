namespace NovaModbus.ClientWPF.ViewModels;

using System.ComponentModel;
using NovaModbus.Rtu.Collections;

/// <summary>Display row for one <see cref="WatchEntry"/>.</summary>
public sealed class WatchResult : ViewModelBase, ISelectable
{
    private string _valueDecText = "-";
    private string _valueHexText = "—";
    private string _timestamp = "-";
    private string _status = "Pending";

    /// <summary>Initializes a pending row.</summary>
    public WatchResult(WatchEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        Entry = entry;
        Entry.PropertyChanged += OnEntryChanged;
    }

    /// <summary>Watched address range.</summary>
    public WatchEntry Entry { get; }

    /// <summary>Selection state (delegated to the entry: both grids stay in sync).</summary>
    public bool IsSelected
    {
        get => Entry.IsSelected;
        set => Entry.IsSelected = value;
    }

    /// <inheritdoc />
    public bool SuppressSelectionNotification
    {
        get => Entry.SuppressSelectionNotification;
        set => Entry.SuppressSelectionNotification = value;
    }

    private void OnEntryChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Forward so the results grid refreshes when the entry is (de)selected elsewhere.
        if (e.PropertyName == nameof(WatchEntry.IsSelected))
        {
            NotifyPropertyChanged(nameof(IsSelected));
        }
    }

    /// <summary>Decimal values.</summary>
    public string ValueDecText
    {
        get => _valueDecText;
        set => SetProperty(ref _valueDecText, value);
    }

    /// <summary>Hex values.</summary>
    public string ValueHexText
    {
        get => _valueHexText;
        set => SetProperty(ref _valueHexText, value);
    }

    /// <summary>Last read time.</summary>
    public string Timestamp
    {
        get => _timestamp;
        set => SetProperty(ref _timestamp, value);
    }

    /// <summary>OK or error message.</summary>
    public string Status
    {
        get => _status;
        set => SetProperty(ref _status, value);
    }
}
