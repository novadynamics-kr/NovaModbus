using System.Collections.ObjectModel;
using NovaModbus.Rtu.Commands;
using NovaModbus.Rtu.Transport;

namespace NovaModbus.Client.ViewModels;

/// <summary>Serial port settings panel (left side of the client window).</summary>
public sealed class SerialPortSettingsViewModel : ViewModelBase
{
    /// <summary>Prefix marking virtual endpoints in the port list.</summary>
    public const string VirtualPrefix = "VIRTUAL:";

    private static readonly string[] DefaultBaudRates =
        ["1200", "2400", "4800", "9600", "19200", "38400", "57600", "115200", "230400", "460800", "921600"];

    private readonly VirtualSerialBus? _bus;

    private string? _selectedPort;
    private string _baudRateText = "9600";
    private int _dataBits = 8;
    private string _parity = "None";
    private string _stopBits = "One";
    private string _statusMessage = string.Empty;

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

    /// <summary>Available data-bit options.</summary>
    public ObservableCollection<int> AvailableDataBits { get; } = [5, 6, 7, 8];

    /// <summary>Available parity options (names match System.IO.Ports.Parity).</summary>
    public ObservableCollection<string> AvailableParity { get; } = ["None", "Odd", "Even", "Mark", "Space"];

    /// <summary>Available stop-bit options (names match System.IO.Ports.StopBits).</summary>
    public ObservableCollection<string> AvailableStopBits { get; } = ["One", "OnePointFive", "Two"];

    /// <summary>Selected port (real COM name or VIRTUAL: prefix + endpoint).</summary>
    public string? SelectedPort
    {
        get => _selectedPort;
        set
        {
            if (SetProperty(ref _selectedPort, value))
            {
                NotifyPropertyChanged(nameof(IsVirtualSelected));
                NotifyPropertyChanged(nameof(VirtualEndpointName));
            }
        }
    }

    /// <summary>Whether the selection is a virtual endpoint.</summary>
    public bool IsVirtualSelected => SelectedPort?.StartsWith(VirtualPrefix, StringComparison.Ordinal) == true;

    /// <summary>Virtual endpoint name, or null for real ports.</summary>
    public string? VirtualEndpointName =>
        IsVirtualSelected ? SelectedPort![VirtualPrefix.Length..] : null;

    /// <summary>Baud rate text (preset selection or custom input).</summary>
    public string BaudRateText
    {
        get => _baudRateText;
        set
        {
            if (SetProperty(ref _baudRateText, value))
            {
                NotifyPropertyChanged(nameof(BaudRate));
                NotifyPropertyChanged(nameof(IsBaudRateValid));
            }
        }
    }

    /// <summary>Parsed baud rate, or 0 when the text is invalid.</summary>
    public int BaudRate => int.TryParse(BaudRateText, out int v) && v > 0 ? v : 0;

    /// <summary>Whether the baud rate text is a positive integer.</summary>
    public bool IsBaudRateValid => BaudRate > 0;

    /// <summary>Selected data bits.</summary>
    public int DataBits
    {
        get => _dataBits;
        set => SetProperty(ref _dataBits, value);
    }

    /// <summary>Selected parity name.</summary>
    public string Parity
    {
        get => _parity;
        set => SetProperty(ref _parity, value);
    }

    /// <summary>Selected stop-bits name.</summary>
    public string StopBits
    {
        get => _stopBits;
        set => SetProperty(ref _stopBits, value);
    }

    /// <summary>Status line (e.g. "no ports found"). Empty when fine.</summary>
    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    /// <summary>Reload the system port list.</summary>
    public RelayCommand RefreshPortsCommand { get; }

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

        return new SerialSettings(SelectedPort, BaudRate, DataBits, Parity, StopBits);
    }
}
