using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using NovaModbus.Client.Services;
using NovaModbus.Rtu;
using NovaModbus.Rtu.Codecs;
using NovaModbus.Rtu.Collections;

namespace NovaModbus.Client.ViewModels;

/// <summary>Registered watch list (entries + results).</summary>
/// <remarks>UI-affine (no ConfigureAwait(false)): bound collections mutate on the UI thread.
/// File dialogs live in the view; this model works with plain paths.</remarks>
public sealed partial class WatchViewModel : ObservableObject, IDisposable
{
    private readonly Func<ModbusRtuClient?> _clientProvider;
    private readonly HashSet<WatchEntry> _entryIndex = [];
    private bool _disposed;
    private CancellationTokenSource? _autoCts;
    private TimeSpan _autoInterval = TimeSpan.FromSeconds(1);

    [ObservableProperty]
    public partial string SlaveIdText { get; set; } = "1";

    [ObservableProperty]
    public partial WatchArea Area { get; set; } = WatchArea.HoldingRegisters;

    [ObservableProperty]
    public partial string StartText { get; set; } = "0";

    [ObservableProperty]
    public partial string AddressEndText { get; set; } = "9";

    [ObservableProperty]
    public partial string CountText { get; set; } = "1";

    [ObservableProperty]
    public partial string ExtraText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial double BulkProgress { get; set; }

    [ObservableProperty]
    public partial bool IsBulkWorking { get; set; }

    [ObservableProperty]
    public partial bool AutoRefresh { get; set; }

    [ObservableProperty]
    public partial string AutoIntervalMsText { get; set; } = "1000";

    [ObservableProperty]
    public partial bool IsBusy { get; set; }

    [ObservableProperty]
    public partial string StatusText { get; set; } = string.Empty;

    /// <summary>Initializes with a live client provider (null when disconnected).</summary>
    public WatchViewModel(Func<ModbusRtuClient?> clientProvider)
    {
        ArgumentNullException.ThrowIfNull(clientProvider);
        _clientProvider = clientProvider;
        AddCommand = new RelayCommand(AddEntry);
        AddRangeCommand = new AsyncRelayCommand(AddRangeAsync, () => !IsBulkWorking);
        ReadAllCommand = new AsyncRelayCommand(ReadAllAsync, () => !IsBusy);
        ClearAllCommand = new RelayCommand(ClearAll);
        SelectAllCommand = new RelayCommand(SelectAll);
        DeselectAllCommand = new RelayCommand(DeselectAll);
    }

    /// <summary>Supported areas.</summary>
    public IReadOnlyList<WatchArea> Areas { get; } =
    [
        WatchArea.HoldingRegisters,
        WatchArea.InputRegisters,
        WatchArea.Coils,
        WatchArea.DiscreteInputs,
        WatchArea.ExceptionStatus,
        WatchArea.DiagnosticsCounter,
        WatchArea.CommEventCounter,
        WatchArea.CommEventLog,
        WatchArea.ServerId,
        WatchArea.FileRecord,
        WatchArea.FifoQueue,
    ];

    /// <summary>Registered entries.</summary>
    public BulkObservableCollection<WatchEntry> Entries { get; } = [];

    /// <summary>Result rows (1:1 with entries).</summary>
    public BulkObservableCollection<WatchResult> Results { get; } = [];

    /// <summary>Add the configured entry.</summary>
    public IRelayCommand AddCommand { get; }

    /// <summary>Add entries for the address range.</summary>
    public IAsyncRelayCommand AddRangeCommand { get; }

    /// <summary>Clear all entries and results.</summary>
    public IRelayCommand ClearAllCommand { get; }

    /// <summary>Select all entries (sets IsSelected = true).</summary>
    public IRelayCommand SelectAllCommand { get; }

    /// <summary>Deselect all entries (sets IsSelected = false).</summary>
    public IRelayCommand DeselectAllCommand { get; }

    /// <summary>Read all entries once.</summary>
    public IAsyncRelayCommand ReadAllCommand { get; }

    private void AddEntry()
    {
        try
        {
            if (IsBulkWorking)
            {
                StatusText = "Bulk add running.";
                return;
            }

            WatchEntry entry = ParseEntry();
            if (!_entryIndex.Add(entry))
            {
                StatusText = "Already in the list.";
                return;
            }

            Entries.Add(entry);
            Results.Add(new WatchResult(entry));
            StatusText = string.Empty;
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
        }
    }

    /// <summary>Remove exactly the given instances (reference equality — equal values stay).</summary>
    public void RemoveEntries(IEnumerable<WatchEntry> entries)
    {
        ArgumentNullException.ThrowIfNull(entries);
        HashSet<WatchEntry> doomed = new(entries, ReferenceEqualityComparer.Instance);
        if (doomed.Count == 0)
        {
            return;
        }

        // Build new collections excluding the doomed entries.
        List<WatchEntry> newEntries = [];
        List<WatchResult> newResults = [];
        foreach (WatchEntry entry in Entries)
        {
            if (!doomed.Contains(entry))
            {
                newEntries.Add(entry);
            }
        }
        foreach (WatchResult result in Results)
        {
            if (!doomed.Contains(result.Entry))
            {
                newResults.Add(result);
            }
        }

        // Update collections and index using bulk replacement (single notification).
        Entries.ReplaceAll(newEntries);
        Results.ReplaceAll(newResults);
        _entryIndex.ExceptWith(doomed);
    }

    /// <summary>Clear all entries and results in one operation.</summary>
    public void ClearAll()
    {
        Entries.ReplaceAll([]);
        Results.ReplaceAll([]);
        _entryIndex.Clear();
        StatusText = "Cleared all entries.";
    }

    /// <summary>Select or clear all entries (single render pass).</summary>
    public void SelectAllItems(bool select)
    {
        Entries.SetAllSelected(select);
        // Results mirror Entry.IsSelected via forwarding, which is suppressed during
        // the bulk set: rebind realized result rows with a single Reset.
        Results.Refresh();
        StatusText = select ? $"All {Entries.Count} entries selected." : "Selection cleared.";
    }

    /// <summary>Select all entries (sets IsSelected = true).</summary>
    public void SelectAll() => SelectAllItems(true);

    /// <summary>Deselect all entries (sets IsSelected = false).</summary>
    public void DeselectAll() => SelectAllItems(false);

    /// <summary>Save the watch list to a JSON file.</summary>
    public void SaveWatchTo(string path)
    {
        try
        {
            WatchListStore.Save(path, Entries);
            StatusText = $"Saved {Entries.Count} entries to {path}.";
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
        }
    }

    /// <summary>Load the watch list from a JSON file (replaces current).</summary>
    public void LoadWatchFrom(string path)
    {
        try
        {
            WatchEntry[] loaded = WatchListStore.Load(path);
            _entryIndex.Clear();
            List<WatchEntry> entries = new(loaded.Length);
            List<WatchResult> rows = new(loaded.Length);
            foreach (WatchEntry entry in loaded)
            {
                if (_entryIndex.Add(entry))
                {
                    entries.Add(entry);
                    rows.Add(new WatchResult(entry));
                }
            }

            Entries.ReplaceAll(entries);
            Results.ReplaceAll(rows);

            StatusText = $"Loaded {loaded.Length} entries from {path}.";
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
        }
    }

    private async Task ReadAllAsync(CancellationToken cancellationToken)
    {
        if (IsBusy)
        {
            return;
        }

        IsBusy = true;
        try
        {
            ModbusRtuClient? client = _clientProvider();
            if (client is null)
            {
                MarkAll("Not connected.", "Error");
                return;
            }

            foreach (WatchResult row in Results)
            {
                cancellationToken.ThrowIfCancellationRequested();
                await ReadOneAsync(client, row, cancellationToken);
            }
        }
        finally
        {
            IsBusy = false;
        }
    }

    private static async Task ReadOneAsync(ModbusRtuClient client, WatchResult row, CancellationToken ct)
    {
        WatchEntry entry = row.Entry;
        try
        {
            (string dec, string hex) = entry.Area switch
            {
                WatchArea.HoldingRegisters => SplitRegisters(
                    await client.ReadHoldingRegistersAsync(entry.SlaveId, entry.Start, entry.Count, ct)),
                WatchArea.InputRegisters => SplitRegisters(
                    await client.ReadInputRegistersAsync(entry.SlaveId, entry.Start, entry.Count, ct)),
                WatchArea.Coils => SplitBits(
                    await client.ReadCoilsAsync(entry.SlaveId, entry.Start, entry.Count, ct)),
                WatchArea.DiscreteInputs => SplitBits(
                    await client.ReadDiscreteInputsAsync(entry.SlaveId, entry.Start, entry.Count, ct)),
                WatchArea.ExceptionStatus => await ReadWatchStatusAsync(client, entry, ct),
                WatchArea.DiagnosticsCounter => await ReadWatchDiagnosticsAsync(client, entry, ct),
                WatchArea.CommEventCounter => await ReadWatchEventCounterAsync(client, entry, ct),
                WatchArea.CommEventLog => await ReadWatchEventLogAsync(client, entry, ct),
                WatchArea.ServerId => await ReadWatchServerIdAsync(client, entry, ct),
                WatchArea.FileRecord => await ReadWatchFileAsync(client, entry, ct),
                WatchArea.FifoQueue => await ReadWatchFifoAsync(client, entry, ct),
                _ => throw new InvalidOperationException($"Unknown area {entry.Area}."),
            };
            row.ValueDecText = dec;
            row.ValueHexText = hex;
            row.Timestamp = DateTime.Now.ToString("HH:mm:ss.fff", null);
            row.Status = "OK";
        }
        catch (Exception ex)
        {
            row.Status = $"Error: {ex.Message}";
        }
    }

    private static (string Dec, string Hex) SplitRegisters(ushort[] values) =>
        (ValueFormatter.FormatRegistersDec(values), ValueFormatter.FormatRegistersHex(values));

    private static (string Dec, string Hex) SplitBits(bool[] values) =>
        (ValueFormatter.FormatBits(values), "—");

    private void MarkAll(string values, string status)
    {
        foreach (WatchResult row in Results)
        {
            row.ValueDecText = values;
            row.ValueHexText = "—";
            row.Status = status;
        }
    }

    private static async Task<(string Dec, string Hex)> ReadWatchStatusAsync(ModbusRtuClient client, WatchEntry entry, CancellationToken ct)
    {
        byte status = await client.ReadExceptionStatusAsync(entry.SlaveId, ct);
        return (status.ToString(System.Globalization.CultureInfo.InvariantCulture), $"0x{status:X2}");
    }

    private static async Task<(string Dec, string Hex)> ReadWatchDiagnosticsAsync(ModbusRtuClient client, WatchEntry entry, CancellationToken ct)
    {
        byte[] data = ParseExtraBytes(entry.Extra);
        byte[] echo = await client.DiagnosticsAsync(entry.SlaveId, entry.Start, data, ct);
        return ($"Sub {entry.Start} echo: {ValueFormatter.FormatBytesDec(echo)}", ValueFormatter.FormatBytesHex(echo));
    }

    private static async Task<(string Dec, string Hex)> ReadWatchEventCounterAsync(ModbusRtuClient client, WatchEntry entry, CancellationToken ct)
    {
        (ushort status, ushort events) = await client.GetCommEventCounterAsync(entry.SlaveId, ct);
        return ($"Status: {status}, Events: {events}", $"0x{status:X4}, 0x{events:X4}");
    }

    private static async Task<(string Dec, string Hex)> ReadWatchEventLogAsync(ModbusRtuClient client, WatchEntry entry, CancellationToken ct)
    {
        (ushort status, ushort events, ushort messages, byte[] log) =
            await client.GetCommEventLogAsync(entry.SlaveId, ct);
        return ($"Status: {status}, Events: {events}, Messages: {messages} | {ValueFormatter.FormatBytesDec(log)}",
            ValueFormatter.FormatBytesHex(log));
    }

    private static async Task<(string Dec, string Hex)> ReadWatchServerIdAsync(ModbusRtuClient client, WatchEntry entry, CancellationToken ct)
    {
        (byte[] id, bool running) = await client.ReportServerIdAsync(entry.SlaveId, ct);
        string hex = ValueFormatter.FormatBytesHex(id);
        string ascii = ValueFormatter.FormatDeviceId(id);
        string dec = ascii == hex
            ? ValueFormatter.FormatBytesDec(id)
            : $"{ascii} (Running: {running})";
        return (dec, hex);
    }

    private static async Task<(string Dec, string Hex)> ReadWatchFileAsync(ModbusRtuClient client, WatchEntry entry, CancellationToken ct)
    {
        ushort length = ParseExtraUshort(entry.Extra, "length");
        FileSubRequest[] subs = [new(entry.Start, entry.Count, length)];
        ushort[][] records = await client.ReadFileRecordAsync(entry.SlaveId, subs, ct);
        return (ValueFormatter.FormatRegistersDec(records[0]), ValueFormatter.FormatRegistersHex(records[0]));
    }

    private static async Task<(string Dec, string Hex)> ReadWatchFifoAsync(ModbusRtuClient client, WatchEntry entry, CancellationToken ct)
    {
        ushort[] values = await client.ReadFifoQueueAsync(entry.SlaveId, entry.Start, ct);
        return values.Length == 0
            ? ("(empty queue)", "—")
            : (ValueFormatter.FormatRegistersDec(values), ValueFormatter.FormatRegistersHex(values));
    }

    private static byte[] ParseExtraBytes(string extra)
    {
        string[] tokens = extra.Split([',', ' ', ';'], StringSplitOptions.RemoveEmptyEntries);
        byte[] data = new byte[tokens.Length];
        for (int i = 0; i < tokens.Length; i++)
        {
            string text = tokens[i].Trim();
            try
            {
                data[i] = text.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                    ? Convert.ToByte(text[2..], 16)
                    : byte.Parse(text, null);
            }
            catch (Exception ex) when (ex is FormatException or OverflowException)
            {
                throw new ArgumentException($"Invalid byte '{tokens[i].Trim()}'.", ex);
            }
        }

        return data;
    }

    private static ushort ParseExtraUshort(string extra, string name)
    {
        if (!ushort.TryParse(extra.Trim(), out ushort value) || value == 0)
        {
            throw new ArgumentException($"Invalid {name}: '{extra}'.");
        }

        return value;
    }

    private void StartAuto()
    {
        if (!int.TryParse(AutoIntervalMsText, out int ms) || ms <= 0)
        {
            StatusText = $"Invalid interval: '{AutoIntervalMsText}'.";
            AutoRefresh = false;
            return;
        }

        StopAuto();
        _autoInterval = TimeSpan.FromMilliseconds(ms);
        _autoCts = new CancellationTokenSource();
        _ = AutoLoopAsync(_autoCts.Token);
    }

    /// <summary>Stop auto-refresh (safe when idle).</summary>
    public void StopAuto()
    {
        try
        {
            _autoCts?.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // Already torn down.
        }

        _autoCts?.Dispose();
        _autoCts = null;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        StopAuto();
        GC.SuppressFinalize(this);
    }

    private async Task AutoLoopAsync(CancellationToken ct)
    {
        try
        {
            using PeriodicTimer timer = new(_autoInterval);
            while (await timer.WaitForNextTickAsync(ct))
            {
                await ReadAllAsync(ct);
            }
        }
        catch (OperationCanceledException)
        {
            // Normal stop.
        }
        catch (Exception ex)
        {
            StatusText = $"Auto-refresh stopped: {ex.Message}";
            AutoRefresh = false;
        }
    }

    private WatchEntry ParseEntry()
    {
        byte slaveId = ParseSlaveId();
        ushort start = ParseUshortHex(StartText, "address");
        ushort count = ParseCount();
        ValidateEntryLimits(count);
        ValidateExtra();
        return new WatchEntry(slaveId, Area, start, count, ExtraText.Trim());
    }

    private async Task AddRangeAsync(CancellationToken cancellationToken)
    {
        byte slaveId;
        ushort start;
        ushort end;
        ushort count;
        string extra;
        try
        {
            slaveId = ParseSlaveId();
            start = ParseUshortHex(StartText, "address");
            end = ParseUshortHex(AddressEndText, "end address");
            if (end < start)
            {
                StatusText = $"Invalid range: '{StartText}-{AddressEndText}'.";
                return;
            }

            if (end - start > ModbusLimits.MaxBulkAdd)
            {
                StatusText = $"Range too large (max {ModbusLimits.MaxBulkAdd}).";
                return;
            }

            count = ParseCount();
            ValidateEntryLimits(count);
            ValidateExtra();
            extra = ExtraText.Trim();
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
            return;
        }

        WatchArea area = Area;
        IsBulkWorking = true;
        BulkProgress = 0;
        try
        {
            // Progress<T> marshals back to the UI thread (WinUI bindings need it).
            var progress = new Progress<double>(v => BulkProgress = v);
            int range = end - start + 1;
            List<WatchEntry> fresh = await Task.Run(() =>
            {
                List<WatchEntry> batch = new(range);
                for (int address = start; address <= end; address++)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    WatchEntry entry = new(slaveId, area, (ushort)address, count, extra);
                    lock (_entryIndex)
                    {
                        if (_entryIndex.Add(entry))
                        {
                            batch.Add(entry);
                        }
                    }

                    if ((address - start) % 512 == 0)
                    {
                        ((IProgress<double>)progress).Report(100.0 * (address - start + 1) / range);
                    }
                }

                return batch;
            }, cancellationToken);

            // Merge fresh entries into existing collections using bulk add (single notification).
            Entries.AddRange(fresh);
            Results.AddRange(fresh.Select(e => new WatchResult(e)));

            StatusText = $"Added {fresh.Count}, skipped {range - fresh.Count}.";
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

    private byte ParseSlaveId()
    {
        if (!byte.TryParse(SlaveIdText, out byte slaveId) || slaveId is 0 or > 247)
        {
            throw new ArgumentException($"Invalid slave ID: '{SlaveIdText}' (1-247).");
        }

        return slaveId;
    }

    private ushort ParseCount()
    {
        if (!ushort.TryParse(CountText, out ushort count) || count == 0)
        {
            throw new ArgumentException($"Invalid count: '{CountText}'.");
        }

        return count;
    }

    private static ushort ParseUshortHex(string text, string name)
    {
        try
        {
            return ModbusAddress.Parse(text);
        }
        catch (ArgumentException)
        {
            throw new ArgumentException($"Invalid {name}: '{text}'.");
        }
    }

    private void ValidateEntryLimits(ushort count)
    {
        if (Area is WatchArea.Coils or WatchArea.DiscreteInputs)
        {
            if (count > ModbusLimits.MaxReadBits)
            {
                throw new ArgumentException($"Count exceeds {Area} limit ({ModbusLimits.MaxReadBits}).");
            }
        }
        else if (Area is WatchArea.HoldingRegisters or WatchArea.InputRegisters)
        {
            if (count > ModbusLimits.MaxReadRegisters)
            {
                throw new ArgumentException($"Count exceeds {Area} limit ({ModbusLimits.MaxReadRegisters}).");
            }
        }
    }

    private void ValidateExtra()
    {
        if (Area == WatchArea.FileRecord)
        {
            _ = ParseExtraUshort(ExtraText, "length");
        }

        if (Area == WatchArea.DiagnosticsCounter)
        {
            _ = ParseExtraBytes(ExtraText);
        }
    }

    partial void OnIsBulkWorkingChanged(bool value) => AddRangeCommand.NotifyCanExecuteChanged();

    partial void OnIsBusyChanged(bool value) => ReadAllCommand.NotifyCanExecuteChanged();

    partial void OnAutoRefreshChanged(bool value)
    {
        if (value)
        {
            StartAuto();
        }
        else
        {
            StopAuto();
        }
    }
}
