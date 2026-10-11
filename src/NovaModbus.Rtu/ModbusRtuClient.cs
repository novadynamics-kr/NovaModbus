using NovaModbus.Rtu.Codecs;
using NovaModbus.Rtu.Enums;
using NovaModbus.Rtu.Logging;
using NovaModbus.Rtu.Transport;

namespace NovaModbus.Rtu;

/// <summary>Modbus RTU master for FC 01,02,03,04,05,06,15,16.</summary>
public sealed class ModbusRtuClient : IAsyncDisposable
{
    private readonly ISerialTransport _transport;
    private readonly IModbusLogger _logger;

    /// <summary>Initializes a client over the given transport.</summary>
    public ModbusRtuClient(ISerialTransport transport, int baudRate = 9600, IModbusLogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(transport);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(baudRate, 0);
        _transport = transport;
        BaudRate = baudRate;
        _logger = logger ?? NullModbusLogger.Instance;
    }

    /// <summary>Gets the line baud rate (frame timing).</summary>
    public int BaudRate { get; }

    /// <summary>Gets or sets the per-attempt response timeout.</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>Gets or sets the resend count after a timed-out attempt.</summary>
    public int Retries { get; set; } = 1;

    /// <summary>Read coils (FC01).</summary>
    public async Task<bool[]> ReadCoilsAsync(byte slaveId, ushort start, ushort count, CancellationToken cancellationToken = default)
    {
        byte[] body = await ExchangeAsync(
            slaveId, ModbusFunction.ReadCoils, ReadBitsCodec.EncodeRequest(start, count), cancellationToken).ConfigureAwait(false);
        return ReadBitsCodec.DecodeResponse(body, count);
    }

    /// <summary>Read discrete inputs (FC02).</summary>
    public async Task<bool[]> ReadDiscreteInputsAsync(byte slaveId, ushort start, ushort count, CancellationToken cancellationToken = default)
    {
        byte[] body = await ExchangeAsync(
            slaveId, ModbusFunction.ReadDiscreteInputs, ReadBitsCodec.EncodeRequest(start, count), cancellationToken).ConfigureAwait(false);
        return ReadBitsCodec.DecodeResponse(body, count);
    }

    /// <summary>Read holding registers (FC03).</summary>
    public async Task<ushort[]> ReadHoldingRegistersAsync(byte slaveId, ushort start, ushort count, CancellationToken cancellationToken = default)
    {
        byte[] body = await ExchangeAsync(
            slaveId, ModbusFunction.ReadHoldingRegisters, ReadRegistersCodec.EncodeRequest(start, count), cancellationToken).ConfigureAwait(false);
        return ReadRegistersCodec.DecodeResponse(body);
    }

    /// <summary>Read input registers (FC04).</summary>
    public async Task<ushort[]> ReadInputRegistersAsync(byte slaveId, ushort start, ushort count, CancellationToken cancellationToken = default)
    {
        byte[] body = await ExchangeAsync(
            slaveId, ModbusFunction.ReadInputRegisters, ReadRegistersCodec.EncodeRequest(start, count), cancellationToken).ConfigureAwait(false);
        return ReadRegistersCodec.DecodeResponse(body);
    }

    /// <summary>Write a single coil (FC05). Verifies the echo.</summary>
    public async Task WriteSingleCoilAsync(byte slaveId, ushort address, bool on, CancellationToken cancellationToken = default)
    {
        byte[] request = WriteSingleCoilCodec.Encode(address, on);
        byte[] echo = await ExchangeAsync(slaveId, ModbusFunction.WriteSingleCoil, request, cancellationToken).ConfigureAwait(false);
        WriteSingleCoilCodec.Decode(echo, out ushort echoAddress, out bool echoOn);
        if (echoAddress != address || echoOn != on)
        {
            throw new InvalidOperationException("FC05 echo mismatch.");
        }
    }

    /// <summary>Write a single register (FC06). Verifies the echo.</summary>
    public async Task WriteSingleRegisterAsync(byte slaveId, ushort address, ushort value, CancellationToken cancellationToken = default)
    {
        byte[] request = WriteSingleRegisterCodec.Encode(address, value);
        byte[] echo = await ExchangeAsync(slaveId, ModbusFunction.WriteSingleRegister, request, cancellationToken).ConfigureAwait(false);
        WriteSingleRegisterCodec.Decode(echo, out ushort echoAddress, out ushort echoValue);
        if (echoAddress != address || echoValue != value)
        {
            throw new InvalidOperationException("FC06 echo mismatch.");
        }
    }

    /// <summary>Write multiple coils (FC15). Verifies start/count echo.</summary>
    public async Task WriteMultipleCoilsAsync(byte slaveId, ushort start, bool[] values, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(values);
        byte[] request = WriteMultipleCoilsCodec.EncodeRequest(start, values);
        byte[] echo = await ExchangeAsync(slaveId, ModbusFunction.WriteMultipleCoils, request, cancellationToken).ConfigureAwait(false);
        WriteMultipleCoilsCodec.DecodeResponse(echo, out ushort echoStart, out ushort echoCount);
        if (echoStart != start || echoCount != values.Length)
        {
            throw new InvalidOperationException("FC15 echo mismatch.");
        }
    }

    /// <summary>Write multiple registers (FC16). Verifies start/count echo.</summary>
    public async Task WriteMultipleRegistersAsync(byte slaveId, ushort start, ushort[] values, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(values);
        byte[] request = WriteMultipleRegistersCodec.EncodeRequest(start, values);
        byte[] echo = await ExchangeAsync(slaveId, ModbusFunction.WriteMultipleRegisters, request, cancellationToken).ConfigureAwait(false);
        WriteMultipleRegistersCodec.DecodeResponse(echo, out ushort echoStart, out ushort echoCount);
        if (echoStart != start || echoCount != values.Length)
        {
            throw new InvalidOperationException("FC16 echo mismatch.");
        }
    }

    private Task<byte[]> ExchangeAsync(byte slaveId, ModbusFunction function, byte[] requestBody, CancellationToken cancellationToken) =>
        ModbusTransaction.ExchangeAsync(
            _transport, BaudRate, slaveId, (byte)function, requestBody,
            Timeout, Retries, logger: _logger, cancellationToken: cancellationToken);

    /// <inheritdoc />
    public ValueTask DisposeAsync() => _transport.DisposeAsync();
}
