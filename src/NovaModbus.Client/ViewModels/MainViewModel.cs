using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.UI.Dispatching;
using NovaModbus.Client.Services;
using NovaModbus.Rtu;
using NovaModbus.Rtu.Transport;

namespace NovaModbus.Client.ViewModels;

/// <summary>Root view model: connection lifecycle + embedded virtual test slave.</summary>
public sealed partial class MainViewModel : ObservableObject, IAsyncDisposable
{
    /// <summary>Endpoint published by the embedded test slave.</summary>
    public const string TestSlaveEndpoint = "nova-test-slave";

    private readonly VirtualSerialBus _bus = VirtualSerialBus.Shared;
    private readonly DispatcherQueue _dispatcher;
    private CancellationTokenSource? _slaveCts;
    private VirtualTransport? _slaveHeldTransport;
    private bool _disposed;

    [ObservableProperty]
    public partial string StatusText { get; set; } = "Disconnected";

    [ObservableProperty]
    public partial bool TestSlaveEnabled { get; set; } = true;

    /// <summary>Initializes with the current thread's queue (construct on the UI thread).</summary>
    public MainViewModel()
        : this(DispatcherQueue.GetForCurrentThread())
    {
    }

    /// <summary>Initializes with an explicit queue.</summary>
    public MainViewModel(DispatcherQueue dispatcher)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);
        _dispatcher = dispatcher;
        SerialSettings = new SerialPortSettingsViewModel(_bus);
        Connection = new ModbusConnection(CommLog);
        ReadWrite = new ReadWriteViewModel(() => Connection.Client);
        Watch = new WatchViewModel(() => Connection.Client);
        Polling = new PollingViewModel(ReadWrite);
        Connection.ConnectionChanged += OnConnectionChanged;

        StartTestSlave();
        ConnectCommand = new AsyncRelayCommand(ConnectAsync, () => !Connection.IsConnected);
        DisconnectCommand = new AsyncRelayCommand(DisconnectAsync, () => Connection.IsConnected);
    }

    /// <summary>Serial port settings (left panel).</summary>
    public SerialPortSettingsViewModel SerialSettings { get; }

    /// <summary>Single-shot function execution tab.</summary>
    public ReadWriteViewModel ReadWrite { get; }

    /// <summary>Watch list tab.</summary>
    public WatchViewModel Watch { get; }

    /// <summary>Polling tab.</summary>
    public PollingViewModel Polling { get; }

    /// <summary>Raw traffic log.</summary>
    public ObservableLogger CommLog { get; } = new();

    /// <summary>Active connection (serial or virtual).</summary>
    public ModbusConnection Connection { get; }

    /// <summary>Whether a transport is open.</summary>
    public bool IsConnected => Connection.IsConnected;

    /// <summary>Connect with the current settings.</summary>
    public IAsyncRelayCommand ConnectCommand { get; }

    /// <summary>Close the current transport.</summary>
    public IAsyncRelayCommand DisconnectCommand { get; }

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
            await Connection.ConnectVirtualAsync(endpoint, _bus, SerialSettings.BaudRate, cancellationToken);
            StatusText = $"Connected (VIRTUAL:{endpoint} @ {SerialSettings.BaudRate}).";
            return;
        }

        SerialSettings settings = SerialSettings.BuildSettings();
        await Connection.ConnectSerialAsync(settings, cancellationToken);
        StatusText = $"Connected ({settings.PortName} @ {settings.BaudRate}).";
    }

    private async Task DisconnectAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        await Connection.DisconnectAsync();
        StatusText = "Disconnected";
    }

    private void OnConnectionChanged(object? sender, EventArgs e)
    {
        // ModbusConnection raises this on a worker thread; WinUI bindings need the UI thread.
        _dispatcher.TryEnqueue(() =>
        {
            OnPropertyChanged(nameof(IsConnected));
            ConnectCommand.NotifyCanExecuteChanged();
            DisconnectCommand.NotifyCanExecuteChanged();
        });
    }

    partial void OnTestSlaveEnabledChanged(bool value)
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

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Polling.Dispose();
        Watch.Dispose();
        StopTestSlave();
        await Connection.DisposeAsync();
        _bus.Unpublish(TestSlaveEndpoint);
        GC.SuppressFinalize(this);
    }
}
