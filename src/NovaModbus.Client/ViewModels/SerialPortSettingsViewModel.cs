using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NovaModbus.Rtu.Transport;

namespace NovaModbus.Client.ViewModels;

/// <summary>Serial port settings panel (left side of the client window).</summary>
public sealed partial class SerialPortSettingsViewModel : ObservableObject
{
    /// <summary>Prefix marking virtual endpoints in the port list.</summary>
    public const string VirtualPrefix = "VIRTUAL:";

    private static readonly string[] DefaultBaudRates =
        ["1200", "2400", "4800", "9600", "19200", "38400", "57600", "115200", "230400", "460800", "921600"];

    private readonly VirtualSerialBus? _bus;

    [ObservableProperty]
    public partial string? SelectedPort { get; set; }

    [ObservableProperty]
    public partial string BaudRateText { get; set; } = "9600";

    [ObservableProperty]
    public partial int DataBits { get; set; } = 8;

    [ObservableProperty]
    public partial string Parity { get; set; } = "Even";

    [ObservableProperty]
    public partial string StopBits { get; set; } = "One";

    [ObservableProperty]
    public partial string StatusMessage { get; set; } = string.Empty;

    /// <summary>Initializes settings and loads the port list (system + virtual).</summary>
    public SerialPortSettingsViewModel(VirtualSerialBus? bus = null)
    {
        _bus = bus;
        RefreshPortsCommand = new RelayCommand(RefreshPorts);
        RefreshPorts();
    }

    /// <summary>Available system serial ports.</summary>
    public ObservableCollection<string> AvailablePorts { get; } = [];

    /// <summary>Common baud rates (ComboBox is editable for custom values).</summary>
    public ObservableCollection<string> AvailableBaudRates { get; } = new(DefaultBaudRates);

    /// <summary>Available data-bit options (RTU requires 8).</summary>
    public ObservableCollection<int> AvailableDataBits { get; } = [8];

    /// <summary>Available parity options (names match System.IO.Ports.Parity).</summary>
    public ObservableCollection<string> AvailableParity { get; } = ["None", "Odd", "Even", "Mark", "Space"];

    /// <summary>Available stop-bit options (RTU 11-bit characters).</summary>
    public ObservableCollection<string> AvailableStopBits { get; } = ["One", "Two"];

    /// <summary>Whether the selection is a virtual endpoint.</summary>
    public bool IsVirtualSelected => SelectedPort?.StartsWith(VirtualPrefix, StringComparison.Ordinal) == true;

    /// <summary>Virtual endpoint name, or null for real ports.</summary>
    public string? VirtualEndpointName =>
        IsVirtualSelected ? SelectedPort![VirtualPrefix.Length..] : null;

    /// <summary>Parsed baud rate, or 0 when the text is invalid.</summary>
    public int BaudRate => int.TryParse(BaudRateText, out int v) && v > 0 ? v : 0;

    /// <summary>Whether the baud rate text is a positive integer.</summary>
    public bool IsBaudRateValid => BaudRate > 0;

    /// <summary>Reload the system port list.</summary>
    public IRelayCommand RefreshPortsCommand { get; }

    /// <summary>Reload the port list (system ports + virtual endpoints).</summary>
    public void RefreshPorts()
    {
        string? keep = SelectedPort;
        string[] names = SerialPortDiscovery.GetPortNames();
        AvailablePorts.Clear();
        foreach (string name in names)
        {
            AvailablePorts.Add(name);
        }

        if (_bus is not null)
        {
            foreach (string endpoint in _bus.ListEndpoints())
            {
                AvailablePorts.Add(VirtualPrefix + endpoint);
            }
        }

        SelectedPort = keep is not null && AvailablePorts.Contains(keep) ? keep
            : AvailablePorts.Count > 0 ? AvailablePorts[0]
            : null;
        StatusMessage = AvailablePorts.Count == 0 ? "No serial ports found." : string.Empty;
    }

    /// <summary>Build transport settings from the current UI state.</summary>
    public SerialSettings BuildSettings()
    {
        if (string.IsNullOrWhiteSpace(SelectedPort))
        {
            throw new InvalidOperationException("No serial port selected.");
        }

        if (!IsBaudRateValid)
        {
            throw new InvalidOperationException($"Invalid baud rate: '{BaudRateText}'.");
        }

        SerialSettings settings = new(SelectedPort, BaudRate, DataBits, Parity, StopBits);
        settings.Validate();
        return settings;
    }

    partial void OnSelectedPortChanged(string? value)
    {
        OnPropertyChanged(nameof(IsVirtualSelected));
        OnPropertyChanged(nameof(VirtualEndpointName));
    }

    partial void OnBaudRateTextChanged(string value)
    {
        OnPropertyChanged(nameof(BaudRate));
        OnPropertyChanged(nameof(IsBaudRateValid));
    }
}
