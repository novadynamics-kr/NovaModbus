using System.Collections.ObjectModel;
using System.Globalization;
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
    private readonly DispatcherTimer _counterTimer;
    private ModbusRtuSlave? _slave;
    private SlaveServer? _server;
    private CancellationTokenSource? _serveCts;
    private string _statusText = "Stopped";
    private string _countersText = string.Empty;
    private string _newSlaveIdText = "1";
    private byte? _selectedSlaveId;
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

    /// <summary>Supported float word orders (Endian display).</summary>
    public IReadOnlyList<FloatOrderOption> FloatOrderOptions { get; } =
    [
        new(FloatWordOrder.Abcd, FloatWordOrderText.ToDisplayString(FloatWordOrder.Abcd)),
        new(FloatWordOrder.Badc, FloatWordOrderText.ToDisplayString(FloatWordOrder.Badc)),
        new(FloatWordOrder.Cdab, FloatWordOrderText.ToDisplayString(FloatWordOrder.Cdab)),
        new(FloatWordOrder.Dcba, FloatWordOrderText.ToDisplayString(FloatWordOrder.Dcba)),
    ];

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
        if (SelectedSlaveId is null || !_stores.TryGetValue(SelectedSlaveId.Value, out ModbusDataStore? store))
        {
            return;
        }

        for (int i = 0; i < store.HoldingCount; i++)
        {
            ushort address = (ushort)i;
            HoldingRows.Add(new RegisterRow(address, store.ReadHolding(address, 1)[0],
                (addr, val) => store.WriteHolding(addr, val)));
        }

        foreach (bool coil in store.ReadCoils(0, (ushort)store.CoilCount))
        {
            ushort address = (ushort)CoilRows.Count;
            CoilRows.Add(new CoilRow(address, coil, store.WriteCoil));
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
        try
        {
            if (SelectedSlaveId is null || !_stores.TryGetValue(SelectedSlaveId.Value, out ModbusDataStore? store))
            {
                StatusText = "Select a slave ID.";
                return;
            }

            if (!ushort.TryParse(FloatAddressText, out ushort address) || address + 1 >= store.HoldingCount)
            {
                StatusText = $"Invalid address: '{FloatAddressText}' (needs 2 registers).";
                return;
            }

            if (!float.TryParse(FloatValueText, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
            {
                StatusText = $"Invalid float: '{FloatValueText}'.";
                return;
            }

            ushort[] regs = Float32Codec.Encode(value, FloatOrder.Order);
            store.WriteHolding(address, regs);
            StatusText = $"Wrote {value} as 0x{regs[0]:X4} 0x{regs[1]:X4} @ {address} ({FloatOrder.Display}).";
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
