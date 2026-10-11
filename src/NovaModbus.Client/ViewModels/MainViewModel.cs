using NovaModbus.Client.Services;
using NovaModbus.Rtu.Commands;
using NovaModbus.Rtu;
using NovaModbus.Rtu.Transport;

namespace NovaModbus.Client.ViewModels;

/// <summary>Root view model: connection lifecycle + embedded virtual test slave.</summary>
public sealed class MainViewModel : ViewModelBase, IAsyncDisposable
{
    /// <summary>Endpoint published by the embedded test slave.</summary>
    public const string TestSlaveEndpoint = "nova-test-slave";

    private readonly VirtualSerialBus _bus = VirtualSerialBus.Shared;
    private CancellationTokenSource? _slaveCts;
    private VirtualTransport? _slaveHeldTransport;
    private string _statusText = "Disconnected";
    private bool _testSlaveEnabled = true;
    private bool _disposed;

    /// <summary>Initializes settings, connection, and the embedded test slave.</summary>
    public MainViewModel()
    {
        SerialSettings = new SerialPortSettingsViewModel(_bus);
        Connection = new ModbusConnection(CommLog);
        ReadWrite = new ReadWriteViewModel(() => Connection.Client);
        Polling = new PollingViewModel(ReadWrite);
        Connection.ConnectionChanged += OnConnectionChanged;

        StartTestSlave();
        ConnectCommand = new AsyncRelayCommand(ConnectAsync, () => !Connection.IsConnected, OnError);
        DisconnectCommand = new AsyncRelayCommand(DisconnectAsync, () => Connection.IsConnected, OnError);
    }

    /// <summary>Whether the embedded test slave runs (publishes its endpoint).</summary>
    public bool TestSlaveEnabled
    {
        get => _testSlaveEnabled;
        set
        {
            if (SetProperty(ref _testSlaveEnabled, value))
            {
                if (value)
                {
                    StartTestSlave();
                }
                else
                {
                    StopTestSlave();
                }
            }
        }
    }

    /// <summary>Serial port settings (left panel).</summary>
    public SerialPortSettingsViewModel SerialSettings { get; }

    /// <summary>Single-shot function execution tab.</summary>
    public ReadWriteViewModel ReadWrite { get; }

    /// <summary>Polling tab.</summary>
    public PollingViewModel Polling { get; }

    /// <summary>Raw traffic log.</summary>
    public ObservableLogger CommLog { get; } = new();

    /// <summary>Active connection (serial or virtual).</summary>
    public ModbusConnection Connection { get; }

    /// <summary>Whether a transport is open.</summary>
    public bool IsConnected => Connection.IsConnected;

    /// <summary>Status line for the left panel.</summary>
    public string StatusText
    {
        get => _statusText;
        private set => SetProperty(ref _statusText, value);
    }

    /// <summary>Connect with the current settings.</summary>
    public AsyncRelayCommand ConnectCommand { get; }

    /// <summary>Close the current transport.</summary>
    public AsyncRelayCommand DisconnectCommand { get; }

    private void StartTestSlave()
    {
        StopTestSlave();
        _slaveCts = new CancellationTokenSource();
        var (held, published) = VirtualTransport.CreatePair();
        _slaveHeldTransport = held;
        _bus.Publish(TestSlaveEndpoint, published);
        ModbusRtuSlave slave = new(held);
        slave.Engine.AddSlave(1);
        _ = new SlaveServer(slave).RunAsync(_slaveCts.Token);
        SerialSettings.RefreshPorts();
    }

    private void StopTestSlave()
    {
        try
        {
            _slaveCts?.Cancel();
        }
        catch (ObjectDisposedException)
        {
            // Already torn down.
        }

        _slaveCts?.Dispose();
        _slaveCts = null;
        _bus.Unpublish(TestSlaveEndpoint);
        if (_slaveHeldTransport is not null)
        {
            // VirtualTransport disposal is fully synchronous (no awaits inside).
            _slaveHeldTransport.DisposeAsync().AsTask().GetAwaiter().GetResult();
            _slaveHeldTransport = null;
        }

        SerialSettings.RefreshPorts();
    }

    private async Task ConnectAsync(CancellationToken cancellationToken)
    {
        if (!SerialSettings.IsBaudRateValid)
        {
            StatusText = $"Invalid baud rate: '{SerialSettings.BaudRateText}'.";
            return;
        }

        if (SerialSettings.IsVirtualSelected)
        {
            string endpoint = SerialSettings.VirtualEndpointName ?? string.Empty;
            await Connection.ConnectVirtualAsync(endpoint, _bus, SerialSettings.BaudRate, cancellationToken).ConfigureAwait(false);
            StatusText = $"Connected (VIRTUAL:{endpoint} @ {SerialSettings.BaudRate}).";
            return;
        }

        SerialSettings settings = SerialSettings.BuildSettings();
        await Connection.ConnectSerialAsync(settings, cancellationToken).ConfigureAwait(false);
        StatusText = $"Connected ({settings.PortName} @ {settings.BaudRate}).";
    }

    private async Task DisconnectAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Connection.DisconnectAsync().ConfigureAwait(false);
        StatusText = "Disconnected";
    }

    private void OnConnectionChanged(object? sender, EventArgs e)
    {
        NotifyPropertyChanged(nameof(IsConnected));
        ConnectCommand.RaiseCanExecuteChanged();
        DisconnectCommand.RaiseCanExecuteChanged();
    }

    private void OnError(Exception ex) => StatusText = $"Error: {ex.Message}";

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Polling.Dispose();
        StopTestSlave();
        await Connection.DisposeAsync().ConfigureAwait(false);
        _bus.Unpublish(TestSlaveEndpoint);
        GC.SuppressFinalize(this);
    }
}
