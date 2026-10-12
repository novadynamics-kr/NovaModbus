using System.Buffers.Binary;
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
    /// <param name="transport">Line transport (owned; line errors subscribed).</param>
    /// <param name="logger">Traffic logger; null selects the null logger (silent).</param>
    public ModbusRtuSlave(ISerialTransport transport, IModbusLogger? logger = null)
    {
        ArgumentNullException.ThrowIfNull(transport);
        _transport = transport;
        _transport.LineError += OnLineError;
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
    /// <param name="requestWire">Raw received ADU bytes (address + function + data + CRC).</param>
    /// <returns>Response ADU bytes, or null when nothing must be sent.</returns>
    /// <exception cref="CrcMismatchException">Thrown on corrupt frames when <see cref="IgnoreCrcErrors"/> is false.</exception>
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
        if (frame.SlaveId == ModbusLimits.BroadcastAddress)
        {
            HandleBroadcast(frame.Function, frame.Data);
            return null;
        }

        if (!Engine.TryGetStore(frame.SlaveId, out ModbusDataStore? store) || store is null)
        {
            return null;
        }

        if (store.ListenOnly && !IsRestartRequest(frame.Function, frame.Data))
        {
            // Listen-only: monitor only, no action and no response.
            return null;
        }

        Engine.IncrementCount(frame.SlaveId);
        store.NoteSlaveMessage();
        store.NoteReceiveEvent(ReceiveEventNormal);
        _logger.LogRx(requestWire);
        if (SimulateBusy)
        {
            store.NoteBusy();
            return ExceptionResponse(frame.SlaveId, frame.Function, ModbusExceptionCode.SlaveDeviceBusy, store);
        }

        byte[]? response = Dispatch(frame.SlaveId, frame.Function, frame.Data, store, Engine);
        if (response is not null && (response[1] & 0x80) != 0)
        {
            // Exception responses never increment the event counter.
            store.NoteSlaveException();
            store.NoteSendEvent(SendEventFor((ModbusExceptionCode)response[2]));
            _logger.LogTx(response);
            return response;
        }

        if (response is null)
        {
            store.NoteNoResponse();
        }
        else
        {
            _logger.LogTx(response);
        }

        store.NoteSendEvent(DedicatedSendEvent(frame.Function, frame.Data) ?? SendEventNormal);
        if (IsCountable(frame.Function, frame.Data))
        {
            store.NoteEventCount();
        }

        return response;
    }

    /// <summary>Serve a single request (read → handle → respond). Returns false on wait timeout.</summary>
    /// <param name="silentIntervalOverride">Test hook replacing the derived t3.5.</param>
    /// <param name="cancellationToken">Cancellation token (throws on cancel).</param>
    /// <returns>True when a frame was received (responded or not); false on wait timeout.</returns>
    public async Task<bool> ServeOnceAsync(TimeSpan? silentIntervalOverride = null, CancellationToken cancellationToken = default)
    {
        byte[]? raw = await RtuFramer.ReadFrameAsync(
            _transport, BaudRate, Timeout, silentIntervalOverride, null, cancellationToken).ConfigureAwait(false);
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

    /// <summary>
    /// Execute a broadcast request on every store. Only writing functions are applied;
    /// reads, diagnostics and unknown functions are ignored. Never responds
    /// (not even exceptions or busy), per spec.
    /// </summary>
    /// <param name="functionByte">Raw function code byte.</param>
    /// <param name="data">Function PDU body.</param>
    private void HandleBroadcast(byte functionByte, byte[] data)
    {
        ModbusFunction function = (ModbusFunction)functionByte;
        bool writable = function is ModbusFunction.WriteSingleCoil
            or ModbusFunction.WriteSingleRegister
            or ModbusFunction.WriteMultipleCoils
            or ModbusFunction.WriteMultipleRegisters
            or ModbusFunction.WriteFileRecord
            or ModbusFunction.MaskWriteRegister
            or ModbusFunction.ReadWriteMultipleRegisters;
        if (!writable)
        {
            return;
        }

        foreach (byte id in Engine.SlaveIds)
        {
            if (!Engine.TryGetStore(id, out ModbusDataStore? store) || store is null)
            {
                continue;
            }

            store.NoteSlaveMessage();
            store.NoteReceiveEvent(ReceiveEventBroadcast);
            byte[]? response = Dispatch(id, functionByte, data, store, Engine);
            if (response is null)
            {
                store.NoteNoResponse();
                store.NoteSendEvent(SendEventNormal);
                continue;
            }

            if ((response[1] & 0x80) != 0)
            {
                // Broadcast exceptions are recorded locally but never sent.
                store.NoteSendEvent(SendEventFor((ModbusExceptionCode)response[2]));
                continue;
            }

            store.NoteSendEvent(SendEventNormal);
            store.NoteEventCount();
        }
    }

    /// <summary>Route a decoded request to its function handler.</summary>
    /// <param name="slaveId">Addressed slave ID (echoed in responses).</param>
    /// <param name="functionByte">Raw function code byte.</param>
    /// <param name="data">Function PDU body.</param>
    /// <param name="store">Addressed slave data store.</param>
    /// <param name="engine">Device-level counters engine.</param>
    /// <returns>Response ADU bytes, or null when nothing must be sent (e.g. listen-only entry).</returns>
    private static byte[]? Dispatch(byte slaveId, byte functionByte, byte[] data, ModbusDataStore store, ModbusSlaveEngine engine)    {
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
            ModbusFunction.GetCommEventLog => HandleCommEventLog(slaveId, functionByte, store, engine),
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

    /// <summary>Build an exception response and record its counters/event.</summary>
    /// <param name="slaveId">Slave ID to echo.</param>
    /// <param name="function">Base function code byte.</param>
    /// <param name="code">Exception code to report.</param>
    /// <param name="store">Store receiving the exception/event counters.</param>
    /// <returns>Exception ADU bytes.</returns>
    private static byte[] ExceptionResponse(byte slaveId, byte function, ModbusExceptionCode code, ModbusDataStore store)
    {
        store.NoteSlaveException();
        store.NoteSendEvent(SendEventFor(code));
        return ExceptionFrame(slaveId, function, code);
    }

    /// <summary>Handle FC01/FC02 reads against a bit capacity.</summary>
    /// <param name="slaveId">Slave ID to echo.</param>
    /// <param name="function">Raw function code byte.</param>
    /// <param name="data">Request PDU body ([start][count]).</param>
    /// <param name="capacity">Addressable bit count of the area.</param>
    /// <param name="read">Area reader (start, count) → states.</param>
    /// <returns>Response ADU bytes (data or 02/03 exception).</returns>
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

    /// <summary>Handle FC03/FC04 reads against a register capacity.</summary>
    /// <param name="slaveId">Slave ID to echo.</param>
    /// <param name="function">Raw function code byte.</param>
    /// <param name="data">Request PDU body ([start][count]).</param>
    /// <param name="capacity">Addressable register count of the area.</param>
    /// <param name="read">Area reader (start, count) → values.</param>
    /// <returns>Response ADU bytes (data or 02/03 exception).</returns>
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

    /// <summary>Handle FC05: validate, write the coil, echo the request.</summary>
    /// <param name="slaveId">Slave ID to echo.</param>
    /// <param name="function">Raw function code byte.</param>
    /// <param name="data">Request PDU body ([address][0xFF00/0x0000]).</param>
    /// <param name="store">Target data store.</param>
    /// <returns>Response ADU bytes (echo or 02/03 exception).</returns>
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

    /// <summary>Handle FC06: validate, write the register, echo the request.</summary>
    /// <param name="slaveId">Slave ID to echo.</param>
    /// <param name="function">Raw function code byte.</param>
    /// <param name="data">Request PDU body ([address][value]).</param>
    /// <param name="store">Target data store.</param>
    /// <returns>Response ADU bytes (echo or 02/03 exception).</returns>
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

    /// <summary>Handle FC15: validate, write the coils, echo start/count.</summary>
    /// <param name="slaveId">Slave ID to echo.</param>
    /// <param name="function">Raw function code byte.</param>
    /// <param name="data">Request PDU body ([start][count][byteCount][packed]).</param>
    /// <param name="store">Target data store.</param>
    /// <returns>Response ADU bytes (echo or 02/03 exception).</returns>
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

    /// <summary>Handle FC16: validate, write the registers, echo start/count.</summary>
    /// <param name="slaveId">Slave ID to echo.</param>
    /// <param name="function">Raw function code byte.</param>
    /// <param name="data">Request PDU body ([start][count][byteCount][values]).</param>
    /// <param name="store">Target data store.</param>
    /// <returns>Response ADU bytes (echo or 02/03 exception).</returns>
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

    /// <summary>Handle FC08 by sub-function (counters, restart, listen-only).</summary>
    /// <param name="slaveId">Slave ID to echo.</param>
    /// <param name="function">Raw function code byte.</param>
    /// <param name="data">Request PDU body ([sub-function][payload]).</param>
    /// <param name="store">Target data store (per-ID counters).</param>
    /// <param name="engine">Device-level bus counters.</param>
    /// <returns>Response ADU bytes, null for listen-only entry, or 01/03 exception.</returns>
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
            DiagnosticsCodec.Subfunctions.ForceListenOnlyMode => EnterListenOnly(store),
            DiagnosticsCodec.Subfunctions.ClearCounters => ClearAllCounters(slaveId, function, data, subfunction, store, engine),
            DiagnosticsCodec.Subfunctions.ReturnBusMessageCount => CounterFrame(slaveId, function, subfunction, engine.BusMessageCount),
            DiagnosticsCodec.Subfunctions.ReturnBusCommErrorCount => CounterFrame(
                slaveId, function, subfunction, engine.BusCrcErrorCount + engine.CharErrorCount),
            DiagnosticsCodec.Subfunctions.ReturnSlaveExceptionCount => CounterFrame(slaveId, function, subfunction, store.SlaveExceptionCount),
            DiagnosticsCodec.Subfunctions.ReturnSlaveMessageCount => CounterFrame(slaveId, function, subfunction, store.SlaveMessageCount),
            DiagnosticsCodec.Subfunctions.ReturnSlaveNoResponseCount => CounterFrame(slaveId, function, subfunction, store.NoResponseCount),
            DiagnosticsCodec.Subfunctions.ReturnSlaveNakCount => CounterFrame(slaveId, function, subfunction, store.NakCount),
            DiagnosticsCodec.Subfunctions.ReturnSlaveBusyCount => CounterFrame(slaveId, function, subfunction, store.BusyCount),
            DiagnosticsCodec.Subfunctions.ReturnBusOverrunCount => CounterFrame(slaveId, function, subfunction, engine.OverrunCount),
            DiagnosticsCodec.Subfunctions.ClearOverrunCounter => ClearOverrun(slaveId, function, data, subfunction, engine),
            _ => ExceptionFrame(slaveId, function, ModbusExceptionCode.IllegalFunction),
        };
    }

    /// <summary>Enter listen-only mode: monitor the line, send nothing.</summary>
    /// <param name="store">Store entering listen-only mode.</param>
    /// <returns>Always null (no response by spec).</returns>
    private static byte[]? EnterListenOnly(ModbusDataStore store)
    {
        store.ListenOnly = true;
        return null;
    }

    /// <summary>Handle FC08 sub 0x0001: validate data, exit listen-only, reset event counter.</summary>
    /// <param name="slaveId">Slave ID to echo.</param>
    /// <param name="function">Raw function code byte.</param>
    /// <param name="data">Full sub-function body (echoed on success).</param>
    /// <param name="payload">Data after the sub-function (0000 = keep log, FF00 = clear log).</param>
    /// <param name="store">Target data store.</param>
    /// <returns>Echo ADU bytes, null when exiting listen-only (silent by spec), or 03 exception.</returns>
    private static byte[]? HandleRestartOption(byte slaveId, byte function, byte[] data, byte[] payload, ModbusDataStore store)
    {
        bool wasListenOnly = store.ListenOnly;
        if (payload is not ([0x00, 0x00] or [0xFF, 0x00]))
        {
            // Malformed restart stays silent while listen-only.
            return wasListenOnly ? null : ExceptionFrame(slaveId, function, ModbusExceptionCode.IllegalDataValue);
        }

        store.ListenOnly = false;
        store.ResetEventCount();
        if (payload[0] == 0xFF)
        {
            store.ClearEventLog();
        }

        // A restart out of listen-only mode never returns a response.
        return wasListenOnly ? null : OkFrame(slaveId, function, data);
    }

    /// <summary>Handle FC08 sub 0x000A: reset store + bus counters and the diagnostic register.</summary>
    /// <param name="slaveId">Slave ID to echo.</param>
    /// <param name="function">Raw function code byte.</param>
    /// <param name="data">Request body (payload ignored; response carries 0000).</param>
    /// <param name="subfunction">Sub-function code to echo (0x000A).</param>
    /// <param name="store">Target data store.</param>
    /// <param name="engine">Device-level bus counters.</param>
    /// <returns>Response ADU bytes ([sub-function][0000]).</returns>
    private static byte[] ClearAllCounters(byte slaveId, byte function, byte[] data, ushort subfunction, ModbusDataStore store, ModbusSlaveEngine engine)
    {
        store.ResetCounters();
        engine.ResetBusCounters();
        return OkFrame(slaveId, function, DiagnosticsCodec.Encode(subfunction, [0x00, 0x00]));
    }

    /// <summary>Handle FC08 sub 0x0014: clear the engine overrun counter.</summary>
    /// <param name="slaveId">Slave ID to echo.</param>
    /// <param name="function">Raw function code byte.</param>
    /// <param name="data">Request body (payload ignored; response carries 0000).</param>
    /// <param name="subfunction">Sub-function code to echo (0x0014).</param>
    /// <param name="engine">Device-level bus counters.</param>
    /// <returns>Response ADU bytes ([sub-function][0000]).</returns>
    private static byte[] ClearOverrun(byte slaveId, byte function, byte[] data, ushort subfunction, ModbusSlaveEngine engine)
    {
        engine.ClearOverrun();
        return OkFrame(slaveId, function, DiagnosticsCodec.Encode(subfunction, [0x00, 0x00]));
    }

    /// <summary>Detect an FC08 restart-communications request (sub 0x0001).</summary>
    /// <param name="functionByte">Raw function code byte.</param>
    /// <param name="data">Function PDU body (needs 2+ bytes).</param>
    /// <returns>True for FC08 with sub-function 0x0001 (payload unchecked).</returns>
    private static bool IsRestartRequest(byte functionByte, byte[] data) =>
        TryDiagnosticsSubfunction(functionByte, data, out ushort subfunction)
        && subfunction == DiagnosticsCodec.Subfunctions.RestartCommunicationsOption;

    /// <summary>Build a two-byte FC08 counter response payload.</summary>
    /// <param name="slaveId">Slave ID to echo.</param>
    /// <param name="function">Raw function code byte.</param>
    /// <param name="subfunction">Sub-function code to echo.</param>
    /// <param name="value">Counter value (clamped to 16 bits).</param>
    /// <returns>Response ADU bytes ([sub-function][count BE]).</returns>
    private static byte[] CounterFrame(byte slaveId, byte function, ushort subfunction, long value) =>
        OkFrame(slaveId, function, DiagnosticsCodec.Encode(subfunction, DiagnosticsCodec.CounterData(Clamp16(value))));

    /// <summary>Handle FC0B: report status word (always 0; no program commands exist) + event count.</summary>
    /// <param name="slaveId">Slave ID to echo.</param>
    /// <param name="function">Raw function code byte.</param>
    /// <param name="store">Target data store.</param>
    /// <returns>Response ADU bytes ([status][eventCount]).</returns>
    private static byte[] HandleCommEventCounter(byte slaveId, byte function, ModbusDataStore store) =>
        OkFrame(slaveId, function, CommEventCounterCodec.EncodeResponse(0x0000, Clamp16(store.EventCount)));

    /// <summary>Handle FC0C: report status + event/bus-message counts + log (newest first).</summary>
    /// <param name="slaveId">Slave ID to echo.</param>
    /// <param name="function">Raw function code byte.</param>
    /// <param name="store">Target data store (event count + log).</param>
    /// <param name="engine">Device-level bus message counter.</param>
    /// <returns>Response ADU bytes.</returns>
    private static byte[] HandleCommEventLog(byte slaveId, byte function, ModbusDataStore store, ModbusSlaveEngine engine) =>
        OkFrame(slaveId, function, CommEventLogCodec.EncodeResponse(
            0x0000, Clamp16(store.EventCount), Clamp16(engine.BusMessageCount), store.ReadEventLog()));

    /// <summary>Handle FC11: report server identity, run flag and additional data.</summary>
    /// <param name="slaveId">Slave ID to echo.</param>
    /// <param name="function">Raw function code byte.</param>
    /// <param name="store">Target data store (identity source).</param>
    /// <returns>Response ADU bytes.</returns>
    private static byte[] HandleReportServerId(byte slaveId, byte function, ModbusDataStore store)
    {
        byte[] identity = System.Text.Encoding.ASCII.GetBytes(store.ServerIdentity);
        return OkFrame(slaveId, function, ServerIdCodec.EncodeResponse(identity, true, store.GetAdditionalData()));
    }

    /// <summary>Handle FC14: read file records, guarding the PDU cap.</summary>
    /// <param name="slaveId">Slave ID to echo.</param>
    /// <param name="function">Raw function code byte.</param>
    /// <param name="data">Request PDU body (sub-request section).</param>
    /// <param name="store">Target data store (file areas).</param>
    /// <returns>Response ADU bytes (records, or 02/03/04 exception).</returns>
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

        if (1 + records.Sum(r => 2 + (r.Length * 2)) > ModbusLimits.MaxPduSize)
        {
            // Valid request the device cannot answer within the PDU cap.
            return ExceptionFrame(slaveId, function, ModbusExceptionCode.SlaveDeviceFailure);
        }

        return OkFrame(slaveId, function, FileRecordCodec.EncodeReadResponse(records));
    }

    /// <summary>Handle FC15: write file records, echoing the sub-requests.</summary>
    /// <param name="slaveId">Slave ID to echo.</param>
    /// <param name="function">Raw function code byte.</param>
    /// <param name="data">Request PDU body (subs + values).</param>
    /// <param name="store">Target data store (file areas).</param>
    /// <returns>Response ADU bytes (sub-request echo, or 02/03 exception).</returns>
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

    /// <summary>Handle FC16: apply (current AND andMask) OR (orMask AND NOT andMask), echo the request.</summary>
    /// <param name="slaveId">Slave ID to echo.</param>
    /// <param name="function">Raw function code byte.</param>
    /// <param name="data">Request PDU body ([address][andMask][orMask]).</param>
    /// <param name="store">Target data store.</param>
    /// <returns>Response ADU bytes (echo or 02/03 exception).</returns>
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

    /// <summary>Handle FC17: write first, then read, in one response.</summary>
    /// <param name="slaveId">Slave ID to echo.</param>
    /// <param name="function">Raw function code byte.</param>
    /// <param name="data">Request PDU body (read part + write part).</param>
    /// <param name="store">Target data store.</param>
    /// <returns>Response ADU bytes (read values, or 02/03 exception).</returns>
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

    /// <summary>Handle FC18: snapshot the addressed FIFO queue (non-destructive).</summary>
    /// <param name="slaveId">Slave ID to echo.</param>
    /// <param name="function">Raw function code byte.</param>
    /// <param name="data">Request PDU body (pointer address).</param>
    /// <param name="store">Target data store (FIFO areas).</param>
    /// <returns>Response ADU bytes (values oldest-first, or 02/03 exception).</returns>
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

    /// <summary>Clamp a long counter to 16 bits for wire encoding.</summary>
    /// <param name="value">Counter value.</param>
    /// <returns><paramref name="value"/> capped at 65535.</returns>
    private static ushort Clamp16(long value) => (ushort)Math.Min(value, (long)ushort.MaxValue);

    // FC0C event-byte base values (spec §6.10).
    private const byte ReceiveEventNormal = 0x80; // receive event, unicast
    private const byte ReceiveEventBroadcast = 0xC0; // receive event, broadcast received
    private const byte SendEventNormal = 0x40; // send event, no exception
    private const byte EnteredListenOnlyEvent = 0x20; // entered listen-only mode
    private const byte RestartEvent = 0x00; // communications restart

    /// <summary>Send-event byte for an exception outcome (spec §6.10 bit layout).</summary>
    /// <param name="code">Exception code that was (or would be) reported.</param>
    /// <returns>0x41 (01-03), 0x42 (04), 0x44 (05-06), 0x48 (07), else 0x40.</returns>
    private static byte SendEventFor(ModbusExceptionCode code) => code switch
    {
        ModbusExceptionCode.IllegalFunction
            or ModbusExceptionCode.IllegalDataAddress
            or ModbusExceptionCode.IllegalDataValue => 0x41,
        ModbusExceptionCode.SlaveDeviceFailure => 0x42,
        ModbusExceptionCode.Acknowledge
            or ModbusExceptionCode.SlaveDeviceBusy => 0x44,
        ModbusExceptionCode.NegativeAcknowledge => 0x48,
        _ => SendEventNormal,
    };

    /// <summary>
    /// Dedicated completion byte for listen-only entry / restart, or null for a
    /// generic send event. Interpretation note: the spec's worked FC12 example shows
    /// one byte per operation; dedicated bytes replace the generic send event here.
    /// </summary>
    /// <param name="functionByte">Raw function code byte.</param>
    /// <param name="data">Function PDU body (sub-function peeked).</param>
    /// <returns>0x20 (enter listen-only), 0x00 (restart), or null.</returns>
    private static byte? DedicatedSendEvent(byte functionByte, byte[] data)
    {
        if (!TryDiagnosticsSubfunction(functionByte, data, out ushort subfunction))
        {
            return null;
        }

        if (subfunction == DiagnosticsCodec.Subfunctions.ForceListenOnlyMode)
        {
            return EnteredListenOnlyEvent;
        }

        return subfunction == DiagnosticsCodec.Subfunctions.RestartCommunicationsOption
            ? RestartEvent
            : null;
    }

    /// <summary>Extract an FC08 sub-function code without full validation.</summary>
    /// <param name="functionByte">Raw function code byte (must be 0x08).</param>
    /// <param name="data">Function PDU body (needs 2+ bytes).</param>
    /// <param name="subfunction">Receives the big-endian sub-function on success.</param>
    /// <returns>False unless FC08 with at least 2 body bytes.</returns>
    private static bool TryDiagnosticsSubfunction(byte functionByte, byte[] data, out ushort subfunction)
    {
        subfunction = 0;
        if ((ModbusFunction)functionByte != ModbusFunction.Diagnostics || data.Length < 2)
        {
            return false;
        }

        subfunction = BinaryPrimitives.ReadUInt16BigEndian(data.AsSpan(0, 2));
        return true;
    }

    /// <summary>
    /// Event-counter eligibility (spec §6.9): incremented on successful completion
    /// except polls (FC07), counter fetches (FC0B/FC0C) and counter resets (FC08/01, FC08/0A).
    /// Never on exceptions.
    /// </summary>
    /// <param name="functionByte">Raw function code byte.</param>
    /// <param name="data">Function PDU body (sub-function peeked for FC08).</param>
    /// <returns>True when a successful completion increments the event counter.</returns>
    private static bool IsCountable(byte functionByte, byte[] data)
    {
        if ((ModbusFunction)functionByte is ModbusFunction.ReadExceptionStatus
            or ModbusFunction.GetCommEventCounter
            or ModbusFunction.GetCommEventLog)
        {
            return false;
        }

        // A reset must stay observable as zero: the resetting op itself is not counted.
        return !TryDiagnosticsSubfunction(functionByte, data, out ushort subfunction)
            || subfunction is not (
                DiagnosticsCodec.Subfunctions.RestartCommunicationsOption
                or DiagnosticsCodec.Subfunctions.ClearCounters);
    }

    /// <summary>Check an address span against a capacity.</summary>
    /// <param name="start">Zero-based start index.</param>
    /// <param name="count">Item count.</param>
    /// <param name="capacity">Exclusive capacity bound.</param>
    /// <returns>True when [start, start+count) fits.</returns>
    private static bool InRange(int start, int count, int capacity) =>
        start >= 0 && count >= 0 && start + count <= capacity;

    /// <summary>Encode a normal response ADU.</summary>
    /// <param name="slaveId">Slave ID to echo.</param>
    /// <param name="function">Function code byte to echo.</param>
    /// <param name="body">Response PDU body.</param>
    /// <returns>Full ADU bytes with CRC.</returns>
    private static byte[] OkFrame(byte slaveId, byte function, byte[] body) =>
        new ModbusFrame(slaveId, function, body).Encode();

    /// <summary>Encode an exception response ADU (function MSB set + code byte).</summary>
    /// <param name="slaveId">Slave ID to echo.</param>
    /// <param name="function">Base function code byte.</param>
    /// <param name="code">Exception code to report.</param>
    /// <returns>Full ADU bytes with CRC.</returns>
    private static byte[] ExceptionFrame(byte slaveId, byte function, ModbusExceptionCode code) =>
        new ModbusFrame(slaveId, ExceptionCodec.ExceptionFunction((ModbusFunction)function), ExceptionCodec.Encode(code)).Encode();

    /// <summary>Feed transport line errors into the device-level error counters.</summary>
    /// <param name="sender">Error source (unused).</param>
    /// <param name="error">Reported line error flags; overrun additionally feeds the overrun counter.</param>
    private void OnLineError(object? sender, SerialLineError error)
    {
        // Character-level errors also feed the bus communication-error counter.
        Engine.NoteCharError();
        if ((error & SerialLineError.Overrun) != 0)
        {
            Engine.NoteOverrun();
        }
    }

    /// <summary>Unsubscribe line errors and dispose the transport.</summary>
    /// <returns>A ValueTask completing when the transport is disposed.</returns>
    public ValueTask DisposeAsync()
    {
        _transport.LineError -= OnLineError;
        return _transport.DisposeAsync();
    }
}
