using System.Diagnostics;
using NovaModbus.Rtu.Codecs;
using NovaModbus.Rtu.Enums;
using NovaModbus.Rtu.Logging;
using NovaModbus.Rtu.Transport;

namespace NovaModbus.Rtu;

/// <summary>Modbus RTU master: full serial-line function set + broadcast writes.</summary>
public sealed class ModbusRtuClient : IAsyncDisposable
{
    private readonly ISerialTransport _transport;
    private readonly IModbusLogger _logger;
    private long _lastBroadcastTicks;

    /// <summary>Initializes a client over the given transport.</summary>
    /// <param name="transport">Open serial transport (owned; disposed with the client).</param>
    /// <param name="baudRate">Line baud rate used for frame timing.</param>
    /// <param name="logger">Traffic logger; null selects the null logger (silent).</param>
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
    /// <param name="slaveId">Target slave ID (1-247).</param>
    /// <param name="start">Zero-based start address.</param>
    /// <param name="count">Coils to read, 1..2000.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Coil states in address order.</returns>
    public async Task<bool[]> ReadCoilsAsync(byte slaveId, ushort start, ushort count, CancellationToken cancellationToken = default)
    {
        byte[] body = await ExchangeAsync(
            slaveId, ModbusFunction.ReadCoils, ReadBitsCodec.EncodeRequest(start, count), cancellationToken).ConfigureAwait(false);
        return ReadBitsCodec.DecodeResponse(body, count);
    }

    /// <summary>Read discrete inputs (FC02).</summary>
    /// <param name="slaveId">Target slave ID (1-247).</param>
    /// <param name="start">Zero-based start address.</param>
    /// <param name="count">Inputs to read, 1..2000.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Input states in address order.</returns>
    public async Task<bool[]> ReadDiscreteInputsAsync(byte slaveId, ushort start, ushort count, CancellationToken cancellationToken = default)
    {
        byte[] body = await ExchangeAsync(
            slaveId, ModbusFunction.ReadDiscreteInputs, ReadBitsCodec.EncodeRequest(start, count), cancellationToken).ConfigureAwait(false);
        return ReadBitsCodec.DecodeResponse(body, count);
    }

    /// <summary>Read holding registers (FC03).</summary>
    /// <param name="slaveId">Target slave ID (1-247).</param>
    /// <param name="start">Zero-based start address.</param>
    /// <param name="count">Registers to read, 1..125.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Register values in address order.</returns>
    public async Task<ushort[]> ReadHoldingRegistersAsync(byte slaveId, ushort start, ushort count, CancellationToken cancellationToken = default)
    {
        byte[] body = await ExchangeAsync(
            slaveId, ModbusFunction.ReadHoldingRegisters, ReadRegistersCodec.EncodeRequest(start, count), cancellationToken).ConfigureAwait(false);
        return ReadRegistersCodec.DecodeResponse(body);
    }

    /// <summary>Read input registers (FC04).</summary>
    /// <param name="slaveId">Target slave ID (1-247).</param>
    /// <param name="start">Zero-based start address.</param>
    /// <param name="count">Registers to read, 1..125.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Register values in address order.</returns>
    public async Task<ushort[]> ReadInputRegistersAsync(byte slaveId, ushort start, ushort count, CancellationToken cancellationToken = default)
    {
        byte[] body = await ExchangeAsync(
            slaveId, ModbusFunction.ReadInputRegisters, ReadRegistersCodec.EncodeRequest(start, count), cancellationToken).ConfigureAwait(false);
        return ReadRegistersCodec.DecodeResponse(body);
    }

    /// <summary>Write a single coil (FC05). Verifies the echo.</summary>
    /// <param name="slaveId">Target slave ID (1-247).</param>
    /// <param name="address">Zero-based coil address.</param>
    /// <param name="on">State to write (encoded 0xFF00/0x0000).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task completing when the echo is verified.</returns>
    /// <exception cref="InvalidOperationException">Thrown on echo mismatch.</exception>
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
    /// <param name="slaveId">Target slave ID (1-247).</param>
    /// <param name="address">Zero-based register address.</param>
    /// <param name="value">Value to write.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task completing when the echo is verified.</returns>
    /// <exception cref="InvalidOperationException">Thrown on echo mismatch.</exception>
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
    /// <param name="slaveId">Target slave ID (1-247).</param>
    /// <param name="start">Zero-based start address.</param>
    /// <param name="values">States to write, 1..1968 items.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task completing when the echo is verified.</returns>
    /// <exception cref="InvalidOperationException">Thrown on echo mismatch.</exception>
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
    /// <param name="slaveId">Target slave ID (1-247).</param>
    /// <param name="start">Zero-based start address.</param>
    /// <param name="values">Values to write, 1..123 items.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task completing when the echo is verified.</returns>
    /// <exception cref="InvalidOperationException">Thrown on echo mismatch.</exception>
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
    /// <param name="slaveId">Target slave ID (1-247).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Exception status byte (device-specific coils).</returns>
    public async Task<byte> ReadExceptionStatusAsync(byte slaveId, CancellationToken cancellationToken = default)
    {
        byte[] body = await ExchangeAsync(
            slaveId, ModbusFunction.ReadExceptionStatus, ExceptionStatusCodec.EncodeRequest(), cancellationToken).ConfigureAwait(false);
        return ExceptionStatusCodec.DecodeResponse(body);
    }

    /// <summary>Diagnostics (FC08). Verifies the sub-function echo, returns the data part.</summary>
    /// <param name="slaveId">Target slave ID (1-247).</param>
    /// <param name="subfunction">Two-byte sub-function code (see <see cref="DiagnosticsCodec.Subfunctions"/>).</param>
    /// <param name="data">Sub-function data bytes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Response data bytes (sub-function echo stripped).</returns>
    /// <exception cref="InvalidOperationException">Thrown on sub-function echo mismatch.</exception>
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
    /// <param name="slaveId">Target slave ID (1-247).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Status word (0xFFFF = busy) and event count.</returns>
    public async Task<(ushort Status, ushort EventCount)> GetCommEventCounterAsync(byte slaveId, CancellationToken cancellationToken = default)
    {
        byte[] body = await ExchangeAsync(
            slaveId, ModbusFunction.GetCommEventCounter, CommEventCounterCodec.EncodeRequest(), cancellationToken).ConfigureAwait(false);
        CommEventCounterCodec.DecodeResponse(body, out ushort status, out ushort eventCount);
        return (status, eventCount);
    }

    /// <summary>Get comm event log (FC0C).</summary>
    /// <param name="slaveId">Target slave ID (1-247).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Status word, event count, message count, and event bytes (newest first).</returns>
    public async Task<(ushort Status, ushort EventCount, ushort MessageCount, byte[] Events)> GetCommEventLogAsync(byte slaveId, CancellationToken cancellationToken = default)
    {
        byte[] body = await ExchangeAsync(
            slaveId, ModbusFunction.GetCommEventLog, CommEventLogCodec.EncodeRequest(), cancellationToken).ConfigureAwait(false);
        CommEventLogCodec.DecodeResponse(body, out ushort status, out ushort eventCount, out ushort messageCount, out byte[] events);
        return (status, eventCount, messageCount, events);
    }

    /// <summary>Report server ID (FC11), assuming no additional data.</summary>
    /// <param name="slaveId">Target slave ID (1-247).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Server ID bytes and run indicator.</returns>
    public async Task<(byte[] ServerId, bool Running)> ReportServerIdAsync(byte slaveId, CancellationToken cancellationToken = default)
    {
        byte[] body = await ExchangeAsync(
            slaveId, ModbusFunction.ReportServerId, ServerIdCodec.EncodeRequest(), cancellationToken).ConfigureAwait(false);
        ServerIdCodec.DecodeResponse(body, out byte[] serverId, out bool running);
        return (serverId, running);
    }

    /// <summary>Report server ID (FC11) with a known server ID length, splitting additional data.</summary>
    /// <param name="slaveId">Target slave ID (1-247).</param>
    /// <param name="serverIdLength">Expected server ID byte length (remainder is additional data).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Server ID bytes, run indicator, and additional data bytes.</returns>
    public async Task<(byte[] ServerId, bool Running, byte[] Additional)> ReportServerIdAsync(byte slaveId, int serverIdLength, CancellationToken cancellationToken = default)
    {
        byte[] body = await ExchangeAsync(
            slaveId, ModbusFunction.ReportServerId, ServerIdCodec.EncodeRequest(), cancellationToken).ConfigureAwait(false);
        ServerIdCodec.DecodeResponse(body, serverIdLength, out byte[] serverId, out bool running, out byte[] additional);
        return (serverId, running, additional);
    }

    /// <summary>Read file records (FC14).</summary>
    /// <param name="slaveId">Target slave ID (1-247).</param>
    /// <param name="subs">Sub-requests (file/record/length triples).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>One register array per sub-request, in order.</returns>
    public async Task<ushort[][]> ReadFileRecordAsync(byte slaveId, FileSubRequest[] subs, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(subs);
        byte[] body = await ExchangeAsync(
            slaveId, ModbusFunction.ReadFileRecord, FileRecordCodec.EncodeReadRequest(subs), cancellationToken).ConfigureAwait(false);
        return FileRecordCodec.DecodeReadResponse(body);
    }

    /// <summary>Write file records (FC15). Verifies the echoed sub-requests.</summary>
    /// <param name="slaveId">Target slave ID (1-247).</param>
    /// <param name="subs">Sub-requests (file/record/length triples).</param>
    /// <param name="datas">One value array per sub-request; lengths must match <paramref name="subs"/>.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task completing when the echo is verified.</returns>
    /// <exception cref="InvalidOperationException">Thrown on echo mismatch.</exception>
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
    /// <param name="slaveId">Target slave ID (1-247).</param>
    /// <param name="address">Zero-based register address.</param>
    /// <param name="andMask">AND mask applied first.</param>
    /// <param name="orMask">OR mask applied second.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task completing when the echo is verified.</returns>
    /// <exception cref="InvalidOperationException">Thrown on echo mismatch.</exception>
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
    /// <param name="slaveId">Target slave ID (1-247).</param>
    /// <param name="readStart">Zero-based read start address.</param>
    /// <param name="readCount">Registers to read, 1..125.</param>
    /// <param name="writeStart">Zero-based write start address.</param>
    /// <param name="writeValues">Values to write first, 1..121 items.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Read register values in address order.</returns>
    public async Task<ushort[]> ReadWriteMultipleRegistersAsync(byte slaveId, ushort readStart, ushort readCount, ushort writeStart, ushort[] writeValues, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(writeValues);
        byte[] body = await ExchangeAsync(
            slaveId, ModbusFunction.ReadWriteMultipleRegisters,
            ReadWriteMultipleCodec.EncodeRequest(readStart, readCount, writeStart, writeValues), cancellationToken).ConfigureAwait(false);
        return ReadWriteMultipleCodec.DecodeResponse(body);
    }

    /// <summary>Read FIFO queue (FC18).</summary>
    /// <param name="slaveId">Target slave ID (1-247).</param>
    /// <param name="pointerAddress">Pointer address identifying the queue.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Queued values oldest-first (empty when the queue is empty).</returns>
    public async Task<ushort[]> ReadFifoQueueAsync(byte slaveId, ushort pointerAddress, CancellationToken cancellationToken = default)
    {
        byte[] body = await ExchangeAsync(
            slaveId, ModbusFunction.ReadFifoQueue, FifoQueueCodec.EncodeRequest(pointerAddress), cancellationToken).ConfigureAwait(false);
        return FifoQueueCodec.DecodeResponse(body);
    }

    /// <summary>Broadcast single-coil write (FC05). No response is returned.</summary>
    /// <param name="address">Zero-based coil address.</param>
    /// <param name="on">State to write.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task completing after transmission (fire-and-forget).</returns>
    public Task BroadcastWriteSingleCoilAsync(ushort address, bool on, CancellationToken cancellationToken = default) =>
        BroadcastAsync(ModbusFunction.WriteSingleCoil, WriteSingleCoilCodec.Encode(address, on), cancellationToken);

    /// <summary>Broadcast single-register write (FC06). No response is returned.</summary>
    /// <param name="address">Zero-based register address.</param>
    /// <param name="value">Value to write.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task completing after transmission (fire-and-forget).</returns>
    public Task BroadcastWriteSingleRegisterAsync(ushort address, ushort value, CancellationToken cancellationToken = default) =>
        BroadcastAsync(ModbusFunction.WriteSingleRegister, WriteSingleRegisterCodec.Encode(address, value), cancellationToken);

    /// <summary>Broadcast multiple-coil write (FC0F). No response is returned.</summary>
    /// <param name="start">Zero-based start address.</param>
    /// <param name="values">States to write, 1..1968 items.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task completing after transmission (fire-and-forget).</returns>
    public Task BroadcastWriteMultipleCoilsAsync(ushort start, bool[] values, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(values);
        return BroadcastAsync(ModbusFunction.WriteMultipleCoils, WriteMultipleCoilsCodec.EncodeRequest(start, values), cancellationToken);
    }

    /// <summary>Broadcast multiple-register write (FC10). No response is returned.</summary>
    /// <param name="start">Zero-based start address.</param>
    /// <param name="values">Values to write, 1..123 items.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task completing after transmission (fire-and-forget).</returns>
    public Task BroadcastWriteMultipleRegistersAsync(ushort start, ushort[] values, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(values);
        return BroadcastAsync(ModbusFunction.WriteMultipleRegisters, WriteMultipleRegistersCodec.EncodeRequest(start, values), cancellationToken);
    }

    /// <summary>Broadcast file-record write (FC15). No response is returned.</summary>
    /// <param name="subs">Sub-requests (file/record/length triples).</param>
    /// <param name="datas">One value array per sub-request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task completing after transmission (fire-and-forget).</returns>
    public Task BroadcastWriteFileRecordAsync(FileSubRequest[] subs, ushort[][] datas, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(subs);
        ArgumentNullException.ThrowIfNull(datas);
        return BroadcastAsync(ModbusFunction.WriteFileRecord, FileRecordCodec.EncodeWriteRequest(subs, datas), cancellationToken);
    }

    /// <summary>Broadcast mask-write register (FC16). No response is returned.</summary>
    /// <param name="address">Zero-based register address.</param>
    /// <param name="andMask">AND mask applied first.</param>
    /// <param name="orMask">OR mask applied second.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task completing after transmission (fire-and-forget).</returns>
    public Task BroadcastMaskWriteRegisterAsync(ushort address, ushort andMask, ushort orMask, CancellationToken cancellationToken = default) =>
        BroadcastAsync(ModbusFunction.MaskWriteRegister, MaskWriteCodec.Encode(address, andMask, orMask), cancellationToken);

    /// <summary>
    /// Broadcast a writing function to address 0. All slaves act, none respond:
    /// returns after transmitting (fire-and-forget). Enforces the t3.5 line quiet
    /// before this and the next transmission.
    /// </summary>
    /// <param name="function">Writing function (05/06/0F/10/15/16/17); FC17 needs an explicit read part.</param>
    /// <param name="requestBody">Function PDU body (CRC excluded).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task completing after transmission.</returns>
    /// <exception cref="ArgumentException">Thrown for non-writing functions or oversize frames.</exception>
    public async Task BroadcastAsync(ModbusFunction function, byte[] requestBody, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requestBody);
        bool writable = function is ModbusFunction.WriteSingleCoil
            or ModbusFunction.WriteSingleRegister
            or ModbusFunction.WriteMultipleCoils
            or ModbusFunction.WriteMultipleRegisters
            or ModbusFunction.WriteFileRecord
            or ModbusFunction.MaskWriteRegister
            or ModbusFunction.ReadWriteMultipleRegisters;
        if (!writable)
        {
            throw new ArgumentException(
                "Broadcast supports writing functions only (05/06/0F/10/15/16/17).", nameof(function));
        }

        byte[] request = new ModbusFrame(ModbusLimits.BroadcastAddress, (byte)function, requestBody).Encode();
        if (request.Length > RtuFramer.MaxFrameSize)
        {
            throw new ArgumentException("Broadcast frame exceeds 256 bytes.", nameof(requestBody));
        }

        await ApplyLineQuietAsync(cancellationToken).ConfigureAwait(false);
        await _transport.WriteAsync(request, cancellationToken).ConfigureAwait(false);
        _logger.LogTx(request);
        Interlocked.Exchange(ref _lastBroadcastTicks, Stopwatch.GetTimestamp());
    }

    /// <summary>Exchange one request/response pair with line-quiet enforcement.</summary>
    /// <param name="slaveId">Target slave ID (1-247).</param>
    /// <param name="function">Function code.</param>
    /// <param name="requestBody">Function PDU body.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Response PDU body.</returns>
    private async Task<byte[]> ExchangeAsync(byte slaveId, ModbusFunction function, byte[] requestBody, CancellationToken cancellationToken)
    {
        await ApplyLineQuietAsync(cancellationToken).ConfigureAwait(false);
        return await ModbusTransaction.ExchangeAsync(
            _transport, BaudRate, slaveId, (byte)function, requestBody,
            Timeout, Retries, logger: _logger, cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Wait out the t3.5 line quiet after a broadcast send (no-op otherwise).</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task completing when the line is quiet.</returns>
    private async Task ApplyLineQuietAsync(CancellationToken cancellationToken)
    {
        long last = Interlocked.Read(ref _lastBroadcastTicks);
        if (last == 0)
        {
            return;
        }

        TimeSpan quiet = RtuFramer.SilentInterval(BaudRate);
        TimeSpan elapsed = Stopwatch.GetElapsedTime(last, Stopwatch.GetTimestamp());
        if (elapsed < quiet)
        {
            await Task.Delay(quiet - elapsed, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync() => _transport.DisposeAsync();
}
