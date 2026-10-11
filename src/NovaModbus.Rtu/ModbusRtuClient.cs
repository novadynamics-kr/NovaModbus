using NovaModbus.Rtu.Logging;
using NovaModbus.Rtu.Transport;

namespace NovaModbus.Rtu;

/// <summary>Modbus RTU master. Phase 1 skeleton: FC03 read holding registers.</summary>
public sealed class ModbusRtuClient : IAsyncDisposable
{
    private readonly ISerialTransport _transport;
    private readonly IModbusLogger _logger;

    /// <summary>Gets or sets the response timeout.</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>Initializes a client over the given transport.</summary>
    public ModbusRtuClient(ISerialTransport transport, IModbusLogger? logger = null)
    {
        _transport = transport;
        _logger = logger ?? NullModbusLogger.Instance;
    }

    /// <summary>Read holding registers (FC03). TODO: full transport loop in phase 4.</summary>
    public Task<ushort[]> ReadHoldingRegistersAsync(byte slaveId, ushort start, ushort count, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfGreaterThan(slaveId, 247);
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(count, 125);
        _logger.LogInfo($"FC03 req slave={slaveId} start={start} count={count}");
        return Task.FromResult<ushort[]>([]);
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => _transport.DisposeAsync();
}
