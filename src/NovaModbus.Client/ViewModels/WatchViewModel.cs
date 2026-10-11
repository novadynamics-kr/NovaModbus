using System.Collections.ObjectModel;
using NovaModbus.Client.Services;
using NovaModbus.Rtu;
using NovaModbus.Rtu.Codecs;
using NovaModbus.Rtu.Commands;

namespace NovaModbus.Client.ViewModels;

/// <summary>Registered watch list (entries + results). Execution lands in stage 2.</summary>
public sealed class WatchViewModel : ViewModelBase, IDisposable
{
    private readonly Func<ModbusRtuClient?> _clientProvider;
    private bool _disposed;
    private string _slaveIdText = "1";
    private WatchArea _area = WatchArea.HoldingRegisters;
    private string _startText = "0";
    private string _addressEndText = "9";
    private string _countText = "1";

    /// <summary>Initializes with a live client provider (null when disconnected).</summary>
    public WatchViewModel(Func<ModbusRtuClient?> clientProvider)
    {
        ArgumentNullException.ThrowIfNull(clientProvider);
        _clientProvider = clientProvider;
        AddCommand = new RelayCommand(AddEntry);
        AddRangeCommand = new AsyncRelayCommand(AddRangeAsync, () => !IsBulkWorking, OnError);
        ReadAllCommand = new AsyncRelayCommand(ReadAllAsync, () => !IsBusy, OnError);
        SaveWatchCommand = new RelayCommand(SaveWatch);
        LoadWatchCommand = new RelayCommand(LoadWatch);
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
    public ObservableCollection<WatchEntry> Entries
    {
        get => _entries;
        private set => SetProperty(ref _entries, value);
    }

    private ObservableCollection<WatchEntry> _entries = [];

    /// <summary>Result rows (1:1 with entries).</summary>
    public ObservableCollection<WatchResult> Results
    {
        get => _results;
        private set => SetProperty(ref _results, value);
    }

    private ObservableCollection<WatchResult> _results = [];

    private readonly HashSet<WatchEntry> _entryIndex = [];

    /// <summary>Bulk operation progress (0-100).</summary>
    public double BulkProgress
    {
        get => _bulkProgress;
        private set => SetProperty(ref _bulkProgress, value);
    }

    private double _bulkProgress;

    /// <summary>Whether a bulk operation runs.</summary>
    public bool IsBulkWorking
    {
        get => _isBulkWorking;
        private set
        {
            if (SetProperty(ref _isBulkWorking, value))
            {
                AddRangeCommand.RaiseCanExecuteChanged();
            }
        }
    }

    private bool _isBulkWorking;

    /// <summary>New-entry slave ID text.</summary>
    public string SlaveIdText
    {
        get => _slaveIdText;
        set => SetProperty(ref _slaveIdText, value);
    }

    /// <summary>New-entry area.</summary>
    public WatchArea Area
    {
        get => _area;
        set => SetProperty(ref _area, value);
    }

    /// <summary>New-entry start address text (decimal or 0x hex).</summary>
    public string StartText
    {
        get => _startText;
        set => SetProperty(ref _startText, value);
    }

    /// <summary>Bulk-add end address text (decimal or 0x hex).</summary>
    public string AddressEndText
    {
        get => _addressEndText;
        set => SetProperty(ref _addressEndText, value);
    }

    /// <summary>New-entry count text (record number for files).</summary>
    public string CountText
    {
        get => _countText;
        set => SetProperty(ref _countText, value);
    }

    /// <summary>New-entry extra text (diagnostic data bytes, file length).</summary>
    public string ExtraText
    {
        get => _extraText;
        set => SetProperty(ref _extraText, value);
    }

    private string _extraText = string.Empty;

    /// <summary>Add the configured entry.</summary>
    public RelayCommand AddCommand { get; }

    /// <summary>Add entries for the address range.</summary>
    public AsyncRelayCommand AddRangeCommand { get; }

    /// <summary>Save the watch list to a JSON file.</summary>
    public RelayCommand SaveWatchCommand { get; }

    /// <summary>Load the watch list from a JSON file (replaces current).</summary>
    public RelayCommand LoadWatchCommand { get; }

    /// <summary>Read all entries once.</summary>
    public AsyncRelayCommand ReadAllCommand { get; }

    /// <summary>Auto-refresh on interval.</summary>
    public bool AutoRefresh
    {
        get => _autoRefresh;
        set
        {
            if (SetProperty(ref _autoRefresh, value))
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
    }

    private bool _autoRefresh;
    private CancellationTokenSource? _autoCts;
    private TimeSpan _autoInterval = TimeSpan.FromSeconds(1);

    /// <summary>Auto-refresh interval text in milliseconds.</summary>
    public string AutoIntervalMsText
    {
        get => _autoIntervalMsText;
        set => SetProperty(ref _autoIntervalMsText, value);
    }

    private string _autoIntervalMsText = "1000";

    /// <summary>Read-all in progress.</summary>
    public bool IsBusy
    {
        get => _isBusy;
        private set
        {
            if (SetProperty(ref _isBusy, value))
            {
                ReadAllCommand.RaiseCanExecuteChanged();
            }
        }
    }

    private bool _isBusy;

    /// <summary>Last add/remove message.</summary>
    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    private string _statusText = string.Empty;

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

        foreach (WatchResult row in Results.Where(r => doomed.Contains(r.Entry)).ToArray())
        {
            Results.Remove(row);
        }

        foreach (WatchEntry entry in Entries.Where(doomed.Contains).ToArray())
        {
            Entries.Remove(entry);
            _entryIndex.Remove(entry);
        }
    }

    private void SaveWatch()
    {
        try
        {
            Microsoft.Win32.SaveFileDialog dialog = new()
            {
                Filter = "Watch list (*.json)|*.json",
                FileName = "watchlist.json",
            };
            if (dialog.ShowDialog() != true)
            {
                return;
            }

            WatchListStore.Save(dialog.FileName, Entries);
            StatusText = $"Saved {Entries.Count} entries to {dialog.FileName}.";
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
        }
    }

    private void LoadWatch()
    {
        try
        {
            Microsoft.Win32.OpenFileDialog dialog = new()
            {
                Filter = "Watch list (*.json)|*.json",
            };
            if (dialog.ShowDialog() != true)
            {
                return;
            }

            WatchEntry[] loaded = WatchListStore.Load(dialog.FileName);
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

            Entries = new ObservableCollection<WatchEntry>(entries);
            Results = new ObservableCollection<WatchResult>(rows);

            StatusText = $"Loaded {loaded.Length} entries from {dialog.FileName}.";
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

            foreach (WatchResult row in Results.ToArray())
            {
                cancellationToken.ThrowIfCancellationRequested();
                await ReadOneAsync(client, row, cancellationToken).ConfigureAwait(false);
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
                    await client.ReadHoldingRegistersAsync(entry.SlaveId, entry.Start, entry.Count, ct).ConfigureAwait(false)),
                WatchArea.InputRegisters => SplitRegisters(
                    await client.ReadInputRegistersAsync(entry.SlaveId, entry.Start, entry.Count, ct).ConfigureAwait(false)),
                WatchArea.Coils => SplitBits(
                    await client.ReadCoilsAsync(entry.SlaveId, entry.Start, entry.Count, ct).ConfigureAwait(false)),
                WatchArea.DiscreteInputs => SplitBits(
                    await client.ReadDiscreteInputsAsync(entry.SlaveId, entry.Start, entry.Count, ct).ConfigureAwait(false)),
                WatchArea.ExceptionStatus => await ReadWatchStatusAsync(client, entry, ct).ConfigureAwait(false),
                WatchArea.DiagnosticsCounter => await ReadWatchDiagnosticsAsync(client, entry, ct).ConfigureAwait(false),
                WatchArea.CommEventCounter => await ReadWatchEventCounterAsync(client, entry, ct).ConfigureAwait(false),
                WatchArea.CommEventLog => await ReadWatchEventLogAsync(client, entry, ct).ConfigureAwait(false),
                WatchArea.ServerId => await ReadWatchServerIdAsync(client, entry, ct).ConfigureAwait(false),
                WatchArea.FileRecord => await ReadWatchFileAsync(client, entry, ct).ConfigureAwait(false),
                WatchArea.FifoQueue => await ReadWatchFifoAsync(client, entry, ct).ConfigureAwait(false),
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
        byte status = await client.ReadExceptionStatusAsync(entry.SlaveId, ct).ConfigureAwait(false);
        return (status.ToString(System.Globalization.CultureInfo.InvariantCulture), $"0x{status:X2}");
    }

    private static async Task<(string Dec, string Hex)> ReadWatchDiagnosticsAsync(ModbusRtuClient client, WatchEntry entry, CancellationToken ct)
    {
        byte[] data = ParseExtraBytes(entry.Extra);
        byte[] echo = await client.DiagnosticsAsync(entry.SlaveId, entry.Start, data, ct).ConfigureAwait(false);
        return ($"Sub {entry.Start} echo: {ValueFormatter.FormatBytesDec(echo)}", ValueFormatter.FormatBytesHex(echo));
    }

    private static async Task<(string Dec, string Hex)> ReadWatchEventCounterAsync(ModbusRtuClient client, WatchEntry entry, CancellationToken ct)
    {
        (ushort status, ushort events) = await client.GetCommEventCounterAsync(entry.SlaveId, ct).ConfigureAwait(false);
        return ($"Status: {status}, Events: {events}", $"0x{status:X4}, 0x{events:X4}");
    }

    private static async Task<(string Dec, string Hex)> ReadWatchEventLogAsync(ModbusRtuClient client, WatchEntry entry, CancellationToken ct)
    {
        (ushort status, ushort events, ushort messages, byte[] log) =
            await client.GetCommEventLogAsync(entry.SlaveId, ct).ConfigureAwait(false);
        return ($"Status: {status}, Events: {events}, Messages: {messages} | {ValueFormatter.FormatBytesDec(log)}",
            ValueFormatter.FormatBytesHex(log));
    }

    private static async Task<(string Dec, string Hex)> ReadWatchServerIdAsync(ModbusRtuClient client, WatchEntry entry, CancellationToken ct)
    {
        (byte[] id, bool running) = await client.ReportServerIdAsync(entry.SlaveId, ct).ConfigureAwait(false);
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
        ushort[][] records = await client.ReadFileRecordAsync(entry.SlaveId, subs, ct).ConfigureAwait(false);
        return (ValueFormatter.FormatRegistersDec(records[0]), ValueFormatter.FormatRegistersHex(records[0]));
    }

    private static async Task<(string Dec, string Hex)> ReadWatchFifoAsync(ModbusRtuClient client, WatchEntry entry, CancellationToken ct)
    {
        ushort[] values = await client.ReadFifoQueueAsync(entry.SlaveId, entry.Start, ct).ConfigureAwait(false);
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

    private void OnError(Exception ex) => StatusText = $"Error: {ex.Message}";

    private void StartAuto()
    {
        if (!int.TryParse(AutoIntervalMsText, out int ms) || ms <= 0)
        {
            StatusText = $"Invalid interval: '{AutoIntervalMsText}'.";
            _autoRefresh = false;
            NotifyPropertyChanged(nameof(AutoRefresh));
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
            // Intentionally UI-affine (no ConfigureAwait(false)): Results is bound.
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
            _autoRefresh = false;
            NotifyPropertyChanged(nameof(AutoRefresh));
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

            if (end - start + 1 > ModbusLimits.MaxBulkAdd)
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
            List<WatchEntry> fresh = await Task.Run(() =>
            {
                List<WatchEntry> batch = new(end - start + 1);
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
                        BulkProgress = 100.0 * (address - start + 1) / (end - start + 1);
                    }
                }

                return batch;
            }, cancellationToken);
            // Intentionally UI-affine below (no ConfigureAwait(false)):
            // bound Entries/Results must mutate on the UI thread.

            foreach (WatchEntry entry in fresh)
            {
                Entries.Add(entry);
                Results.Add(new WatchResult(entry));
            }

            StatusText = $"Added {fresh.Count}, skipped {end - start + 1 - fresh.Count}.";
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
}
