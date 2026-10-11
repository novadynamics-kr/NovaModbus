using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Windows.Threading;
using NovaModbus.Rtu;
using NovaModbus.Rtu.Codecs;
using NovaModbus.Rtu.Commands;
using NovaModbus.Rtu.Transport;
using NovaModbus.Slave.Services;

namespace NovaModbus.Slave.ViewModels;

/// <summary>Root view model: listener lifecycle, slave IDs, data editing, counters.</summary>
public sealed class MainViewModel : ViewModelBase, IAsyncDisposable
{
    private readonly Dictionary<byte, ModbusDataStore> _stores = [];
    private readonly Dictionary<byte, List<(ushort Address, ushort Value)>> _holdingMaps = [];
    private readonly Dictionary<byte, List<(ushort Address, bool IsOn)>> _coilMaps = [];
    private readonly DispatcherTimer _counterTimer;
    private ModbusRtuSlave? _slave;
    private SlaveServer? _server;
    private CancellationTokenSource? _serveCts;
    private string _statusText = "Stopped";
    private string _countersText = string.Empty;
    private string _newSlaveIdText = "1";
    private byte? _selectedSlaveId;
    private string _holdingAddressText = "0";
    private string _holdingValueText = "0";
    private string _holdingEndText = "9";
    private string _coilAddressText = "0";
    private string _coilEndText = "9";
    private string _floatAddressText = "0";
    private string _floatValueText = "0";
    private FloatOrderOption _floatOrder;
    private bool _ignoreCrcErrors = true;
    private bool _simulateBusy;
    private bool _disposed;

    /// <summary>Initializes settings, log, default ID, and the counter timer.</summary>
    public MainViewModel()
    {
        _floatOrder = FloatOrderOptions[0];
        SerialSettings = new SlaveSerialSettingsViewModel();
        StartCommand = new AsyncRelayCommand(StartAsync, () => !IsRunning, OnError);
        StopCommand = new AsyncRelayCommand(StopAsync, () => IsRunning, OnError);
        AddSlaveCommand = new RelayCommand(AddSlave);
        RemoveSlaveCommand = new RelayCommand(RemoveSlave, () => SelectedSlaveId is not null);
        WriteFloatCommand = new RelayCommand(WriteFloat);
        AddHoldingCommand = new RelayCommand(AddHolding);
        AddCoilCommand = new RelayCommand(AddCoil);
        AddHoldingRangeCommand = new RelayCommand(AddHoldingRange);
        AddCoilRangeCommand = new RelayCommand(AddCoilRange);
        SaveMapCommand = new RelayCommand(SaveMap);
        LoadMapCommand = new RelayCommand(LoadMap);
        _counterTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _counterTimer.Tick += (_, _) => RefreshCounters();
        _counterTimer.Start();
        AddSlaveId(1);
    }

    /// <summary>Serial port settings.</summary>
    public SlaveSerialSettingsViewModel SerialSettings { get; }

    /// <summary>Raw traffic log.</summary>
    public ObservableLog CommLog { get; } = new();

    /// <summary>Configured slave IDs.</summary>
    public ObservableCollection<byte> SlaveIds { get; } = [];

    /// <summary>Selected slave ID (drives the editors).</summary>
    public byte? SelectedSlaveId
    {
        get => _selectedSlaveId;
        set
        {
            if (SetProperty(ref _selectedSlaveId, value))
            {
                RemoveSlaveCommand.RaiseCanExecuteChanged();
                RefreshRows();
                RefreshCounters();
            }
        }
    }

    /// <summary>New ID text.</summary>
    public string NewSlaveIdText
    {
        get => _newSlaveIdText;
        set => SetProperty(ref _newSlaveIdText, value);
    }

    /// <summary>Holding-register editor rows for the selected ID.</summary>
    public ObservableCollection<RegisterRow> HoldingRows { get; } = [];

    /// <summary>Coil editor rows for the selected ID.</summary>
    public ObservableCollection<CoilRow> CoilRows { get; } = [];

    /// <summary>Counter text.</summary>
    public string CountersText
    {
        get => _countersText;
        private set => SetProperty(ref _countersText, value);
    }

    /// <summary>Status line.</summary>
    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    /// <summary>Whether the listener runs.</summary>
    public bool IsRunning => _slave is not null;

    /// <summary>Corrupt frames get no response (applied live).</summary>
    public bool IgnoreCrcErrors
    {
        get => _ignoreCrcErrors;
        set
        {
            if (SetProperty(ref _ignoreCrcErrors, value) && _slave is not null)
            {
                _slave.IgnoreCrcErrors = value;
            }
        }
    }

    /// <summary>Answer everything busy (applied live).</summary>
    public bool SimulateBusy
    {
        get => _simulateBusy;
        set
        {
            if (SetProperty(ref _simulateBusy, value) && _slave is not null)
            {
                _slave.SimulateBusy = value;
            }
        }
    }

    /// <summary>Start listening.</summary>
    public AsyncRelayCommand StartCommand { get; }

    /// <summary>Stop listening.</summary>
    public AsyncRelayCommand StopCommand { get; }

    /// <summary>Add the configured slave ID.</summary>
    public RelayCommand AddSlaveCommand { get; }

    /// <summary>Remove the selected slave ID.</summary>
    public RelayCommand RemoveSlaveCommand { get; }

    /// <summary>New holding address text.</summary>
    public string HoldingAddressText
    {
        get => _holdingAddressText;
        set => SetProperty(ref _holdingAddressText, value);
    }

    /// <summary>New holding value text (decimal or 0x hex).</summary>
    public string HoldingValueText
    {
        get => _holdingValueText;
        set => SetProperty(ref _holdingValueText, value);
    }

    /// <summary>New coil address text.</summary>
    public string CoilAddressText
    {
        get => _coilAddressText;
        set => SetProperty(ref _coilAddressText, value);
    }

    /// <summary>Bulk-add end address text (holding).</summary>
    public string HoldingEndText
    {
        get => _holdingEndText;
        set => SetProperty(ref _holdingEndText, value);
    }

    /// <summary>Bulk-add end address text (coils).</summary>
    public string CoilEndText
    {
        get => _coilEndText;
        set => SetProperty(ref _coilEndText, value);
    }

    /// <summary>Add a holding register to the user map.</summary>
    public RelayCommand AddHoldingCommand { get; }

    /// <summary>Add a holding address range to the user map.</summary>
    public RelayCommand AddHoldingRangeCommand { get; }

    /// <summary>Add a coil to the user map.</summary>
    public RelayCommand AddCoilCommand { get; }

    /// <summary>Add a coil address range to the user map.</summary>
    public RelayCommand AddCoilRangeCommand { get; }

    /// <summary>Save the current ID map to a JSON file.</summary>
    public RelayCommand SaveMapCommand { get; }

    /// <summary>Load a map from a JSON file (replaces the current ID map).</summary>
    public RelayCommand LoadMapCommand { get; }

    /// <summary>Supported float word orders (Endian display).</summary>
    public IReadOnlyList<FloatOrderOption> FloatOrderOptions { get; } = FloatOrderOption.All;

    /// <summary>Float write address text.</summary>
    public string FloatAddressText
    {
        get => _floatAddressText;
        set => SetProperty(ref _floatAddressText, value);
    }

    /// <summary>Float value text.</summary>
    public string FloatValueText
    {
        get => _floatValueText;
        set => SetProperty(ref _floatValueText, value);
    }

    /// <summary>Float word order choice.</summary>
    public FloatOrderOption FloatOrder
    {
        get => _floatOrder;
        set => SetProperty(ref _floatOrder, value);
    }

    /// <summary>Write the float to the selected slave store.</summary>
    public RelayCommand WriteFloatCommand { get; }

    private async Task StartAsync(CancellationToken cancellationToken)
    {
        if (IsRunning)
        {
            return;
        }

        SerialSettings settings = SerialSettings.BuildSettings();
        ISerialTransport transport = await TransportFactory.OpenSerialAsync(settings, cancellationToken).ConfigureAwait(false);
        try
        {
            ModbusRtuSlave slave = new(transport, CommLog)
            {
                BaudRate = settings.BaudRate,
                IgnoreCrcErrors = IgnoreCrcErrors,
                SimulateBusy = SimulateBusy,
            };
            foreach (byte id in SlaveIds)
            {
                slave.Engine.AddSlave(id, _stores[id]);
            }

            _slave = slave;
            _server = new SlaveServer(slave);
            _serveCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _ = _server.RunAsync(_serveCts.Token);
            StatusText = $"Listening on {settings.PortName} @ {settings.BaudRate}.";
        }
        catch
        {
            await transport.DisposeAsync().ConfigureAwait(false);
            throw;
        }

        RefreshAll();
    }

    private async Task StopAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        try
        {
            _serveCts?.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // Already torn down.
        }

        _serveCts?.Dispose();
        _serveCts = null;
        if (_slave is not null)
        {
            await _slave.DisposeAsync().ConfigureAwait(false);
            _slave = null;
        }

        _server = null;
        StatusText = "Stopped";
        RefreshAll();
    }

    private void AddSlave()
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

        if (_slave is not null)
        {
            _slave.Engine.AddSlave(id, _stores[id]);
        }

        StatusText = string.Empty;
    }

    private void RemoveSlave()
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
        _slave?.Engine.RemoveSlave(id);
        SelectedSlaveId = SlaveIds.Count > 0 ? SlaveIds[0] : null;
        RefreshRows();
        RefreshCounters();
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

    private void RefreshAll()
    {
        NotifyPropertyChanged(nameof(IsRunning));
        StartCommand.RaiseCanExecuteChanged();
        StopCommand.RaiseCanExecuteChanged();
        AddSlaveCommand.RaiseCanExecuteChanged();
        RemoveSlaveCommand.RaiseCanExecuteChanged();
        RefreshRows();
        RefreshCounters();
    }

    private void RefreshRows()
    {
        HoldingRows.Clear();
        CoilRows.Clear();
        if (SelectedSlaveId is null
            || !_stores.TryGetValue(SelectedSlaveId.Value, out ModbusDataStore? store)
            || !_holdingMaps.TryGetValue(SelectedSlaveId.Value, out List<(ushort Address, ushort Value)>? holdings)
            || !_coilMaps.TryGetValue(SelectedSlaveId.Value, out List<(ushort Address, bool IsOn)>? coils))
        {
            return;
        }

        foreach ((ushort address, ushort _) in holdings.OrderBy(e => e.Address))
        {
            HoldingRows.Add(new RegisterRow(address, store.ReadHolding(address, 1)[0],
                (addr, val) => store.WriteHolding(addr, val)));
        }

        foreach ((ushort address, bool _) in coils.OrderBy(e => e.Address))
        {
            CoilRows.Add(new CoilRow(address, store.ReadCoils(address, 1)[0], store.WriteCoil));
        }
    }

    private void AddHolding()
    {
        try
        {
            ModbusDataStore store = RequireStore();
            ushort address = ParseHexAddress(HoldingAddressText, store.HoldingCount);
            ushort value = ParseUshortValue(HoldingValueText);
            List<(ushort Address, ushort Value)> map = _holdingMaps[SelectedSlaveId!.Value];
            if (map.Any(e => e.Address == address))
            {
                StatusText = $"Address {address} already in the map.";
                return;
            }

            store.WriteHolding(address, value);
            map.Add((address, value));
            StatusText = string.Empty;
            RefreshRows();
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
        }
    }

    /// <summary>Remove holding registers from the map (multi-select).</summary>
    public void RemoveHoldings(IEnumerable<ushort> addresses)
    {
        ArgumentNullException.ThrowIfNull(addresses);
        if (SelectedSlaveId is null || !_holdingMaps.TryGetValue(SelectedSlaveId.Value, out List<(ushort Address, ushort Value)>? map))
        {
            return;
        }

        HashSet<ushort> doomed = [.. addresses];
        map.RemoveAll(e => doomed.Contains(e.Address));
        RefreshRows();
    }

    /// <summary>Remove coils from the map (multi-select).</summary>
    public void RemoveCoils(IEnumerable<ushort> addresses)
    {
        ArgumentNullException.ThrowIfNull(addresses);
        if (SelectedSlaveId is null || !_coilMaps.TryGetValue(SelectedSlaveId.Value, out List<(ushort Address, bool IsOn)>? map))
        {
            return;
        }

        HashSet<ushort> doomed = [.. addresses];
        map.RemoveAll(e => doomed.Contains(e.Address));
        RefreshRows();
    }

    private void AddCoil()
    {
        try
        {
            ModbusDataStore store = RequireStore();
            ushort address = ParseHexAddress(CoilAddressText, store.CoilCount);
            List<(ushort Address, bool IsOn)> map = _coilMaps[SelectedSlaveId!.Value];
            if (map.Any(e => e.Address == address))
            {
                StatusText = $"Address {address} already in the map.";
                return;
            }

            map.Add((address, false));
            StatusText = string.Empty;
            RefreshRows();
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
        }
    }

    private void AddHoldingRange()
    {
        try
        {
            ModbusDataStore store = RequireStore();
            ushort start = ParseMapAddress(HoldingAddressText, store.HoldingCount);
            ushort end = ParseMapAddress(HoldingEndText, store.HoldingCount);
            if (end < start)
            {
                StatusText = $"Invalid range: '{HoldingAddressText}-{HoldingEndText}'.";
                return;
            }

            if (end - start + 1 > ModbusLimits.MaxBulkAdd)
            {
                StatusText = $"Range too large (max {ModbusLimits.MaxBulkAdd}).";
                return;
            }

            ushort value = ParseUshortValue(HoldingValueText);
            List<(ushort Address, ushort Value)> map = _holdingMaps[SelectedSlaveId!.Value];
            int added = 0;
            int skipped = 0;
            for (int address = start; address <= end; address++)
            {
                ushort a = (ushort)address;
                if (map.Any(e => e.Address == a))
                {
                    skipped++;
                    continue;
                }

                store.WriteHolding(a, value);
                map.Add((a, value));
                added++;
            }

            StatusText = $"Added {added}, skipped {skipped}.";
            RefreshRows();
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
        }
    }

    private void AddCoilRange()
    {
        try
        {
            ModbusDataStore store = RequireStore();
            ushort start = ParseMapAddress(CoilAddressText, store.CoilCount);
            ushort end = ParseMapAddress(CoilEndText, store.CoilCount);
            if (end < start)
            {
                StatusText = $"Invalid range: '{CoilAddressText}-{CoilEndText}'.";
                return;
            }

            if (end - start + 1 > ModbusLimits.MaxBulkAdd)
            {
                StatusText = $"Range too large (max {ModbusLimits.MaxBulkAdd}).";
                return;
            }

            List<(ushort Address, bool IsOn)> map = _coilMaps[SelectedSlaveId!.Value];
            int added = 0;
            int skipped = 0;
            for (int address = start; address <= end; address++)
            {
                ushort a = (ushort)address;
                if (map.Any(e => e.Address == a))
                {
                    skipped++;
                    continue;
                }

                map.Add((a, false));
                added++;
            }

            StatusText = $"Added {added}, skipped {skipped}.";
            RefreshRows();
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
        }
    }

    private static ushort ParseMapAddress(string text, int capacity)
    {
        ushort address = ParseHexAddress(text, capacity);
        return address;
    }

    private static ushort ParseHexAddress(string text, int capacity)
    {
        string trimmed = text.Trim();
        ushort address;
        try
        {
            address = trimmed.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                ? Convert.ToUInt16(trimmed[2..], 16)
                : ushort.Parse(trimmed, null);
        }
        catch (Exception ex) when (ex is FormatException or OverflowException)
        {
            throw new ArgumentException($"Invalid address: '{text}'.", ex);
        }

        if (address >= capacity)
        {
            throw new ArgumentException($"Address out of range: '{text}'.");
        }

        return address;
    }

    private void SaveMap()
    {
        try
        {
            ModbusDataStore store = RequireStore();
            Microsoft.Win32.SaveFileDialog dialog = new()
            {
                Filter = "Register map (*.json)|*.json",
                FileName = $"slave{SelectedSlaveId!.Value}-map.json",
            };
            if (dialog.ShowDialog() != true)
            {
                return;
            }

            List<RegisterMapStore.HoldingMapItem> holdings = _holdingMaps[SelectedSlaveId.Value]
                .OrderBy(e => e.Address)
                .Select(e => new RegisterMapStore.HoldingMapItem(e.Address, store.ReadHolding(e.Address, 1)[0]))
                .ToList();
            List<RegisterMapStore.CoilMapItem> coils = _coilMaps[SelectedSlaveId.Value]
                .OrderBy(e => e.Address)
                .Select(e => new RegisterMapStore.CoilMapItem(e.Address, store.ReadCoils(e.Address, 1)[0]))
                .ToList();
            RegisterMapStore.Save(dialog.FileName, holdings, coils);
            StatusText = $"Saved to {dialog.FileName}.";
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
        }
    }

    private void LoadMap()
    {
        try
        {
            ModbusDataStore store = RequireStore();
            Microsoft.Win32.OpenFileDialog dialog = new()
            {
                Filter = "Register map (*.json)|*.json",
            };
            if (dialog.ShowDialog() != true)
            {
                return;
            }

            RegisterMapStore.RegisterMapFile file = RegisterMapStore.Load(dialog.FileName);
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

            List<(ushort Address, ushort Value)> holdings = file.Holding
                .GroupBy(e => e.Address).Select(g => g.First())
                .OrderBy(e => e.Address).Select(e => (e.Address, e.Value)).ToList();
            List<(ushort Address, bool IsOn)> coils = file.Coils
                .GroupBy(e => e.Address).Select(g => g.First())
                .OrderBy(e => e.Address).Select(e => (e.Address, e.IsOn)).ToList();

            foreach ((ushort address, ushort value) in holdings)
            {
                store.WriteHolding(address, value);
            }

            foreach ((ushort address, bool isOn) in coils)
            {
                store.WriteCoil(address, isOn);
            }

            _holdingMaps[SelectedSlaveId!.Value] = holdings;
            _coilMaps[SelectedSlaveId.Value] = coils;
            StatusText = $"Loaded {holdings.Count} holding, {coils.Count} coils from {dialog.FileName}.";
            RefreshRows();
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
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
                : ushort.Parse(trimmed, null);
        }
        catch (Exception ex) when (ex is FormatException or OverflowException)
        {
            throw new ArgumentException($"Invalid value: '{text}'.", ex);
        }
    }

    private void RefreshCounters()
    {
        List<string> lines = [];
        foreach (byte id in SlaveIds)
        {
            if (!_stores.TryGetValue(id, out ModbusDataStore? store))
            {
                continue;
            }

            long requests = _slave?.Engine.GetCount(id) ?? 0;
            lines.Add($"ID {id}: requests={requests} msg={store.SlaveMessageCount} exc={store.SlaveExceptionCount} " +
                $"noresp={store.NoResponseCount} busy={store.BusyCount} events={store.EventCount}");
        }

        if (_slave is not null)
        {
            lines.Add($"Bus: frames={_slave.Engine.BusMessageCount} crcErrors={_slave.Engine.BusCrcErrorCount}");
        }

        CountersText = string.Join(Environment.NewLine, lines);
    }

    private void OnError(Exception ex) => StatusText = $"Error: {ex.Message}";

    private void WriteFloat()
    {
        ushort address;
        try
        {
            address = ParseHexAddress(FloatAddressText, ushort.MaxValue + 1);
        }
        catch (ArgumentException)
        {
            StatusText = $"Invalid address: '{FloatAddressText}'.";
            return;
        }

        if (!float.TryParse(FloatValueText, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
        {
            StatusText = $"Invalid float: '{FloatValueText}'.";
            return;
        }

        WriteFloatAt(address, value, FloatOrder.Order);
    }

    /// <summary>Write a float to the selected slave store (dialog + tab share this).</summary>
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

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _counterTimer.Stop();
        await StopAsync(CancellationToken.None).ConfigureAwait(false);
        GC.SuppressFinalize(this);
    }
}
