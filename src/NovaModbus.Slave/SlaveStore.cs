using System.Globalization;
using System.IO;
using NovaModbus.Rtu;
using NovaModbus.Rtu.Codecs;
using NovaModbus.Slave.Services;
using NovaModbus.Slave.ViewModels;

namespace NovaModbus.Slave;

/// <summary>Per-ID data stores, user maps, and editor rows (plain state; the form owns the controls).</summary>
/// <remarks>UI-affine (no ConfigureAwait(false)): row lists feed virtual grids on the UI thread.</remarks>
public sealed class SlaveStore : IDisposable
{
    private readonly Func<ModbusRtuSlave?> _slaveProvider;
    private readonly Dictionary<byte, ModbusDataStore> _stores = [];
    private readonly Dictionary<byte, Dictionary<ushort, ushort>> _holdingMaps = [];
    private readonly Dictionary<byte, HashSet<ushort>> _coilMaps = [];
    private bool _disposed;
    private string _statusText = "Stopped";

    /// <summary>Initializes with a live slave provider (null when stopped).</summary>
    public SlaveStore(Func<ModbusRtuSlave?> slaveProvider)
    {
        ArgumentNullException.ThrowIfNull(slaveProvider);
        _slaveProvider = slaveProvider;
        FloatOrder = FloatOrderOption.All[0];
        AddSlaveId(1);
    }

    /// <summary>Configured slave IDs (sorted).</summary>
    public List<byte> SlaveIds { get; } = [];

    /// <summary>Selected slave ID (drives the editors).</summary>
    public byte? SelectedSlaveId { get; set; }

    /// <summary>Holding-register editor rows for the selected ID (sorted by address).</summary>
    public List<RegisterRow> HoldingRows { get; } = [];

    /// <summary>Coil editor rows for the selected ID (sorted by address).</summary>
    public List<CoilRow> CoilRows { get; } = [];

    /// <summary>Bulk operation progress (0-100).</summary>
    public double BulkProgress { get; private set; }

    /// <summary>Whether a bulk operation runs.</summary>
    public bool IsBulkWorking { get; private set; }

    /// <summary>New ID text.</summary>
    public string NewSlaveIdText { get; set; } = "1";

    /// <summary>New holding address text.</summary>
    public string HoldingAddressText { get; set; } = "0";

    /// <summary>New holding value text (decimal or 0x hex).</summary>
    public string HoldingValueText { get; set; } = "0";

    /// <summary>Bulk-add end address text (holding).</summary>
    public string HoldingEndText { get; set; } = "9";

    /// <summary>New coil address text.</summary>
    public string CoilAddressText { get; set; } = "0";

    /// <summary>Bulk-add end address text (coils).</summary>
    public string CoilEndText { get; set; } = "9";

    /// <summary>Float write address text.</summary>
    public string FloatAddressText { get; set; } = "0";

    /// <summary>Float value text.</summary>
    public string FloatValueText { get; set; } = "0";

    /// <summary>Float word order choice.</summary>
    public FloatOrderOption FloatOrder { get; set; }

    /// <summary>Supported float word orders.</summary>
    public IReadOnlyList<FloatOrderOption> FloatOrderOptions { get; } = FloatOrderOption.All;

    /// <summary>Status line.</summary>
    public string StatusText
    {
        get => _statusText;
        private set
        {
            if (_statusText != value)
            {
                _statusText = value;
                StatusChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }

    /// <summary>Raised when <see cref="StatusText"/> changes.</summary>
    public event EventHandler? StatusChanged;

    /// <summary>Live slave (null when stopped).</summary>
    public ModbusRtuSlave? Slave => _slaveProvider();

    /// <summary>Whether the listener runs.</summary>
    public bool IsRunning => Slave is not null;

    /// <summary>Store for an ID (used to wire the live engine).</summary>
    public ModbusDataStore? GetStore(byte id) =>
        _stores.TryGetValue(id, out ModbusDataStore? store) ? store : null;

    /// <summary>Set the running status line.</summary>
    public void SetRunningStatus(string text) => StatusText = text;

    /// <summary>Add the configured slave ID.</summary>
    public void AddSlave()
    {
        if (!byte.TryParse(NewSlaveIdText, out byte id) || id is 0 or > 247)
        {
            StatusText = $"Invalid slave ID: '{NewSlaveIdText}' (1-247).";
            return;
        }

        if (!AddSlaveId(id))
        {
            StatusText = $"Slave ID {id} already exists.";
            return;
        }

        if (Slave is not null && _stores.TryGetValue(id, out ModbusDataStore? store))
        {
            Slave.Engine.AddSlave(id, store);
        }

        StatusText = string.Empty;
    }

    /// <summary>Remove the selected slave ID.</summary>
    public void RemoveSelectedSlave()
    {
        if (SelectedSlaveId is null)
        {
            return;
        }

        byte id = SelectedSlaveId.Value;
        _stores.Remove(id);
        _holdingMaps.Remove(id);
        _coilMaps.Remove(id);
        SlaveIds.Remove(id);
        Slave?.Engine.RemoveSlave(id);
        SelectedSlaveId = SlaveIds.Count > 0 ? SlaveIds[0] : null;
        RefreshRows();
    }

    /// <summary>Rebuild editor rows for the selected ID.</summary>
    public void RefreshRows()
    {
        HoldingRows.Clear();
        CoilRows.Clear();
        if (SelectedSlaveId is null
            || !_stores.TryGetValue(SelectedSlaveId.Value, out ModbusDataStore? store)
            || !_holdingMaps.TryGetValue(SelectedSlaveId.Value, out Dictionary<ushort, ushort>? holdings)
            || !_coilMaps.TryGetValue(SelectedSlaveId.Value, out HashSet<ushort>? coils))
        {
            return;
        }

        foreach (ushort address in holdings.Keys.OrderBy(a => a))
        {
            HoldingRows.Add(new RegisterRow(address, store.ReadHolding(address, 1)[0],
                (addr, val) => store.WriteHolding(addr, val)));
        }

        foreach (ushort address in coils.OrderBy(a => a))
        {
            CoilRows.Add(new CoilRow(address, store.ReadCoils(address, 1)[0], store.WriteCoil));
        }
    }

    /// <summary>Add a holding register to the user map.</summary>
    public void AddHolding()
    {
        try
        {
            if (IsBulkWorking)
            {
                StatusText = "Bulk add running.";
                return;
            }

            ModbusDataStore store = RequireStore();
            ushort address = ModbusAddress.Parse(HoldingAddressText, store.HoldingCount);
            ushort value = ParseUshortValue(HoldingValueText);
            Dictionary<ushort, ushort> map = _holdingMaps[SelectedSlaveId!.Value];
            if (map.ContainsKey(address))
            {
                StatusText = $"Address {address} already in the map.";
                return;
            }

            store.WriteHolding(address, value);
            map[address] = value;
            StatusText = string.Empty;
            HoldingRows.Insert(FindInsertionIndex(HoldingRows, r => r.Address, address),
                new RegisterRow(address, value, (addr, val) => store.WriteHolding(addr, val)));
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
        }
    }

    /// <summary>Add a coil to the user map.</summary>
    public void AddCoil()
    {
        try
        {
            if (IsBulkWorking)
            {
                StatusText = "Bulk add running.";
                return;
            }

            ModbusDataStore store = RequireStore();
            ushort address = ModbusAddress.Parse(CoilAddressText, store.CoilCount);
            HashSet<ushort> map = _coilMaps[SelectedSlaveId!.Value];
            if (!map.Add(address))
            {
                StatusText = $"Address {address} already in the map.";
                return;
            }

            StatusText = string.Empty;
            bool value = store.ReadCoils(address, 1)[0];
            CoilRows.Insert(FindInsertionIndex(CoilRows, r => r.Address, address),
                new CoilRow(address, value, store.WriteCoil));
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
        }
    }

    /// <summary>Remove holding rows by grid index (native selection path, O(n) single pass).</summary>
    public void RemoveHoldingsAt(IReadOnlyCollection<int> indices)
    {
        ArgumentNullException.ThrowIfNull(indices);
        if (indices.Count == 0 || HoldingRows.Count == 0)
        {
            return;
        }

        if (SelectedSlaveId is null || !_holdingMaps.TryGetValue(SelectedSlaveId.Value, out Dictionary<ushort, ushort>? map))
        {
            return;
        }

        if (indices.Count >= HoldingRows.Count)
        {
            foreach (RegisterRow row in HoldingRows)
            {
                map.Remove(row.Address);
            }

            HoldingRows.Clear();
            StatusText = "Removed all holding entries.";
            return;
        }

        HashSet<int> doomed = new(indices);
        List<RegisterRow> survivors = new(HoldingRows.Count - doomed.Count);
        int removed = 0;
        for (int i = 0; i < HoldingRows.Count; i++)
        {
            if (doomed.Contains(i))
            {
                map.Remove(HoldingRows[i].Address);
                removed++;
            }
            else
            {
                survivors.Add(HoldingRows[i]);
            }
        }

        if (removed == 0)
        {
            return;
        }

        HoldingRows.Clear();
        HoldingRows.AddRange(survivors);
        StatusText = $"Removed {removed} holding entries.";
    }

    /// <summary>Remove coil rows by grid index (native selection path, O(n) single pass).</summary>
    public void RemoveCoilsAt(IReadOnlyCollection<int> indices)
    {
        ArgumentNullException.ThrowIfNull(indices);
        if (indices.Count == 0 || CoilRows.Count == 0)
        {
            return;
        }

        if (SelectedSlaveId is null || !_coilMaps.TryGetValue(SelectedSlaveId.Value, out HashSet<ushort>? map))
        {
            return;
        }

        if (indices.Count >= CoilRows.Count)
        {
            foreach (CoilRow row in CoilRows)
            {
                map.Remove(row.Address);
            }

            CoilRows.Clear();
            StatusText = "Removed all coil entries.";
            return;
        }

        HashSet<int> doomed = new(indices);
        List<CoilRow> survivors = new(CoilRows.Count - doomed.Count);
        int removed = 0;
        for (int i = 0; i < CoilRows.Count; i++)
        {
            if (doomed.Contains(i))
            {
                map.Remove(CoilRows[i].Address);
                removed++;
            }
            else
            {
                survivors.Add(CoilRows[i]);
            }
        }

        if (removed == 0)
        {
            return;
        }

        CoilRows.Clear();
        CoilRows.AddRange(survivors);
        StatusText = $"Removed {removed} coil entries.";
    }

    /// <summary>Add a holding address range to the user map.</summary>
    public async Task AddHoldingRangeAsync(CancellationToken cancellationToken = default)
    {
        ModbusDataStore store;
        ushort start;
        ushort end;
        ushort value;
        try
        {
            store = RequireStore();
            start = ModbusAddress.Parse(HoldingAddressText, store.HoldingCount);
            end = ModbusAddress.Parse(HoldingEndText, store.HoldingCount);
            if (end < start)
            {
                StatusText = $"Invalid range: '{HoldingAddressText}-{HoldingEndText}'.";
                return;
            }

            if (end - start > ModbusLimits.MaxBulkAdd)
            {
                StatusText = $"Range too large (max {ModbusLimits.MaxBulkAdd}).";
                return;
            }

            value = ParseUshortValue(HoldingValueText);
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
            return;
        }

        Dictionary<ushort, ushort> map = _holdingMaps[SelectedSlaveId!.Value];
        await RunBulkAsync(start, end, a =>
        {
            if (!map.ContainsKey(a))
            {
                store.WriteHolding(a, value);
                map[a] = value;
            }
        }, cancellationToken);
        StatusText = $"Added range {start}-{end}.";
        RefreshRows();
    }

    /// <summary>Add a coil address range to the user map.</summary>
    public async Task AddCoilRangeAsync(CancellationToken cancellationToken = default)
    {
        ModbusDataStore store;
        ushort start;
        ushort end;
        try
        {
            store = RequireStore();
            start = ModbusAddress.Parse(CoilAddressText, store.CoilCount);
            end = ModbusAddress.Parse(CoilEndText, store.CoilCount);
            if (end < start)
            {
                StatusText = $"Invalid range: '{CoilAddressText}-{CoilEndText}'.";
                return;
            }

            if (end - start > ModbusLimits.MaxBulkAdd)
            {
                StatusText = $"Range too large (max {ModbusLimits.MaxBulkAdd}).";
                return;
            }
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
            return;
        }

        HashSet<ushort> map = _coilMaps[SelectedSlaveId!.Value];
        await RunBulkAsync(start, end, a => map.Add(a), cancellationToken);
        StatusText = $"Added range {start}-{end}.";
        RefreshRows();
    }

    /// <summary>Save the current ID map to a JSON file.</summary>
    public void SaveMapTo(string path)
    {
        try
        {
            ModbusDataStore store = RequireStore();
            List<RegisterMapStore.HoldingMapItem> holdings = _holdingMaps[SelectedSlaveId!.Value]
                .OrderBy(e => e.Key)
                .Select(e => new RegisterMapStore.HoldingMapItem(e.Key, store.ReadHolding(e.Key, 1)[0]))
                .ToList();
            List<RegisterMapStore.CoilMapItem> coils = _coilMaps[SelectedSlaveId.Value]
                .OrderBy(a => a)
                .Select(a => new RegisterMapStore.CoilMapItem(a, store.ReadCoils(a, 1)[0]))
                .ToList();
            RegisterMapStore.Save(path, holdings, coils);
            StatusText = $"Saved to {path}.";
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
        }
    }

    /// <summary>Load a map from a JSON file (replaces the current ID map).</summary>
    public void LoadMapFrom(string path)
    {
        try
        {
            ModbusDataStore store = RequireStore();
            RegisterMapStore.RegisterMapFile file = RegisterMapStore.Load(path);
            foreach (RegisterMapStore.HoldingMapItem item in file.Holding)
            {
                if (item.Address >= store.HoldingCount)
                {
                    throw new InvalidDataException($"Address {item.Address} out of range.");
                }
            }

            foreach (RegisterMapStore.CoilMapItem item in file.Coils)
            {
                if (item.Address >= store.CoilCount)
                {
                    throw new InvalidDataException($"Coil {item.Address} out of range.");
                }
            }

            Dictionary<ushort, ushort> holdings = file.Holding
                .GroupBy(e => e.Address).Select(g => g.First())
                .ToDictionary(e => e.Address, e => e.Value);
            Dictionary<ushort, bool> coilValues = file.Coils
                .GroupBy(e => e.Address).Select(g => g.First())
                .ToDictionary(e => e.Address, e => e.IsOn);
            HashSet<ushort> coils = [.. coilValues.Keys];

            foreach ((ushort address, ushort value) in holdings)
            {
                store.WriteHolding(address, value);
            }

            foreach ((ushort address, bool isOn) in coilValues)
            {
                store.WriteCoil(address, isOn);
            }

            _holdingMaps[SelectedSlaveId!.Value] = holdings;
            _coilMaps[SelectedSlaveId.Value] = coils;
            StatusText = $"Loaded {holdings.Count} holding, {coils.Count} coils from {path}.";
            RefreshRows();
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
        }
    }

    /// <summary>Write a float to the selected slave store.</summary>
    public void WriteFloatAt(ushort address, float value, FloatWordOrder order)
    {
        try
        {
            if (SelectedSlaveId is null || !_stores.TryGetValue(SelectedSlaveId.Value, out ModbusDataStore? store))
            {
                StatusText = "Select a slave ID.";
                return;
            }

            if (address + 1 >= store.HoldingCount)
            {
                StatusText = $"Invalid address: '{address}' (needs 2 registers).";
                return;
            }

            ushort[] regs = Float32Codec.Encode(value, order);
            store.WriteHolding(address, regs);
            StatusText = $"Wrote {value} as 0x{regs[0]:X4} 0x{regs[1]:X4} @ {address} ({FloatWordOrderText.ToDisplayString(order)}).";
            RefreshRows();
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
        }
    }

    /// <summary>Build the counters text (called on a 1s timer).</summary>
    public string BuildCountersText()
    {
        List<string> lines = [];
        ModbusRtuSlave? slave = Slave;
        foreach (byte id in SlaveIds)
        {
            if (!_stores.TryGetValue(id, out ModbusDataStore? store))
            {
                continue;
            }

            long requests = slave?.Engine.GetCount(id) ?? 0;
            lines.Add($"ID {id}: requests={requests} msg={store.SlaveMessageCount} exc={store.SlaveExceptionCount} " +
                $"noresp={store.NoResponseCount} busy={store.BusyCount} nak={store.NakCount} events={store.EventCount}");
        }

        if (slave is not null)
        {
            lines.Add($"Bus: frames={slave.Engine.BusMessageCount} crcErrors={slave.Engine.BusCrcErrorCount}");
        }

        return string.Join(Environment.NewLine, lines);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        GC.SuppressFinalize(this);
    }

    private bool AddSlaveId(byte id)
    {
        if (!_stores.TryAdd(id, new ModbusDataStore()))
        {
            return false;
        }

        _holdingMaps[id] = [];
        _coilMaps[id] = [];
        int index = SlaveIds.TakeWhile(existing => existing < id).Count();
        SlaveIds.Insert(index, id);
        SelectedSlaveId ??= id;
        return true;
    }

    private async Task RunBulkAsync(ushort start, ushort end, Action<ushort> work, CancellationToken ct)
    {
        IsBulkWorking = true;
        BulkProgress = 0;
        try
        {
            // Progress<T> marshals back to the UI thread.
            var progress = new Progress<double>(v => BulkProgress = v);
            int total = end - start + 1;
            await Task.Run(() =>
            {
                for (int address = start; address <= end; address++)
                {
                    ct.ThrowIfCancellationRequested();
                    work((ushort)address);
                    if ((address - start) % 512 == 0)
                    {
                        ((IProgress<double>)progress).Report(100.0 * (address - start + 1) / total);
                    }
                }
            }, ct);
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
        }
        finally
        {
            IsBulkWorking = false;
            BulkProgress = 0;
        }
    }

    private ModbusDataStore RequireStore()
    {
        if (SelectedSlaveId is null || !_stores.TryGetValue(SelectedSlaveId.Value, out ModbusDataStore? store))
        {
            throw new InvalidOperationException("Select a slave ID.");
        }

        return store;
    }

    private static ushort ParseUshortValue(string text)
    {
        string trimmed = text.Trim();
        try
        {
            return trimmed.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                ? Convert.ToUInt16(trimmed[2..], 16)
                : ushort.Parse(trimmed, CultureInfo.InvariantCulture);
        }
        catch (Exception ex) when (ex is FormatException or OverflowException)
        {
            throw new ArgumentException($"Invalid value: '{text}'.", ex);
        }
    }

    private static int FindInsertionIndex<T>(IList<T> items, Func<T, ushort> addressSelector, ushort address)
    {
        for (int i = 0; i < items.Count; i++)
        {
            if (addressSelector(items[i]) > address)
            {
                return i;
            }
        }

        return items.Count;
    }
}
