using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Threading;
using NovaModbus.Rtu;
using NovaModbus.Rtu.Codecs;
using NovaModbus.Rtu.Collections;
using NovaModbus.Rtu.Commands;
using NovaModbus.Rtu.Transport;
using NovaModbus.Slave.Services;

namespace NovaModbus.Slave.ViewModels;

/// <summary>Root view model: listener lifecycle, slave IDs, data editing, counters.</summary>
public sealed class MainViewModel : ViewModelBase, IAsyncDisposable
{
    private readonly Dictionary<byte, ModbusDataStore> _stores = [];
    private readonly Dictionary<byte, Dictionary<ushort, ushort>> _holdingMaps = [];
    private readonly Dictionary<byte, HashSet<ushort>> _coilMaps = [];
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
    private double _bulkProgress;
    private bool _isBulkWorking;
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
        AddHoldingRangeCommand = new AsyncRelayCommand(AddHoldingRangeAsync, () => !IsBulkWorking, OnError);
        AddCoilRangeCommand = new AsyncRelayCommand(AddCoilRangeAsync, () => !IsBulkWorking, OnError);
        SaveMapCommand = new RelayCommand(SaveMap);
        LoadMapCommand = new RelayCommand(LoadMap);
        SelectAllHoldingsCommand = new RelayCommand(SelectAllHoldings);
        DeselectAllHoldingsCommand = new RelayCommand(DeselectAllHoldings);
        SelectAllCoilsCommand = new RelayCommand(SelectAllCoils);
        DeselectAllCoilsCommand = new RelayCommand(DeselectAllCoils);
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
    public BulkObservableCollection<RegisterRow> HoldingRows
    {
        get => _holdingRows;
        private set => SetProperty(ref _holdingRows, value);
    }

    private BulkObservableCollection<RegisterRow> _holdingRows = [];

    /// <summary>Coil editor rows for the selected ID.</summary>
    public BulkObservableCollection<CoilRow> CoilRows
    {
        get => _coilRows;
        private set => SetProperty(ref _coilRows, value);
    }

    private BulkObservableCollection<CoilRow> _coilRows = [];

    /// <summary>Bulk operation progress (0-100).</summary>
    public double BulkProgress
    {
        get => _bulkProgress;
        private set => SetProperty(ref _bulkProgress, value);
    }

    /// <summary>Whether a bulk operation runs.</summary>
    public bool IsBulkWorking
    {
        get => _isBulkWorking;
        private set
        {
            if (SetProperty(ref _isBulkWorking, value))
            {
                AddHoldingRangeCommand.RaiseCanExecuteChanged();
                AddCoilRangeCommand.RaiseCanExecuteChanged();
            }
        }
    }

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
        set => SetProperty(ref _statusText, value);
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

    /// <summary>Select all holding register rows.</summary>
    public RelayCommand SelectAllHoldingsCommand { get; }

    /// <summary>Deselect all holding register rows.</summary>
    public RelayCommand DeselectAllHoldingsCommand { get; }

    /// <summary>Select all coil rows.</summary>
    public RelayCommand SelectAllCoilsCommand { get; }

    /// <summary>Deselect all coil rows.</summary>
    public RelayCommand DeselectAllCoilsCommand { get; }

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
    public AsyncRelayCommand AddHoldingRangeCommand { get; }

    /// <summary>Add a coil to the user map.</summary>
    public RelayCommand AddCoilCommand { get; }

    /// <summary>Add a coil address range to the user map.</summary>
    public AsyncRelayCommand AddCoilRangeCommand { get; }

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
        if (SelectedSlaveId is null
            || !_stores.TryGetValue(SelectedSlaveId.Value, out ModbusDataStore? store)
            || !_holdingMaps.TryGetValue(SelectedSlaveId.Value, out Dictionary<ushort, ushort>? holdings)
            || !_coilMaps.TryGetValue(SelectedSlaveId.Value, out HashSet<ushort>? coils))
        {
            HoldingRows = [];
            CoilRows = [];
            return;
        }

        List<RegisterRow> holdingRows = new(holdings.Count);
        foreach (ushort address in holdings.Keys.OrderBy(a => a))
        {
            holdingRows.Add(new RegisterRow(address, store.ReadHolding(address, 1)[0],
                (addr, val) => store.WriteHolding(addr, val)));
        }

        List<CoilRow> coilRows = new(coils.Count);
        foreach (ushort address in coils.OrderBy(a => a))
        {
            coilRows.Add(new CoilRow(address, store.ReadCoils(address, 1)[0], store.WriteCoil));
        }

        HoldingRows = new BulkObservableCollection<RegisterRow>(holdingRows);
        CoilRows = new BulkObservableCollection<CoilRow>(coilRows);
    }

    private void AddHolding()
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
            var newRow = new RegisterRow(address, value, (addr, val) => store.WriteHolding(addr, val));
            int index = FindInsertionIndex(HoldingRows, r => r.Address, address);
            HoldingRows.Insert(index, newRow);
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
        if (SelectedSlaveId is null || !_holdingMaps.TryGetValue(SelectedSlaveId.Value, out Dictionary<ushort, ushort>? map))
        {
            return;
        }

        foreach (ushort address in addresses)
        {
            map.Remove(address);
        }

        HoldingRows.RemoveRange(HoldingRows.Where(r => addresses.Contains(r.Address)));
    }

    /// <summary>Remove coils from the map (multi-select).</summary>
    public void RemoveCoils(IEnumerable<ushort> addresses)
    {
        ArgumentNullException.ThrowIfNull(addresses);
        if (SelectedSlaveId is null || !_coilMaps.TryGetValue(SelectedSlaveId.Value, out HashSet<ushort>? map))
        {
            return;
        }

        foreach (ushort address in addresses)
        {
            map.Remove(address);
        }

        CoilRows.RemoveRange(CoilRows.Where(r => addresses.Contains(r.Address)));
    }

    /// <summary>Select or clear all holding-register rows inside a deferred view refresh.</summary>
    public void SetHoldingsSelected(bool select)
    {
        ICollectionView view = CollectionViewSource.GetDefaultView(HoldingRows);
        using (view.DeferRefresh())
        {
            HoldingRows.SetAllSelected(select);
        }

        StatusText = select ? $"All {HoldingRows.Count} holding entries selected." : "Holding selection cleared.";
    }

    /// <summary>Select all holding register rows.</summary>
    public void SelectAllHoldings() => SetHoldingsSelected(true);

    /// <summary>Deselect all holding register rows.</summary>
    public void DeselectAllHoldings() => SetHoldingsSelected(false);

    /// <summary>Select or clear all coil rows inside a deferred view refresh.</summary>
    public void SetCoilsSelected(bool select)
    {
        ICollectionView view = CollectionViewSource.GetDefaultView(CoilRows);
        using (view.DeferRefresh())
        {
            CoilRows.SetAllSelected(select);
        }

        StatusText = select ? $"All {CoilRows.Count} coil entries selected." : "Coil selection cleared.";
    }

    /// <summary>Select all coil rows.</summary>
    public void SelectAllCoils() => SetCoilsSelected(true);

    /// <summary>Deselect all coil rows.</summary>
    public void DeselectAllCoils() => SetCoilsSelected(false);

    private void AddCoil()
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
            var newRow = new CoilRow(address, value, store.WriteCoil);
            int index = FindInsertionIndex(CoilRows, r => r.Address, address);
            CoilRows.Insert(index, newRow);
        }
        catch (Exception ex)
        {
            StatusText = $"Error: {ex.Message}";
        }
    }

    private async Task AddHoldingRangeAsync(CancellationToken cancellationToken)
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
        }, cancellationToken).ConfigureAwait(false);
        StatusText = $"Added range {start}-{end}.";
        store = RequireStore();
        // Rebuild HoldingRows from the updated map using BulkObservableCollection.
        var addresses = map.Keys.OrderBy(a => a).ToList();
        var rows = new List<RegisterRow>(addresses.Count);
        foreach (ushort addr in addresses)
        {
            ushort val = store.ReadHolding(addr, 1)[0];
            rows.Add(new RegisterRow(addr, val, (a, v) => store.WriteHolding(a, v)));
        }
        HoldingRows = new BulkObservableCollection<RegisterRow>(rows);
    }

    private async Task AddCoilRangeAsync(CancellationToken cancellationToken)
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
        await RunBulkAsync(start, end, a => map.Add(a), cancellationToken).ConfigureAwait(false);
        StatusText = $"Added range {start}-{end}.";
        store = RequireStore();
        // Rebuild CoilRows from the updated map using BulkObservableCollection.
        var addresses = map.OrderBy(a => a).ToList();
        var rows = new List<CoilRow>(addresses.Count);
        foreach (ushort addr in addresses)
        {
            bool val = store.ReadCoils(addr, 1)[0];
            rows.Add(new CoilRow(addr, val, store.WriteCoil));
        }
        CoilRows = new BulkObservableCollection<CoilRow>(rows);
    }

    private async Task RunBulkAsync(ushort start, ushort end, Action<ushort> work, CancellationToken ct)
    {
        IsBulkWorking = true;
        BulkProgress = 0;
        try
        {
            await Task.Run(() =>
            {
                for (int address = start; address <= end; address++)
                {
                    ct.ThrowIfCancellationRequested();
                    work((ushort)address);
                    if ((address - start) % 512 == 0)
                    {
                        BulkProgress = 100.0 * (address - start + 1) / (end - start + 1);
                    }
                }
            }, ct).ConfigureAwait(false);
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
                .OrderBy(e => e.Key)
                .Select(e => new RegisterMapStore.HoldingMapItem(e.Key, store.ReadHolding(e.Key, 1)[0]))
                .ToList();
            List<RegisterMapStore.CoilMapItem> coils = _coilMaps[SelectedSlaveId.Value]
                .OrderBy(a => a)
                .Select(a => new RegisterMapStore.CoilMapItem(a, store.ReadCoils(a, 1)[0]))
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
            address = ModbusAddress.Parse(FloatAddressText, ushort.MaxValue + 1);
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
