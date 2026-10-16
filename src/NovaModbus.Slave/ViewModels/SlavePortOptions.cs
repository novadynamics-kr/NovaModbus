using NovaModbus.Rtu.Transport;

namespace NovaModbus.Slave.ViewModels;

/// <summary>Serial port selections for the slave listener (plain state; the form owns the controls).</summary>
public sealed class SlavePortOptions
{
    private static readonly string[] DefaultBaudRates =
        ["1200", "2400", "4800", "9600", "19200", "38400", "57600", "115200", "230400", "460800", "921600"];

    /// <summary>Available system serial ports.</summary>
    public List<string> AvailablePorts { get; } = [];

    /// <summary>Common baud rates (combo is editable for custom values).</summary>
    public List<string> AvailableBaudRates { get; } = new(DefaultBaudRates);

    /// <summary>Available data-bit options (RTU requires 8).</summary>
    public List<int> AvailableDataBits { get; } = [8];

    /// <summary>Available parity options (names match System.IO.Ports.Parity).</summary>
    public List<string> AvailableParity { get; } = ["None", "Odd", "Even", "Mark", "Space"];

    /// <summary>Available stop-bit options (RTU 11-bit characters).</summary>
    public List<string> AvailableStopBits { get; } = ["One", "Two"];

    /// <summary>Selected port.</summary>
    public string? SelectedPort { get; set; }

    /// <summary>Baud rate text (preset selection or custom input).</summary>
    public string BaudRateText { get; set; } = "9600";

    /// <summary>Selected data bits.</summary>
    public int DataBits { get; set; } = 8;

    /// <summary>Selected parity name.</summary>
    public string Parity { get; set; } = "Even";

    /// <summary>Selected stop-bits name.</summary>
    public string StopBits { get; set; } = "One";

    /// <summary>Status line (e.g. "no ports found"). Empty when fine.</summary>
    public string StatusMessage { get; private set; } = string.Empty;

    /// <summary>Parsed baud rate, or 0 when the text is invalid.</summary>
    public int BaudRate => int.TryParse(BaudRateText, out int v) && v > 0 ? v : 0;

    /// <summary>Whether the baud rate text is a positive integer.</summary>
    public bool IsBaudRateValid => BaudRate > 0;

    /// <summary>Reload the port list.</summary>
    public void RefreshPorts()
    {
        string? keep = SelectedPort;
        string[] names = SerialPortDiscovery.GetPortNames();
        AvailablePorts.Clear();
        AvailablePorts.AddRange(names);
        SelectedPort = keep is not null && AvailablePorts.Contains(keep) ? keep
            : AvailablePorts.Count > 0 ? AvailablePorts[0]
            : null;
        StatusMessage = AvailablePorts.Count == 0 ? "No serial ports found." : string.Empty;
    }

    /// <summary>Build transport settings from the current state.</summary>
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
