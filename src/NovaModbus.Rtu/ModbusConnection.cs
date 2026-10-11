using NovaModbus.Rtu.Logging;
using NovaModbus.Rtu.Transport;

namespace NovaModbus.Rtu;

/// <summary>Owns one open transport + client pair (serial or virtual).</summary>
public sealed class ModbusConnection : IAsyncDisposable
{
    private readonly IModbusLogger _logger;
    private ISerialTransport? _transport;

    /// <summary>Initializes a connection using the given logger for traffic.</summary>
    public ModbusConnection(IModbusLogger? logger = null)
    {
        _logger = logger ?? NullModbusLogger.Instance;
    }

    /// <summary>Active client, or null when disconnected.</summary>
    public ModbusRtuClient? Client { get; private set; }

    /// <summary>Whether a transport is open.</summary>
    public bool IsConnected => Client is not null;

    /// <summary>Raised after connect/disconnect.</summary>
    public event EventHandler? ConnectionChanged;

    /// <summary>Open a real serial port (disconnects any existing one first).</summary>
    public async Task ConnectSerialAsync(SerialSettings settings, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(settings);
        await DisconnectAsync().ConfigureAwait(false);
        ISerialTransport transport = await TransportFactory.OpenSerialAsync(settings, cancellationToken).ConfigureAwait(false);
        Attach(transport, settings.BaudRate);
    }

    /// <summary>Take a published virtual endpoint (disconnects any existing one first).</summary>
    public async Task ConnectVirtualAsync(string endpoint, VirtualSerialBus bus, int baudRate, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint);
        ArgumentNullException.ThrowIfNull(bus);
        cancellationToken.ThrowIfCancellationRequested();
        await DisconnectAsync().ConfigureAwait(false);
        Attach(bus.Connect(endpoint), baudRate);
    }

    /// <summary>Close the current transport (safe when idle).</summary>
    public async ValueTask DisconnectAsync()
    {
        if (_transport is not null)
        {
            await _transport.DisposeAsync().ConfigureAwait(false);
            _transport = null;
        }

        Client = null;
        ConnectionChanged?.Invoke(this, EventArgs.Empty);
    }

    private void Attach(ISerialTransport transport, int baudRate)
    {
        _transport = transport;
        Client = new ModbusRtuClient(transport, baudRate, _logger);
        ConnectionChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => DisconnectAsync();
}
