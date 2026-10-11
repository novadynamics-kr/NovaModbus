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
            Engine.NoteBusCrcError();
            if (IgnoreCrcErrors)
            {
                return null;
            }

            throw new CrcMismatchException("CRC mismatch on received frame.");
        }

        Engine.NoteBusMessage();
        if (!Engine.TryGetStore(frame.SlaveId, out ModbusDataStore? store) || store is null)
        {
            return null;
        }

        Engine.IncrementCount(frame.SlaveId);
        store.NoteSlaveMessage();
        store.NoteEvent(0x00);
        _logger.LogRx(requestWire);
        if (SimulateBusy)
        {
            store.NoteBusy();
            return ExceptionResponse(frame.SlaveId, frame.Function, ModbusExceptionCode.SlaveDeviceBusy, store);
        }

        byte[]? response = Dispatch(frame.SlaveId, frame.Function, frame.Data, store, Engine);
        if (response is null)
        {
            store.NoteNoResponse();
            return null;
        }

        if ((response[1] & 0x80) != 0)
        {
            store.NoteSlaveException();
            store.NoteEvent(0xFF);
        }

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

    private static byte[]? Dispatch(byte slaveId, byte functionByte, byte[] data, ModbusDataStore store, ModbusSlaveEngine engine)
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
            ModbusFunction.ReadExceptionStatus => OkFrame(slaveId, functionByte, [store.ExceptionStatus]),
            ModbusFunction.Diagnostics => HandleDiagnostics(slaveId, functionByte, data, store, engine),
            ModbusFunction.GetCommEventCounter => HandleCommEventCounter(slaveId, functionByte, store),
            ModbusFunction.GetCommEventLog => HandleCommEventLog(slaveId, functionByte, store),
            ModbusFunction.WriteMultipleCoils => HandleWriteMultipleCoils(slaveId, functionByte, data, store),
            ModbusFunction.WriteMultipleRegisters => HandleWriteMultipleRegisters(slaveId, functionByte, data, store),
            ModbusFunction.ReportServerId => HandleReportServerId(slaveId, functionByte, store),
            ModbusFunction.ReadFileRecord => HandleReadFileRecord(slaveId, functionByte, data, store),
            ModbusFunction.WriteFileRecord => HandleWriteFileRecord(slaveId, functionByte, data, store),
            ModbusFunction.MaskWriteRegister => HandleMaskWrite(slaveId, functionByte, data, store),
            ModbusFunction.ReadWriteMultipleRegisters => HandleReadWriteMultiple(slaveId, functionByte, data, store),
            ModbusFunction.ReadFifoQueue => HandleReadFifo(slaveId, functionByte, data, store),
            _ => ExceptionFrame(slaveId, functionByte, ModbusExceptionCode.IllegalFunction),
        };
    }

    private static byte[] ExceptionResponse(byte slaveId, byte function, ModbusExceptionCode code, ModbusDataStore store)
    {
        store.NoteSlaveException();
        store.NoteEvent(0xFF);
        return ExceptionFrame(slaveId, function, code);
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

    private static byte[]? HandleDiagnostics(byte slaveId, byte function, byte[] data, ModbusDataStore store, ModbusSlaveEngine engine)
    {
        ushort subfunction;
        byte[] payload;
        try
        {
            DiagnosticsCodec.Decode(data, out subfunction, out payload);
        }
        catch (ArgumentException)
        {
            return ExceptionFrame(slaveId, function, ModbusExceptionCode.IllegalDataValue);
        }

        return subfunction switch
        {
            DiagnosticsCodec.Subfunctions.ReturnQueryData => OkFrame(slaveId, function, data),
            DiagnosticsCodec.Subfunctions.RestartCommunicationsOption => HandleRestartOption(slaveId, function, data, payload, store),
            DiagnosticsCodec.Subfunctions.ReturnDiagnosticRegister => OkFrame(slaveId, function,
                DiagnosticsCodec.Encode(subfunction, DiagnosticsCodec.CounterData(store.DiagnosticRegister))),
            DiagnosticsCodec.Subfunctions.ForceListenOnlyMode => null,
            DiagnosticsCodec.Subfunctions.ClearCounters => ClearAllCounters(slaveId, function, data, subfunction, store, engine),
            DiagnosticsCodec.Subfunctions.ReturnBusMessageCount => CounterFrame(slaveId, function, subfunction, engine.BusMessageCount),
            DiagnosticsCodec.Subfunctions.ReturnBusCommErrorCount => CounterFrame(slaveId, function, subfunction, engine.BusCrcErrorCount),
            DiagnosticsCodec.Subfunctions.ReturnSlaveExceptionCount => CounterFrame(slaveId, function, subfunction, store.SlaveExceptionCount),
            DiagnosticsCodec.Subfunctions.ReturnSlaveMessageCount => CounterFrame(slaveId, function, subfunction, store.SlaveMessageCount),
            DiagnosticsCodec.Subfunctions.ReturnSlaveNoResponseCount => CounterFrame(slaveId, function, subfunction, store.NoResponseCount),
            DiagnosticsCodec.Subfunctions.ReturnSlaveBusyCount => CounterFrame(slaveId, function, subfunction, store.BusyCount),
            DiagnosticsCodec.Subfunctions.ReturnBusOverrunCount => CounterFrame(slaveId, function, subfunction, 0),
            _ => ExceptionFrame(slaveId, function, ModbusExceptionCode.IllegalFunction),
        };
    }

    private static byte[] HandleRestartOption(byte slaveId, byte function, byte[] data, byte[] payload, ModbusDataStore store)
    {
        if (payload is not ([0x00, 0x00] or [0xFF, 0x00]))
        {
            return ExceptionFrame(slaveId, function, ModbusExceptionCode.IllegalDataValue);
        }

        if (payload[0] == 0xFF)
        {
            store.ClearEventLog();
        }

        return OkFrame(slaveId, function, data);
    }

    private static byte[] ClearAllCounters(byte slaveId, byte function, byte[] data, ushort subfunction, ModbusDataStore store, ModbusSlaveEngine engine)
    {
        store.ResetCounters();
        engine.ResetBusCounters();
        return OkFrame(slaveId, function, DiagnosticsCodec.Encode(subfunction, [0x00, 0x00]));
    }

    private static byte[] CounterFrame(byte slaveId, byte function, ushort subfunction, long value) =>
        OkFrame(slaveId, function, DiagnosticsCodec.Encode(subfunction, DiagnosticsCodec.CounterData(Clamp16(value))));

    private static byte[] HandleCommEventCounter(byte slaveId, byte function, ModbusDataStore store) =>
        OkFrame(slaveId, function, CommEventCounterCodec.EncodeResponse(0x0000, Clamp16(store.EventCount)));

    private static byte[] HandleCommEventLog(byte slaveId, byte function, ModbusDataStore store) =>
        OkFrame(slaveId, function, CommEventLogCodec.EncodeResponse(
            0x0000, Clamp16(store.EventCount), Clamp16(store.SlaveMessageCount), store.ReadEventLog()));

    private static byte[] HandleReportServerId(byte slaveId, byte function, ModbusDataStore store)
    {
        byte[] identity = System.Text.Encoding.ASCII.GetBytes(store.ServerIdentity);
        return OkFrame(slaveId, function, ServerIdCodec.EncodeResponse(identity, true, store.GetAdditionalData()));
    }

    private static byte[] HandleReadFileRecord(byte slaveId, byte function, byte[] data, ModbusDataStore store)
    {
        FileSubRequest[] subs;
        try
        {
            subs = FileRecordCodec.DecodeReadRequest(data);
        }
        catch (ArgumentException)
        {
            return ExceptionFrame(slaveId, function, ModbusExceptionCode.IllegalDataValue);
        }

        List<ushort[]> records = new(subs.Length);
        foreach (FileSubRequest sub in subs)
        {
            try
            {
                records.Add(store.ReadFileRecord(sub.FileNumber, sub.RecordNumber, sub.Length));
            }
            catch (ArgumentOutOfRangeException)
            {
                return ExceptionFrame(slaveId, function, ModbusExceptionCode.IllegalDataAddress);
            }
        }

        return OkFrame(slaveId, function, FileRecordCodec.EncodeReadResponse(records));
    }

    private static byte[] HandleWriteFileRecord(byte slaveId, byte function, byte[] data, ModbusDataStore store)
    {
        FileSubRequest[] subs;
        ushort[][] datas;
        try
        {
            FileRecordCodec.DecodeWriteRequest(data, out subs, out datas);
        }
        catch (ArgumentException)
        {
            return ExceptionFrame(slaveId, function, ModbusExceptionCode.IllegalDataValue);
        }

        for (int i = 0; i < subs.Length; i++)
        {
            try
            {
                store.WriteFileRecord(subs[i].FileNumber, subs[i].RecordNumber, datas[i]);
            }
            catch (ArgumentOutOfRangeException)
            {
                return ExceptionFrame(slaveId, function, ModbusExceptionCode.IllegalDataAddress);
            }
        }

        return OkFrame(slaveId, function, FileRecordCodec.EncodeWriteResponse(subs));
    }

    private static byte[] HandleMaskWrite(byte slaveId, byte function, byte[] data, ModbusDataStore store)
    {
        try
        {
            MaskWriteCodec.Decode(data, out ushort address, out ushort andMask, out ushort orMask);
            if (!InRange(address, 1, store.HoldingCount))
            {
                return ExceptionFrame(slaveId, function, ModbusExceptionCode.IllegalDataAddress);
            }

            ushort current = store.ReadHolding(address, 1)[0];
            store.WriteHolding(address, MaskWriteCodec.Apply(current, andMask, orMask));
            return OkFrame(slaveId, function, data);
        }
        catch (ArgumentException)
        {
            return ExceptionFrame(slaveId, function, ModbusExceptionCode.IllegalDataValue);
        }
    }

    private static byte[] HandleReadWriteMultiple(byte slaveId, byte function, byte[] data, ModbusDataStore store)
    {
        ushort readStart;
        ushort readCount;
        ushort writeStart;
        ushort[] writeValues;
        try
        {
            ReadWriteMultipleCodec.DecodeRequest(data, out readStart, out readCount, out writeStart, out writeValues);
        }
        catch (ArgumentException)
        {
            return ExceptionFrame(slaveId, function, ModbusExceptionCode.IllegalDataValue);
        }

        if (!InRange(writeStart, writeValues.Length, store.HoldingCount) || !InRange(readStart, readCount, store.HoldingCount))
        {
            return ExceptionFrame(slaveId, function, ModbusExceptionCode.IllegalDataAddress);
        }

        store.WriteHolding(writeStart, writeValues);
        return OkFrame(slaveId, function, ReadWriteMultipleCodec.EncodeResponse(store.ReadHolding(readStart, readCount)));
    }

    private static byte[] HandleReadFifo(byte slaveId, byte function, byte[] data, ModbusDataStore store)
    {
        ushort pointer;
        try
        {
            pointer = FifoQueueCodec.DecodeRequest(data);
        }
        catch (ArgumentException)
        {
            return ExceptionFrame(slaveId, function, ModbusExceptionCode.IllegalDataValue);
        }

        if (!store.FifoExists(pointer))
        {
            return ExceptionFrame(slaveId, function, ModbusExceptionCode.IllegalDataAddress);
        }

        ushort[] values = store.PeekFifo(pointer);
        if (values.Length > ModbusLimits.MaxFifoCount)
        {
            return ExceptionFrame(slaveId, function, ModbusExceptionCode.IllegalDataValue);
        }

        return OkFrame(slaveId, function, FifoQueueCodec.EncodeResponse(values));
    }

    private static ushort Clamp16(long value) => (ushort)Math.Min(value, (long)ushort.MaxValue);

    private static bool InRange(int start, int count, int capacity) =>
        start >= 0 && count >= 0 && start + count <= capacity;

    private static byte[] OkFrame(byte slaveId, byte function, byte[] body) =>
        new ModbusFrame(slaveId, function, body).Encode();

    private static byte[] ExceptionFrame(byte slaveId, byte function, ModbusExceptionCode code) =>
        new ModbusFrame(slaveId, ExceptionCodec.ExceptionFunction((ModbusFunction)function), ExceptionCodec.Encode(code)).Encode();

    /// <inheritdoc />
    public ValueTask DisposeAsync() => _transport.DisposeAsync();
}
