namespace NovaModbus.Rtu;

/// <summary>Thread-safe Modbus data store for one slave ID.</summary>
public sealed class ModbusDataStore
{
    private readonly object _sync = new();
    private readonly bool[] _coils;
    private readonly bool[] _discreteInputs;
    private readonly ushort[] _holdingRegisters;
    private readonly ushort[] _inputRegisters;

    /// <summary>Initializes a store with the given capacities.</summary>
    public ModbusDataStore(int coilCount = 1000, int discreteCount = 1000, int holdingCount = 1000, int inputCount = 1000)
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
