using NovaModbus.Client.Services;
using NovaModbus.Rtu;
using NovaModbus.Rtu.Transport;

namespace NovaModbus.Client.ViewModels;

/// <summary>Root view model: connection lifecycle + embedded virtual test slave.</summary>
public sealed class MainViewModel : ViewModelBase, IAsyncDisposable
{
    /// <summary>Endpoint published by the embedded test slave.</summary>
    public const string TestSlaveEndpoint = "nova-test-slave";

    private readonly VirtualSerialBus _bus = VirtualSerialBus.Shared;
    private readonly CancellationTokenSource _lifetime = new();
    private readonly SlaveServer _testSlaveServer;
    private string _statusText = "Disconnected";
    private bool _disposed;

    /// <summary>Initializes settings, connection, and the embedded test slave.</summary>
    public MainViewModel()
    {
        SerialSettings = new SerialPortSettingsViewModel(_bus);
        Connection = new ModbusConnection(CommLog);
        ReadWrite = new ReadWriteViewModel(() => Connection.Client);
        Polling = new PollingViewModel(ReadWrite);
        Connection.ConnectionChanged += OnConnectionChanged;

        var (held, published) = VirtualTransport.CreatePair();
        _bus.Publish(TestSlaveEndpoint, published);
        ModbusRtuSlave slave = new(held);
        slave.Engine.AddSlave(1);
        _testSlaveServer = new SlaveServer(slave);
        _ = _testSlaveServer.RunAsync(_lifetime.Token);

        SerialSettings.RefreshPorts();
        ConnectCommand = new AsyncRelayCommand(ConnectAsync, () => !Connection.IsConnected, OnError);
        DisconnectCommand = new AsyncRelayCommand(DisconnectAsync, () => Connection.IsConnected, OnError);
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
        await Connection.DisposeAsync().ConfigureAwait(false);
        await _lifetime.CancelAsync().ConfigureAwait(false);
        _bus.Unpublish(TestSlaveEndpoint);
        _lifetime.Dispose();
        GC.SuppressFinalize(this);
    }
}
