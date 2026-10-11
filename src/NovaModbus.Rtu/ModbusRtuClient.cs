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

    /// <summary>Read exception status (FC07).</summary>
    public async Task<byte> ReadExceptionStatusAsync(byte slaveId, CancellationToken cancellationToken = default)
    {
        byte[] body = await ExchangeAsync(
            slaveId, ModbusFunction.ReadExceptionStatus, ExceptionStatusCodec.EncodeRequest(), cancellationToken).ConfigureAwait(false);
        return ExceptionStatusCodec.DecodeResponse(body);
    }

    /// <summary>Diagnostics (FC08). Verifies the sub-function echo, returns the data part.</summary>
    public async Task<byte[]> DiagnosticsAsync(byte slaveId, ushort subfunction, byte[] data, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(data);
        byte[] body = await ExchangeAsync(
            slaveId, ModbusFunction.Diagnostics, DiagnosticsCodec.Encode(subfunction, data), cancellationToken).ConfigureAwait(false);
        DiagnosticsCodec.Decode(body, out ushort echoSub, out byte[] echoData);
        if (echoSub != subfunction)
        {
            throw new InvalidOperationException("FC08 sub-function echo mismatch.");
        }

        return echoData;
    }

    /// <summary>Get comm event counter (FC0B).</summary>
    public async Task<(ushort Status, ushort EventCount)> GetCommEventCounterAsync(byte slaveId, CancellationToken cancellationToken = default)
    {
        byte[] body = await ExchangeAsync(
            slaveId, ModbusFunction.GetCommEventCounter, CommEventCounterCodec.EncodeRequest(), cancellationToken).ConfigureAwait(false);
        CommEventCounterCodec.DecodeResponse(body, out ushort status, out ushort eventCount);
        return (status, eventCount);
    }

    /// <summary>Get comm event log (FC0C).</summary>
    public async Task<(ushort Status, ushort EventCount, ushort MessageCount, byte[] Events)> GetCommEventLogAsync(byte slaveId, CancellationToken cancellationToken = default)
    {
        byte[] body = await ExchangeAsync(
            slaveId, ModbusFunction.GetCommEventLog, CommEventLogCodec.EncodeRequest(), cancellationToken).ConfigureAwait(false);
        CommEventLogCodec.DecodeResponse(body, out ushort status, out ushort eventCount, out ushort messageCount, out byte[] events);
        return (status, eventCount, messageCount, events);
    }

    /// <summary>Report server ID (FC11), assuming no additional data.</summary>
    public async Task<(byte[] ServerId, bool Running)> ReportServerIdAsync(byte slaveId, CancellationToken cancellationToken = default)
    {
        byte[] body = await ExchangeAsync(
            slaveId, ModbusFunction.ReportServerId, ServerIdCodec.EncodeRequest(), cancellationToken).ConfigureAwait(false);
        ServerIdCodec.DecodeResponse(body, out byte[] serverId, out bool running);
        return (serverId, running);
    }

    /// <summary>Report server ID (FC11) with a known server ID length, splitting additional data.</summary>
    public async Task<(byte[] ServerId, bool Running, byte[] Additional)> ReportServerIdAsync(byte slaveId, int serverIdLength, CancellationToken cancellationToken = default)
    {
        byte[] body = await ExchangeAsync(
            slaveId, ModbusFunction.ReportServerId, ServerIdCodec.EncodeRequest(), cancellationToken).ConfigureAwait(false);
        ServerIdCodec.DecodeResponse(body, serverIdLength, out byte[] serverId, out bool running, out byte[] additional);
        return (serverId, running, additional);
    }

    /// <summary>Read file records (FC14).</summary>
    public async Task<ushort[][]> ReadFileRecordAsync(byte slaveId, FileSubRequest[] subs, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(subs);
        byte[] body = await ExchangeAsync(
            slaveId, ModbusFunction.ReadFileRecord, FileRecordCodec.EncodeReadRequest(subs), cancellationToken).ConfigureAwait(false);
        return FileRecordCodec.DecodeReadResponse(body);
    }

    /// <summary>Write file records (FC15). Verifies the echoed sub-requests.</summary>
    public async Task WriteFileRecordAsync(byte slaveId, FileSubRequest[] subs, ushort[][] datas, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(subs);
        ArgumentNullException.ThrowIfNull(datas);
        byte[] body = await ExchangeAsync(
            slaveId, ModbusFunction.WriteFileRecord, FileRecordCodec.EncodeWriteRequest(subs, datas), cancellationToken).ConfigureAwait(false);
        FileSubRequest[] echo = FileRecordCodec.DecodeWriteResponse(body);
        if (!echo.SequenceEqual(subs))
        {
            throw new InvalidOperationException("FC15 echo mismatch.");
        }
    }

    /// <summary>Mask write register (FC16). Verifies the echo.</summary>
    public async Task MaskWriteRegisterAsync(byte slaveId, ushort address, ushort andMask, ushort orMask, CancellationToken cancellationToken = default)
    {
        byte[] request = MaskWriteCodec.Encode(address, andMask, orMask);
        byte[] echo = await ExchangeAsync(slaveId, ModbusFunction.MaskWriteRegister, request, cancellationToken).ConfigureAwait(false);
        MaskWriteCodec.Decode(echo, out ushort echoAddress, out ushort echoAnd, out ushort echoOr);
        if (echoAddress != address || echoAnd != andMask || echoOr != orMask)
        {
            throw new InvalidOperationException("Mask-write echo mismatch.");
        }
    }

    /// <summary>Read/write multiple registers (FC17).</summary>
    public async Task<ushort[]> ReadWriteMultipleRegistersAsync(byte slaveId, ushort readStart, ushort readCount, ushort writeStart, ushort[] writeValues, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(writeValues);
        byte[] body = await ExchangeAsync(
            slaveId, ModbusFunction.ReadWriteMultipleRegisters,
            ReadWriteMultipleCodec.EncodeRequest(readStart, readCount, writeStart, writeValues), cancellationToken).ConfigureAwait(false);
        return ReadWriteMultipleCodec.DecodeResponse(body);
    }

    /// <summary>Read FIFO queue (FC18).</summary>
    public async Task<ushort[]> ReadFifoQueueAsync(byte slaveId, ushort pointerAddress, CancellationToken cancellationToken = default)
    {
        byte[] body = await ExchangeAsync(
            slaveId, ModbusFunction.ReadFifoQueue, FifoQueueCodec.EncodeRequest(pointerAddress), cancellationToken).ConfigureAwait(false);
        return FifoQueueCodec.DecodeResponse(body);
    }

    private Task<byte[]> ExchangeAsync(byte slaveId, ModbusFunction function, byte[] requestBody, CancellationToken cancellationToken) =>
        ModbusTransaction.ExchangeAsync(
            _transport, BaudRate, slaveId, (byte)function, requestBody,
            Timeout, Retries, logger: _logger, cancellationToken: cancellationToken);

    /// <inheritdoc />
    public ValueTask DisposeAsync() => _transport.DisposeAsync();
}
