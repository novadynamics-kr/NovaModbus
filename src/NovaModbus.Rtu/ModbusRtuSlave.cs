using NovaModbus.Rtu.Logging;
using NovaModbus.Rtu.Transport;

namespace NovaModbus.Rtu;

/// <summary>Modbus RTU slave listener. Phase 1 skeleton; request loop lands in phase 4.</summary>
public sealed class ModbusRtuSlave : IAsyncDisposable
{
    private readonly ISerialTransport _transport;
    private readonly IModbusLogger _logger;

    /// <summary>Gets the multi-ID engine.</summary>
    public ModbusSlaveEngine Engine { get; } = new();

    /// <summary>When true, CRC errors are ignored without response (for testing).</summary>
    public bool IgnoreCrcErrors { get; set; }

    /// <summary>Initializes a slave over the given transport.</summary>
    public ModbusRtuSlave(ISerialTransport transport, IModbusLogger? logger = null)
    {
        _transport = transport;
        _logger = logger ?? NullModbusLogger.Instance;
        _logger.LogInfo("Slave created.");
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => _transport.DisposeAsync();
}
