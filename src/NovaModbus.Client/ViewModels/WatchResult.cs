namespace NovaModbus.Client.ViewModels;

/// <summary>Display row for one <see cref="WatchEntry"/>.</summary>
public sealed class WatchResult : ViewModelBase
{
    private string _valuesText = "-";
    private string _timestamp = "-";
    private string _status = "Pending";

    /// <summary>Initializes a pending row.</summary>
    public WatchResult(WatchEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        Entry = entry;
    }

    /// <summary>Watched address range.</summary>
    public WatchEntry Entry { get; }

    /// <summary>Formatted values.</summary>
    public string ValuesText
    {
        get => _valuesText;
        set => SetProperty(ref _valuesText, value);
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
