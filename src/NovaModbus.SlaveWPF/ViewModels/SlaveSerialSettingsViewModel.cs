using System.Collections.ObjectModel;
using NovaModbus.Rtu.Commands;
using NovaModbus.Rtu.Transport;

namespace NovaModbus.SlaveWPF.ViewModels;

/// <summary>Serial port settings for the slave listener.</summary>
public sealed class SlaveSerialSettingsViewModel : ViewModelBase
{
    private static readonly string[] DefaultBaudRates =
        ["1200", "2400", "4800", "9600", "19200", "38400", "57600", "115200", "230400", "460800", "921600"];

    private string? _selectedPort;
    private string _baudRateText = "9600";
    private int _dataBits = 8;
    private string _parity = "Even";
    private string _stopBits = "One";
    private string _statusMessage = string.Empty;

    /// <summary>Initializes settings and loads the system port list.</summary>
    public SlaveSerialSettingsViewModel()
    {
        RefreshPortsCommand = new RelayCommand(RefreshPorts);
        RefreshPorts();
    }

    /// <summary>Available system serial ports.</summary>
    public ObservableCollection<string> AvailablePorts { get; } = [];

    /// <summary>Common baud rates (editable for custom values).</summary>
    public ObservableCollection<string> AvailableBaudRates { get; } = new(DefaultBaudRates);

    /// <summary>Available data-bit options (RTU requires 8).</summary>
    public ObservableCollection<int> AvailableDataBits { get; } = [8];

    /// <summary>Available parity options.</summary>
    public ObservableCollection<string> AvailableParity { get; } = ["None", "Odd", "Even", "Mark", "Space"];

    /// <summary>Available stop-bit options (RTU 11-bit characters).</summary>
    public ObservableCollection<string> AvailableStopBits { get; } = ["One", "Two"];

    /// <summary>Selected COM port.</summary>
    public string? SelectedPort
    {
        get => _selectedPort;
        set => SetProperty(ref _selectedPort, value);
    }

    /// <summary>Baud rate text.</summary>
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

    /// <summary>Parsed baud rate, or 0 when invalid.</summary>
    public int BaudRate => int.TryParse(BaudRateText, out int v) && v > 0 ? v : 0;

    /// <summary>Whether the baud rate text is valid.</summary>
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

    /// <summary>Status line (empty when fine).</summary>
    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    /// <summary>Reload the system port list.</summary>
    public RelayCommand RefreshPortsCommand { get; }

    /// <summary>Reload the system port list, preserving selection when possible.</summary>
    public void RefreshPorts()
    {
        string? keep = SelectedPort;
        string[] names = SerialPortDiscovery.GetPortNames();
        AvailablePorts.Clear();
        foreach (string name in names)
        {
            AvailablePorts.Add(name);
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
}
