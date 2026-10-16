namespace NovaModbus.Client.ViewModels;

using System.ComponentModel;
using NovaModbus.Rtu.Collections;

/// <summary>Display row for one <see cref="WatchEntry"/>.</summary>
public sealed class WatchResult : INotifyPropertyChanged, ISelectable
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

    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnEntryChanged(object? sender, PropertyChangedEventArgs e)
    {
        // Forward so the results grid refreshes when the entry is (de)selected elsewhere.
        if (e.PropertyName == nameof(WatchEntry.IsSelected))
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
        }
    }

    private bool SetText(ref string field, string value, string propertyName)
    {
        if (field == value)
        {
            return false;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }

    /// <summary>Decimal values.</summary>
    public string ValueDecText
    {
        get => _valueDecText;
        set => SetText(ref _valueDecText, value, nameof(ValueDecText));
    }

    /// <summary>Hex values.</summary>
    public string ValueHexText
    {
        get => _valueHexText;
        set => SetText(ref _valueHexText, value, nameof(ValueHexText));
    }

    /// <summary>Last read time.</summary>
    public string Timestamp
    {
        get => _timestamp;
        set => SetText(ref _timestamp, value, nameof(Timestamp));
    }

    /// <summary>OK or error message.</summary>
    public string Status
    {
        get => _status;
        set => SetText(ref _status, value, nameof(Status));
    }
}
