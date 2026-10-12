using NovaModbus.Rtu.Codecs;

namespace NovaModbus.Rtu;

/// <summary>Thread-safe Modbus data store for one slave ID.</summary>
public sealed class ModbusDataStore
{
    private readonly object _sync = new();
    private readonly bool[] _coils;
    private readonly bool[] _discreteInputs;
    private readonly ushort[] _holdingRegisters;
    private readonly ushort[] _inputRegisters;
    private readonly Dictionary<ushort, ushort[]> _files = [];
    private readonly Dictionary<ushort, Queue<ushort>> _fifos = [];
    private readonly Queue<byte> _eventLog = [];
    private byte[] _additionalData = [];
    private int _slaveMessageCount;
    private int _slaveExceptionCount;
    private int _noResponseCount;
    private int _busyCount;
    private int _nakCount;
    private int _eventCount;

    /// <summary>Initializes a store with the given capacities (defaults cover 0x0000-0xFFFF).</summary>
    /// <param name="coilCount">Coil capacity (must be positive).</param>
    /// <param name="discreteCount">Discrete-input capacity (must be positive).</param>
    /// <param name="holdingCount">Holding-register capacity (must be positive).</param>
    /// <param name="inputCount">Input-register capacity (must be positive).</param>
    public ModbusDataStore(
        int coilCount = ModbusLimits.MaxAddress + 1,
        int discreteCount = ModbusLimits.MaxAddress + 1,
        int holdingCount = ModbusLimits.MaxAddress + 1,
        int inputCount = ModbusLimits.MaxAddress + 1)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(coilCount, 0);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(discreteCount, 0);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(holdingCount, 0);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(inputCount, 0);
        _coils = new bool[coilCount];
        _discreteInputs = new bool[discreteCount];
        _holdingRegisters = new ushort[holdingCount];
        _inputRegisters = new ushort[inputCount];
    }

    /// <summary>Gets coil count.</summary>
    public int CoilCount => _coils.Length;

    /// <summary>Gets discrete input count.</summary>
    public int DiscreteCount => _discreteInputs.Length;

    /// <summary>Gets holding register count.</summary>
    public int HoldingCount => _holdingRegisters.Length;

    /// <summary>Gets input register count.</summary>
    public int InputCount => _inputRegisters.Length;

    /// <summary>Read holding registers (bounds checked).</summary>
    /// <param name="start">Zero-based start address.</param>
    /// <param name="count">Registers to read.</param>
    /// <returns>Register values in address order.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the range exceeds capacity.</exception>
    public ushort[] ReadHolding(ushort start, ushort count)
    {
        lock (_sync)
        {
            CheckRange(start, count, _holdingRegisters.Length);
            return _holdingRegisters.AsSpan(start, count).ToArray();
        }
    }

    /// <summary>Write holding registers (bounds checked).</summary>
    /// <param name="start">Zero-based start address.</param>
    /// <param name="values">Values to store in address order.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the range exceeds capacity.</exception>
    public void WriteHolding(ushort start, ReadOnlySpan<ushort> values)
    {
        lock (_sync)
        {
            CheckRange(start, values.Length, _holdingRegisters.Length);
            values.CopyTo(_holdingRegisters.AsSpan(start, values.Length));
        }
    }

    /// <summary>Write single holding register (bounds checked).</summary>
    /// <param name="address">Zero-based address.</param>
    /// <param name="value">Value to store.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the address exceeds capacity.</exception>
    public void WriteHolding(ushort address, ushort value)
    {
        lock (_sync)
        {
            CheckRange(address, 1, _holdingRegisters.Length);
            _holdingRegisters[address] = value;
        }
    }

    /// <summary>Read input registers (bounds checked).</summary>
    /// <param name="start">Zero-based start address.</param>
    /// <param name="count">Registers to read.</param>
    /// <returns>Register values in address order.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the range exceeds capacity.</exception>
    public ushort[] ReadInput(ushort start, ushort count)
    {
        lock (_sync)
        {
            CheckRange(start, count, _inputRegisters.Length);
            return _inputRegisters.AsSpan(start, count).ToArray();
        }
    }

    /// <summary>Seed input registers (simulator side, bounds checked).</summary>
    /// <param name="start">Zero-based start address.</param>
    /// <param name="values">Values to store in address order.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the range exceeds capacity.</exception>
    public void WriteInput(ushort start, ReadOnlySpan<ushort> values)
    {
        lock (_sync)
        {
            CheckRange(start, values.Length, _inputRegisters.Length);
            values.CopyTo(_inputRegisters.AsSpan(start, values.Length));
        }
    }

    /// <summary>Read coils (bounds checked).</summary>
    /// <param name="start">Zero-based start address.</param>
    /// <param name="count">Coils to read.</param>
    /// <returns>Coil states in address order.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the range exceeds capacity.</exception>
    public bool[] ReadCoils(ushort start, ushort count)
    {
        lock (_sync)
        {
            CheckRange(start, count, _coils.Length);
            return _coils.AsSpan(start, count).ToArray();
        }
    }

    /// <summary>Write single coil (bounds checked).</summary>
    /// <param name="address">Zero-based address.</param>
    /// <param name="value">State to store.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the address exceeds capacity.</exception>
    public void WriteCoil(ushort address, bool value)
    {
        lock (_sync)
        {
            CheckRange(address, 1, _coils.Length);
            _coils[address] = value;
        }
    }

    /// <summary>Write a run of coils (bounds checked).</summary>
    /// <param name="start">Zero-based start address.</param>
    /// <param name="values">States to store in address order.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the range exceeds capacity.</exception>
    public void WriteCoils(ushort start, ReadOnlySpan<bool> values)
    {
        lock (_sync)
        {
            CheckRange(start, values.Length, _coils.Length);
            for (int i = 0; i < values.Length; i++)
            {
                _coils[start + i] = values[i];
            }
        }
    }

    /// <summary>Read discrete inputs (bounds checked).</summary>
    /// <param name="start">Zero-based start address.</param>
    /// <param name="count">Inputs to read.</param>
    /// <returns>Input states in address order.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the range exceeds capacity.</exception>
    public bool[] ReadDiscrete(ushort start, ushort count)
    {
        lock (_sync)
        {
            CheckRange(start, count, _discreteInputs.Length);
            return _discreteInputs.AsSpan(start, count).ToArray();
        }
    }

    /// <summary>Seed discrete inputs (simulator side, bounds checked).</summary>
    /// <param name="start">Zero-based start address.</param>
    /// <param name="values">States to store in address order.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the range exceeds capacity.</exception>
    public void WriteDiscrete(ushort start, ReadOnlySpan<bool> values)
    {
        lock (_sync)
        {
            CheckRange(start, values.Length, _discreteInputs.Length);
            for (int i = 0; i < values.Length; i++)
            {
                _discreteInputs[start + i] = values[i];
            }
        }
    }

    /// <summary>Gets or sets the exception status byte (FC07).</summary>
    public byte ExceptionStatus { get; set; }

    /// <summary>Gets or sets the diagnostic register (FC08 sub 0x0002).</summary>
    public ushort DiagnosticRegister { get; set; }

    /// <summary>Gets or sets listen-only mode (FC08 sub 0x0004 / 0x0001).</summary>
    public bool ListenOnly { get; set; }

    /// <summary>Gets or sets the server identity string (FC11, ASCII).</summary>
    public string ServerIdentity { get; set; } = "NovaModbus";

    /// <summary>Gets a copy of the FC11 additional data.</summary>
    /// <returns>Copy of the additional identity bytes.</returns>
    public byte[] GetAdditionalData()
    {
        lock (_sync)
        {
            return [.. _additionalData];
        }
    }

    /// <summary>Sets the FC11 additional data.</summary>
    /// <param name="data">Identity bytes to store (copied).</param>
    public void SetAdditionalData(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        lock (_sync)
        {
            _additionalData = [.. data];
        }
    }

    /// <summary>Creates or resizes a file record area (simulator setup).</summary>
    /// <param name="fileNumber">File number to create.</param>
    /// <param name="recordCapacity">Register capacity (must be positive).</param>
    public void EnsureFile(ushort fileNumber, int recordCapacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(recordCapacity, 0);
        lock (_sync)
        {
            _files[fileNumber] = new ushort[recordCapacity];
        }
    }

    /// <summary>Whether a file record area exists.</summary>
    /// <param name="fileNumber">File number to look up.</param>
    /// <returns>True when the file area was created via <see cref="EnsureFile"/>.</returns>
    public bool FileExists(ushort fileNumber)
    {
        lock (_sync)
        {
            return _files.ContainsKey(fileNumber);
        }
    }

    /// <summary>Read file records (bounds checked).</summary>
    /// <param name="fileNumber">File number (must exist).</param>
    /// <param name="recordNumber">Zero-based start record.</param>
    /// <param name="length">Registers to read.</param>
    /// <returns>Record values in order.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown for unknown files or out-of-range spans.</exception>
    public ushort[] ReadFileRecord(ushort fileNumber, ushort recordNumber, ushort length)
    {
        lock (_sync)
        {
            if (!_files.TryGetValue(fileNumber, out ushort[]? file))
            {
                throw new ArgumentOutOfRangeException(nameof(fileNumber), "Unknown file number.");
            }

            CheckRange(recordNumber, length, file.Length);
            return file.AsSpan(recordNumber, length).ToArray();
        }
    }

    /// <summary>Write file records (bounds checked).</summary>
    /// <param name="fileNumber">File number (must exist).</param>
    /// <param name="recordNumber">Zero-based start record.</param>
    /// <param name="values">Values to store in order.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown for unknown files or out-of-range spans.</exception>
    public void WriteFileRecord(ushort fileNumber, ushort recordNumber, ReadOnlySpan<ushort> values)
    {
        lock (_sync)
        {
            if (!_files.TryGetValue(fileNumber, out ushort[]? file))
            {
                throw new ArgumentOutOfRangeException(nameof(fileNumber), "Unknown file number.");
            }

            CheckRange(recordNumber, values.Length, file.Length);
            values.CopyTo(file.AsSpan(recordNumber, values.Length));
        }
    }

    /// <summary>Creates an empty FIFO queue (simulator setup).</summary>
    /// <param name="pointerAddress">Pointer address identifying the queue.</param>
    public void EnsureFifo(ushort pointerAddress)
    {
        lock (_sync)
        {
            _fifos.TryAdd(pointerAddress, new Queue<ushort>());
        }
    }

    /// <summary>Whether a FIFO queue exists.</summary>
    /// <param name="pointerAddress">Pointer address to look up.</param>
    /// <returns>True when the queue was created via <see cref="EnsureFifo"/>.</returns>
    public bool FifoExists(ushort pointerAddress)
    {
        lock (_sync)
        {
            return _fifos.ContainsKey(pointerAddress);
        }
    }

    /// <summary>Append a value to a FIFO queue (simulator side).</summary>
    /// <param name="pointerAddress">Pointer address identifying the queue.</param>
    /// <param name="value">Value to enqueue.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown for unknown pointers.</exception>
    public void EnqueueFifo(ushort pointerAddress, ushort value)
    {
        lock (_sync)
        {
            if (!_fifos.TryGetValue(pointerAddress, out Queue<ushort>? queue))
            {
                throw new ArgumentOutOfRangeException(nameof(pointerAddress), "Unknown FIFO pointer.");
            }

            queue.Enqueue(value);
        }
    }

    /// <summary>Non-destructive snapshot of a FIFO queue.</summary>
    /// <param name="pointerAddress">Pointer address identifying the queue.</param>
    /// <returns>Queued values oldest-first; the queue is unchanged.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown for unknown pointers.</exception>
    public ushort[] PeekFifo(ushort pointerAddress)
    {
        lock (_sync)
        {
            if (!_fifos.TryGetValue(pointerAddress, out Queue<ushort>? queue))
            {
                throw new ArgumentOutOfRangeException(nameof(pointerAddress), "Unknown FIFO pointer.");
            }

            return [.. queue];
        }
    }

    /// <summary>Gets the served-message count for this ID.</summary>
    public int SlaveMessageCount => Volatile.Read(ref _slaveMessageCount);

    /// <summary>Gets the exception-response count for this ID.</summary>
    public int SlaveExceptionCount => Volatile.Read(ref _slaveExceptionCount);

    /// <summary>Gets the no-response count for this ID.</summary>
    public int NoResponseCount => Volatile.Read(ref _noResponseCount);

    /// <summary>Gets the busy-response count for this ID.</summary>
    public int BusyCount => Volatile.Read(ref _busyCount);

    /// <summary>Gets the NAK-response count for this ID (FC08 sub 0x0010).</summary>
    public int NakCount => Volatile.Read(ref _nakCount);

    /// <summary>Gets the comm-event count for this ID.</summary>
    public int EventCount => Volatile.Read(ref _eventCount);

    /// <summary>Record a served request.</summary>
    /// <remarks>Thread-safe; backs the FC08 sub 0x0E counter.</remarks>
    public void NoteSlaveMessage() => Interlocked.Increment(ref _slaveMessageCount);

    /// <summary>Record an exception response.</summary>
    /// <remarks>Thread-safe; backs the FC08 sub 0x0D counter.</remarks>
    public void NoteSlaveException() => Interlocked.Increment(ref _slaveExceptionCount);

    /// <summary>Record an addressed-but-unanswered request.</summary>
    /// <remarks>Thread-safe; backs the FC08 sub 0x0F counter.</remarks>
    public void NoteNoResponse() => Interlocked.Increment(ref _noResponseCount);

    /// <summary>Record a busy response.</summary>
    /// <remarks>Thread-safe; backs the FC08 sub 0x11 counter.</remarks>
    public void NoteBusy() => Interlocked.Increment(ref _busyCount);

    /// <summary>Record a NAK response.</summary>
    /// <remarks>Thread-safe; backs the FC08 sub 0x10 counter.</remarks>
    public void NoteNak() => Interlocked.Increment(ref _nakCount);

    /// <summary>Record a comm event and append a log byte.</summary>
    /// <param name="eventByte">Event byte to append (counter unchanged).</param>
    [Obsolete("Use NoteReceiveEvent/NoteSendEvent plus NoteEventCount (spec §6.10 semantics).")]
    public void NoteEvent(byte eventByte) => NoteReceiveEvent(eventByte);

    /// <summary>Append a receive-event byte (no counter change).</summary>
    /// <param name="eventByte">Event byte per spec §6.10 layout (bit 7 set).</param>
    public void NoteReceiveEvent(byte eventByte)
    {
        lock (_sync)
        {
            AppendEvent(eventByte);
        }
    }

    /// <summary>Append a send-event byte (no counter change).</summary>
    /// <param name="eventByte">Event byte per spec §6.10 layout (bit 7 clear, bit 6 set).</param>
    public void NoteSendEvent(byte eventByte)
    {
        lock (_sync)
        {
            AppendEvent(eventByte);
        }
    }

    /// <summary>Count one successful message completion (FC0B event counter).</summary>
    public void NoteEventCount()
    {
        lock (_sync)
        {
            _eventCount++;
        }
    }

    /// <summary>Reset the comm-event counter (FC08 sub 0x0001).</summary>
    public void ResetEventCount()
    {
        lock (_sync)
        {
            _eventCount = 0;
        }
    }

    /// <summary>Append a log byte, evicting the oldest beyond 64 entries.</summary>
    /// <param name="eventByte">Event byte to append.</param>
    private void AppendEvent(byte eventByte)
    {
        _eventLog.Enqueue(eventByte);
        while (_eventLog.Count > ModbusLimits.MaxEventLogBytes)
        {
            _eventLog.Dequeue();
        }
    }

    /// <summary>Snapshot of the comm event log (newest first, per spec §6.10).</summary>
    /// <returns>Event bytes, most recent first, at most 64.</returns>
    public byte[] ReadEventLog()
    {
        lock (_sync)
        {
            return [.. _eventLog.Reverse()];
        }
    }

    /// <summary>Clear the comm event log.</summary>
    public void ClearEventLog()
    {
        lock (_sync)
        {
            _eventLog.Clear();
        }
    }

    /// <summary>Reset counters and the diagnostic register (FC08 sub 0x000A).</summary>
    public void ResetCounters()
    {
        lock (_sync)
        {
            _slaveMessageCount = 0;
            _slaveExceptionCount = 0;
            _noResponseCount = 0;
            _busyCount = 0;
            _nakCount = 0;
            _eventCount = 0;
            DiagnosticRegister = 0;
        }
    }

    /// <summary>Validate an address span against a capacity.</summary>
    /// <param name="start">Zero-based start index.</param>
    /// <param name="count">Item count.</param>
    /// <param name="length">Exclusive capacity bound.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown for negative inputs or spans past <paramref name="length"/>.</exception>
    private static void CheckRange(int start, int count, int length)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(start, 0);
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 0);
        if (start + count > length)
        {
            throw new ArgumentOutOfRangeException(nameof(start), "Address range out of store bounds.");
        }
    }
}
