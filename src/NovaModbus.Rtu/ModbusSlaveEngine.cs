using System.Collections.Concurrent;

namespace NovaModbus.Rtu;

/// <summary>Multi slave-ID engine: SlaveId to DataStore routing with per-ID counters.</summary>
public sealed class ModbusSlaveEngine
{
    private readonly ConcurrentDictionary<byte, ModbusDataStore> _stores = new();
    private readonly ConcurrentDictionary<byte, long> _requestCounts = new();

    /// <summary>Add or replace a slave ID store. Returns false for reserved IDs (0 broadcast).</summary>
    public bool AddSlave(byte slaveId, ModbusDataStore? store = null)
    {
        if (slaveId == 0 || slaveId > 247)
        {
            return false;
        }

        _stores[slaveId] = store ?? new ModbusDataStore();
        _requestCounts.TryAdd(slaveId, 0);
        return true;
    }

    /// <summary>Remove a slave ID. Returns true when removed.</summary>
    public bool RemoveSlave(byte slaveId) => _stores.TryRemove(slaveId, out _) && _requestCounts.TryRemove(slaveId, out _);

    /// <summary>Try get the store for a slave ID.</summary>
    public bool TryGetStore(byte slaveId, out ModbusDataStore? store) => _stores.TryGetValue(slaveId, out store);

    /// <summary>Known slave IDs in ascending order.</summary>
    public byte[] SlaveIds => _stores.Keys.OrderBy(id => id).ToArray();

    /// <summary>Increment and return the request counter for a slave ID.</summary>
    public long IncrementCount(byte slaveId) => _requestCounts.AddOrUpdate(slaveId, 1, (_, v) => v + 1);

    /// <summary>Get the request counter for a slave ID (0 when unknown).</summary>
    public long GetCount(byte slaveId) => _requestCounts.TryGetValue(slaveId, out long v) ? v : 0;

    /// <summary>Gets the valid-frame bus message count (device level).</summary>
    public long BusMessageCount => Interlocked.Read(ref _busMessageCount);

    /// <summary>Gets the CRC-error frame count (device level).</summary>
    public long BusCrcErrorCount => Interlocked.Read(ref _busCrcErrorCount);

    /// <summary>Gets the character-level error count: parity, framing, overrun (device level).</summary>
    public long CharErrorCount => Interlocked.Read(ref _charErrorCount);

    /// <summary>Gets the character-overrun count (device level).</summary>
    public long OverrunCount => Interlocked.Read(ref _overrunCount);

    /// <summary>Record a CRC-valid frame on the line.</summary>
    public void NoteBusMessage() => Interlocked.Increment(ref _busMessageCount);

    /// <summary>Record a CRC-error frame on the line.</summary>
    public void NoteBusCrcError() => Interlocked.Increment(ref _busCrcErrorCount);

    /// <summary>Record a character-level line error (parity/framing/overrun).</summary>
    public void NoteCharError() => Interlocked.Increment(ref _charErrorCount);

    /// <summary>Record a character-overrun condition on the line.</summary>
    public void NoteOverrun() => Interlocked.Increment(ref _overrunCount);

    /// <summary>Clear the overrun counter and flag.</summary>
    public void ClearOverrun() => Interlocked.Exchange(ref _overrunCount, 0);

    /// <summary>Reset device-level bus counters.</summary>
    public void ResetBusCounters()
    {
        Interlocked.Exchange(ref _busMessageCount, 0);
        Interlocked.Exchange(ref _busCrcErrorCount, 0);
        Interlocked.Exchange(ref _charErrorCount, 0);
        Interlocked.Exchange(ref _overrunCount, 0);
    }

    private long _busMessageCount;
    private long _busCrcErrorCount;
    private long _charErrorCount;
    private long _overrunCount;
}
