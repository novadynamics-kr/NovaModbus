namespace NovaModbus.Client.ViewModels;

/// <summary>Display row for one <see cref="WatchEntry"/>.</summary>
public sealed class WatchResult : ViewModelBase
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
    }

    /// <summary>Watched address range.</summary>
    public WatchEntry Entry { get; }

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
