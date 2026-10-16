using NovaModbus.Client.Services;
using NovaModbus.Client.ViewModels;
using NovaModbus.Rtu;
using NovaModbus.Rtu.Transport;

namespace NovaModbus.Client;

/// <summary>Main window: serial settings + function tabs.</summary>
public partial class MainForm : Form
{
    /// <summary>Endpoint published by the embedded test slave.</summary>
    public const string TestSlaveEndpoint = "nova-test-slave";

    private readonly VirtualSerialBus _bus = VirtualSerialBus.Shared;
    private readonly SerialPortOptions _options;
    private readonly ModbusConnection _connection;
    private readonly ReadWriteRunner _runner;
    private readonly WinFormsLogger _log;
    private CancellationTokenSource? _slaveCts;
    private VirtualTransport? _slaveHeldTransport;
    private bool _disposed;
    private bool _busy;

    /// <summary>Initializes the main form.</summary>
    public MainForm()
    {
        InitializeComponent();
        _log = new WinFormsLogger(this);
        _options = new SerialPortOptions(_bus);
        _connection = new ModbusConnection(_log);
        _runner = new ReadWriteRunner(() => _connection.Client);
        _connection.ConnectionChanged += OnConnectionChanged;
        FillSettings();
        FillReadWrite();
        StartTestSlave();
        RefreshConnectButtons();
        FormClosing += OnFormClosing;
    }

    private void FillReadWrite()
    {
        _functionBox.Items.Clear();
        _functionBox.Items.AddRange(_runner.AvailableFunctions.Cast<object>().ToArray());
        _functionBox.SelectedItem = _runner.SelectedFunction;
        _hintLabel.Text = _runner.HintText;
        _countBox.Enabled = _runner.NeedsCount;
        _valuesBox.Enabled = _runner.NeedsValues;
    }

    private void FillSettings()
    {
        _portBox.Items.Clear();
        _portBox.Items.AddRange(_options.AvailablePorts.ToArray());
        _portBox.SelectedItem = _options.SelectedPort;
        _baudBox.Items.Clear();
        _baudBox.Items.AddRange(_options.AvailableBaudRates.ToArray());
        _baudBox.Text = _options.BaudRateText;
        _dataBitsBox.Items.Clear();
        _dataBitsBox.Items.AddRange(_options.AvailableDataBits.Cast<object>().ToArray());
        _dataBitsBox.SelectedItem = _options.DataBits;
        _parityBox.Items.Clear();
        _parityBox.Items.AddRange(_options.AvailableParity.ToArray());
        _parityBox.SelectedItem = _options.Parity;
        _stopBitsBox.Items.Clear();
        _stopBitsBox.Items.AddRange(_options.AvailableStopBits.ToArray());
        _stopBitsBox.SelectedItem = _options.StopBits;
        _portStatusLabel.Text = _options.StatusMessage;
    }

    private void SyncOptionsFromUi()
    {
        _options.SelectedPort = (string?)_portBox.SelectedItem;
        _options.BaudRateText = _baudBox.Text;
        _options.DataBits = _dataBitsBox.SelectedItem is int bits ? bits : 8;
        _options.Parity = (string?)_parityBox.SelectedItem ?? "Even";
        _options.StopBits = (string?)_stopBitsBox.SelectedItem ?? "One";
    }

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
        SyncOptionsFromUi();
        _options.RefreshPorts();
        FillSettings();
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
    }

    private async void OnRefreshPorts(object? sender, EventArgs e)
    {
        SyncOptionsFromUi();
        await Task.Run(() => _options.RefreshPorts());
        FillSettings();
    }

    private async void OnConnect(object? sender, EventArgs e)
    {
        SyncOptionsFromUi();
        if (!_options.IsBaudRateValid)
        {
            SetStatus($"Invalid baud rate: '{_options.BaudRateText}'.");
            return;
        }

        _connectButton.Enabled = false;
        try
        {
            if (_options.IsVirtualSelected)
            {
                string endpoint = _options.VirtualEndpointName ?? string.Empty;
                await _connection.ConnectVirtualAsync(endpoint, _bus, _options.BaudRate);
                SetStatus($"Connected (VIRTUAL:{endpoint} @ {_options.BaudRate}).");
                return;
            }

            SerialSettings settings = _options.BuildSettings();
            await _connection.ConnectSerialAsync(settings);
            SetStatus($"Connected ({settings.PortName} @ {settings.BaudRate}).");
        }
        catch (Exception ex)
        {
            SetStatus($"Error: {ex.Message}");
        }
        finally
        {
            RefreshConnectButtons();
        }
    }

    private async void OnDisconnect(object? sender, EventArgs e)
    {
        _disconnectButton.Enabled = false;
        try
        {
            await _connection.DisconnectAsync();
            SetStatus("Disconnected");
        }
        catch (Exception ex)
        {
            SetStatus($"Error: {ex.Message}");
        }
        finally
        {
            RefreshConnectButtons();
        }
    }

    private void OnConnectionChanged(object? sender, EventArgs e)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new EventHandler(OnConnectionChanged), sender, e);
            return;
        }

        RefreshConnectButtons();
    }

    private void RefreshConnectButtons()
    {
        _connectButton.Enabled = !_connection.IsConnected;
        _disconnectButton.Enabled = _connection.IsConnected;
    }

    private void SetStatus(string text)
    {
        _statusLabel.Text = text;
        _connectionStatusLabel.Text = text;
    }

    private void OnTestSlaveToggled(object? sender, EventArgs e)
    {
        if (_testSlaveCheck.Checked)
        {
            StartTestSlave();
        }
        else
        {
            StopTestSlave();
            SyncOptionsFromUi();
            _options.RefreshPorts();
            FillSettings();
        }
    }

    private void OnFunctionChanged(object? sender, EventArgs e)
    {
        if (_functionBox.SelectedItem is NovaModbus.Rtu.Enums.ModbusFunction function)
        {
            _runner.SelectedFunction = function;
            _hintLabel.Text = _runner.HintText;
            _countBox.Enabled = _runner.NeedsCount;
            _valuesBox.Enabled = _runner.NeedsValues;
        }
    }

    private async void OnExecute(object? sender, EventArgs e)
    {
        if (_busy)
        {
            return;
        }

        _busy = true;
        _executeButton.Enabled = false;
        try
        {
            _runner.SelectedFunction = _functionBox.SelectedItem is NovaModbus.Rtu.Enums.ModbusFunction f
                ? f
                : NovaModbus.Rtu.Enums.ModbusFunction.ReadHoldingRegisters;
            _runner.SlaveIdText = _slaveBox.Text;
            _runner.StartText = _startBox.Text;
            _runner.CountText = _countBox.Text;
            _runner.ValuesText = _valuesBox.Text;
            _resultBox.Text = await _runner.RunOnceAsync();
        }
        finally
        {
            _busy = false;
            _executeButton.Enabled = true;
        }
    }

    private async void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        StopTestSlave();
        await _connection.DisposeAsync();
        _bus.Unpublish(TestSlaveEndpoint);
    }
}
