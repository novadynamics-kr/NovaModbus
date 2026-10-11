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
}
