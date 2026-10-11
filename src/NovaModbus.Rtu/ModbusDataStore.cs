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
    private int _eventCount;

    /// <summary>Initializes a store with the given capacities (defaults cover 0x0000-0xFFFF).</summary>
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
    public ushort[] ReadHolding(ushort start, ushort count)
    {
        lock (_sync)
        {
            CheckRange(start, count, _holdingRegisters.Length);
            return _holdingRegisters.AsSpan(start, count).ToArray();
        }
    }

    /// <summary>Write holding registers (bounds checked).</summary>
    public void WriteHolding(ushort start, ReadOnlySpan<ushort> values)
    {
        lock (_sync)
        {
            CheckRange(start, values.Length, _holdingRegisters.Length);
            values.CopyTo(_holdingRegisters.AsSpan(start, values.Length));
        }
    }

    /// <summary>Write single holding register (bounds checked).</summary>
    public void WriteHolding(ushort address, ushort value)
    {
        lock (_sync)
        {
            CheckRange(address, 1, _holdingRegisters.Length);
            _holdingRegisters[address] = value;
        }
    }

    /// <summary>Read input registers (bounds checked).</summary>
    public ushort[] ReadInput(ushort start, ushort count)
    {
        lock (_sync)
        {
            CheckRange(start, count, _inputRegisters.Length);
            return _inputRegisters.AsSpan(start, count).ToArray();
        }
    }

    /// <summary>Seed input registers (simulator side, bounds checked).</summary>
    public void WriteInput(ushort start, ReadOnlySpan<ushort> values)
    {
        lock (_sync)
        {
            CheckRange(start, values.Length, _inputRegisters.Length);
            values.CopyTo(_inputRegisters.AsSpan(start, values.Length));
        }
    }

    /// <summary>Read coils (bounds checked).</summary>
    public bool[] ReadCoils(ushort start, ushort count)
    {
        lock (_sync)
        {
            CheckRange(start, count, _coils.Length);
            return _coils.AsSpan(start, count).ToArray();
        }
    }

    /// <summary>Write single coil (bounds checked).</summary>
    public void WriteCoil(ushort address, bool value)
    {
        lock (_sync)
        {
            CheckRange(address, 1, _coils.Length);
            _coils[address] = value;
        }
    }

    /// <summary>Write a run of coils (bounds checked).</summary>
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
    public bool[] ReadDiscrete(ushort start, ushort count)
    {
        lock (_sync)
        {
            CheckRange(start, count, _discreteInputs.Length);
            return _discreteInputs.AsSpan(start, count).ToArray();
        }
    }

    /// <summary>Seed discrete inputs (simulator side, bounds checked).</summary>
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

    /// <summary>Gets or sets the server identity string (FC11, ASCII).</summary>
    public string ServerIdentity { get; set; } = "NovaModbus";

    /// <summary>Gets a copy of the FC11 additional data.</summary>
    public byte[] GetAdditionalData()
    {
        lock (_sync)
        {
            return [.. _additionalData];
        }
    }

    /// <summary>Sets the FC11 additional data.</summary>
    public void SetAdditionalData(byte[] data)
    {
        ArgumentNullException.ThrowIfNull(data);
        lock (_sync)
        {
            _additionalData = [.. data];
        }
    }

    /// <summary>Creates or resizes a file record area (simulator setup).</summary>
    public void EnsureFile(ushort fileNumber, int recordCapacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(recordCapacity, 0);
        lock (_sync)
        {
            _files[fileNumber] = new ushort[recordCapacity];
        }
    }

    /// <summary>Whether a file record area exists.</summary>
    public bool FileExists(ushort fileNumber)
    {
        lock (_sync)
        {
            return _files.ContainsKey(fileNumber);
        }
    }

    /// <summary>Read file records (bounds checked).</summary>
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
    public void EnsureFifo(ushort pointerAddress)
    {
        lock (_sync)
        {
            _fifos.TryAdd(pointerAddress, new Queue<ushort>());
        }
    }

    /// <summary>Whether a FIFO queue exists.</summary>
    public bool FifoExists(ushort pointerAddress)
    {
        lock (_sync)
        {
            return _fifos.ContainsKey(pointerAddress);
        }
    }

    /// <summary>Append a value to a FIFO queue (simulator side).</summary>
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

    /// <summary>Gets the comm-event count for this ID.</summary>
    public int EventCount => Volatile.Read(ref _eventCount);

    /// <summary>Record a served request.</summary>
    public void NoteSlaveMessage() => Interlocked.Increment(ref _slaveMessageCount);

    /// <summary>Record an exception response.</summary>
    public void NoteSlaveException() => Interlocked.Increment(ref _slaveExceptionCount);

    /// <summary>Record an addressed-but-unanswered request.</summary>
    public void NoteNoResponse() => Interlocked.Increment(ref _noResponseCount);

    /// <summary>Record a busy response.</summary>
    public void NoteBusy() => Interlocked.Increment(ref _busyCount);

    /// <summary>Record a comm event and append a log byte.</summary>
    public void NoteEvent(byte eventByte)
    {
        lock (_sync)
        {
            _eventCount++;
            _eventLog.Enqueue(eventByte);
            while (_eventLog.Count > ModbusLimits.MaxEventLogBytes)
            {
                _eventLog.Dequeue();
            }
        }
    }

    /// <summary>Snapshot of the comm event log (oldest first).</summary>
    public byte[] ReadEventLog()
    {
        lock (_sync)
        {
            return [.. _eventLog];
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
            _eventCount = 0;
            DiagnosticRegister = 0;
        }
    }

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
