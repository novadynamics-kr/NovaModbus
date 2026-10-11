using NovaModbus.Rtu.Codecs;
using NovaModbus.Rtu.Enums;
using NovaModbus.Rtu.Errors;
using NovaModbus.Rtu.Logging;
using NovaModbus.Rtu.Transport;

namespace NovaModbus.Rtu;

/// <summary>Modbus RTU slave listener with multi-ID dispatch.</summary>
public sealed class ModbusRtuSlave : IAsyncDisposable
{
    private readonly ISerialTransport _transport;
    private readonly IModbusLogger _logger;

    /// <summary>Initializes a slave over the given transport.</summary>
    public ModbusRtuSlave(ISerialTransport transport, IModbusLogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(transport);
        _transport = transport;
        _logger = logger ?? NullModbusLogger.Instance;
        _logger.LogInfo("Slave created.");
    }

    /// <summary>Gets the multi-ID engine.</summary>
    public ModbusSlaveEngine Engine { get; } = new();

    /// <summary>
    /// When true (default), corrupt frames get no response.
    /// When false, corrupt frames raise <see cref="CrcMismatchException"/> (diagnostics).
    /// </summary>
    public bool IgnoreCrcErrors { get; set; } = true;

    /// <summary>When true, every known-slave request gets a SlaveDeviceBusy exception (error injection).</summary>
    public bool SimulateBusy { get; set; }

    /// <summary>Gets or sets the line baud rate (frame timing).</summary>
    public int BaudRate { get; set; } = 9600;

    /// <summary>Gets or sets the request wait timeout for <see cref="ServeOnceAsync"/>.</summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromMilliseconds(500);

    /// <summary>
    /// Pure request dispatch: returns response wire bytes, or null for no response
    /// (unknown slave ID, broadcast, corrupt frame with <see cref="IgnoreCrcErrors"/>).
    /// </summary>
    public byte[]? HandleRequest(byte[] requestWire)
    {
        ArgumentNullException.ThrowIfNull(requestWire);
        if (!ModbusFrame.TryDecode(requestWire, out ModbusFrame? frame) || frame is null)
        {
            if (IgnoreCrcErrors)
            {
                return null;
            }

            throw new CrcMismatchException("CRC mismatch on received frame.");
        }

        if (!Engine.TryGetStore(frame.SlaveId, out ModbusDataStore? store) || store is null)
        {
            return null;
        }

        Engine.IncrementCount(frame.SlaveId);
        _logger.LogRx(requestWire);
        if (SimulateBusy)
        {
            return ExceptionFrame(frame.SlaveId, frame.Function, ModbusExceptionCode.SlaveDeviceBusy);
        }

        byte[] response = Dispatch(frame.SlaveId, frame.Function, frame.Data, store);
        _logger.LogTx(response);
        return response;
    }

    /// <summary>Serve a single request (read → handle → respond). Returns false on wait timeout.</summary>
    public async Task<bool> ServeOnceAsync(TimeSpan? silentIntervalOverride = null, CancellationToken cancellationToken = default)
    {
        byte[]? raw = await RtuFramer.ReadFrameAsync(
            _transport, BaudRate, Timeout, silentIntervalOverride, cancellationToken).ConfigureAwait(false);
        if (raw is null)
        {
            return false;
        }

        byte[]? response = HandleRequest(raw);
        if (response is not null)
        {
            await _transport.WriteAsync(response, cancellationToken).ConfigureAwait(false);
        }

        return true;
    }

    private static byte[] Dispatch(byte slaveId, byte functionByte, byte[] data, ModbusDataStore store)
    {
        ModbusFunction function = (ModbusFunction)functionByte;
        return function switch
        {
            ModbusFunction.ReadCoils => HandleReadBits(slaveId, functionByte, data, store.CoilCount, store.ReadCoils),
            ModbusFunction.ReadDiscreteInputs => HandleReadBits(slaveId, functionByte, data, store.DiscreteCount, store.ReadDiscrete),
            ModbusFunction.ReadHoldingRegisters => HandleReadRegisters(slaveId, functionByte, data, store.HoldingCount, store.ReadHolding),
            ModbusFunction.ReadInputRegisters => HandleReadRegisters(slaveId, functionByte, data, store.InputCount, store.ReadInput),
            ModbusFunction.WriteSingleCoil => HandleWriteSingleCoil(slaveId, functionByte, data, store),
            ModbusFunction.WriteSingleRegister => HandleWriteSingleRegister(slaveId, functionByte, data, store),
            ModbusFunction.WriteMultipleCoils => HandleWriteMultipleCoils(slaveId, functionByte, data, store),
            ModbusFunction.WriteMultipleRegisters => HandleWriteMultipleRegisters(slaveId, functionByte, data, store),
            _ => ExceptionFrame(slaveId, functionByte, ModbusExceptionCode.IllegalFunction),
        };
    }

    private static byte[] HandleReadBits(byte slaveId, byte function, byte[] data, int capacity, Func<ushort, ushort, bool[]> read)
    {
        try
        {
            ReadBitsCodec.DecodeRequest(data, out ushort start, out ushort count);
            if (!InRange(start, count, capacity))
            {
                return ExceptionFrame(slaveId, function, ModbusExceptionCode.IllegalDataAddress);
            }

            return OkFrame(slaveId, function, ReadBitsCodec.EncodeResponse(read(start, count)));
        }
        catch (ArgumentException)
        {
            return ExceptionFrame(slaveId, function, ModbusExceptionCode.IllegalDataValue);
        }
    }

    private static byte[] HandleReadRegisters(byte slaveId, byte function, byte[] data, int capacity, Func<ushort, ushort, ushort[]> read)
    {
        try
        {
            ReadRegistersCodec.DecodeRequest(data, out ushort start, out ushort count);
            if (!InRange(start, count, capacity))
            {
                return ExceptionFrame(slaveId, function, ModbusExceptionCode.IllegalDataAddress);
            }

            return OkFrame(slaveId, function, ReadRegistersCodec.EncodeResponse(read(start, count)));
        }
        catch (ArgumentException)
        {
            return ExceptionFrame(slaveId, function, ModbusExceptionCode.IllegalDataValue);
        }
    }

    private static byte[] HandleWriteSingleCoil(byte slaveId, byte function, byte[] data, ModbusDataStore store)
    {
        try
        {
            WriteSingleCoilCodec.Decode(data, out ushort address, out bool on);
            if (!InRange(address, 1, store.CoilCount))
            {
                return ExceptionFrame(slaveId, function, ModbusExceptionCode.IllegalDataAddress);
            }

            store.WriteCoil(address, on);
            return OkFrame(slaveId, function, data);
        }
        catch (ArgumentException)
        {
            return ExceptionFrame(slaveId, function, ModbusExceptionCode.IllegalDataValue);
        }
    }

    private static byte[] HandleWriteSingleRegister(byte slaveId, byte function, byte[] data, ModbusDataStore store)
    {
        try
        {
            WriteSingleRegisterCodec.Decode(data, out ushort address, out ushort value);
            if (!InRange(address, 1, store.HoldingCount))
            {
                return ExceptionFrame(slaveId, function, ModbusExceptionCode.IllegalDataAddress);
            }

            store.WriteHolding(address, value);
            return OkFrame(slaveId, function, data);
        }
        catch (ArgumentException)
        {
            return ExceptionFrame(slaveId, function, ModbusExceptionCode.IllegalDataValue);
        }
    }

    private static byte[] HandleWriteMultipleCoils(byte slaveId, byte function, byte[] data, ModbusDataStore store)
    {
        try
        {
            WriteMultipleCoilsCodec.DecodeRequest(data, out ushort start, out bool[] values);
            if (!InRange(start, values.Length, store.CoilCount))
            {
                return ExceptionFrame(slaveId, function, ModbusExceptionCode.IllegalDataAddress);
            }

            store.WriteCoils(start, values);
            return OkFrame(slaveId, function, WriteMultipleCoilsCodec.EncodeResponse(start, (ushort)values.Length));
        }
        catch (ArgumentException)
        {
            return ExceptionFrame(slaveId, function, ModbusExceptionCode.IllegalDataValue);
        }
    }

    private static byte[] HandleWriteMultipleRegisters(byte slaveId, byte function, byte[] data, ModbusDataStore store)
    {
        try
        {
            WriteMultipleRegistersCodec.DecodeRequest(data, out ushort start, out ushort[] values);
            if (!InRange(start, values.Length, store.HoldingCount))
            {
                return ExceptionFrame(slaveId, function, ModbusExceptionCode.IllegalDataAddress);
            }

            store.WriteHolding(start, values);
            return OkFrame(slaveId, function, WriteMultipleRegistersCodec.EncodeResponse(start, (ushort)values.Length));
        }
        catch (ArgumentException)
        {
            return ExceptionFrame(slaveId, function, ModbusExceptionCode.IllegalDataValue);
        }
    }

    private static bool InRange(int start, int count, int capacity) =>
        start >= 0 && count >= 0 && start + count <= capacity;

    private static byte[] OkFrame(byte slaveId, byte function, byte[] body) =>
        new ModbusFrame(slaveId, function, body).Encode();

    private static byte[] ExceptionFrame(byte slaveId, byte function, ModbusExceptionCode code) =>
        new ModbusFrame(slaveId, ExceptionCodec.ExceptionFunction((ModbusFunction)function), ExceptionCodec.Encode(code)).Encode();

    /// <inheritdoc />
    public ValueTask DisposeAsync() => _transport.DisposeAsync();
}
