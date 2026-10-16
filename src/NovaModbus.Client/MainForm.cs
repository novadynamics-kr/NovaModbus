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
    private readonly WatchList _watch;
    private readonly PollingRunner _polling;
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
        _watch = new WatchList(() => _connection.Client);
        _polling = new PollingRunner(_runner);
        _connection.ConnectionChanged += OnConnectionChanged;
        _watch.StatusChanged += OnWatchStatus;
        _polling.IsPollingChanged += OnPollingChanged;
        FillSettings();
        FillReadWrite();
        FillWatch();
        StartTestSlave();
        RefreshConnectButtons();
        RefreshPollingButtons();
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

    private void OnFunctionChanged(object? sender, EventArgs e)    {
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
        _polling.Dispose();
        _watch.Dispose();
        StopTestSlave();
        await _connection.DisposeAsync();
        _bus.Unpublish(TestSlaveEndpoint);
    }

    private void OnWatchStatus(object? sender, EventArgs e)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new EventHandler(OnWatchStatus), sender, e);
            return;
        }

        _watchStatusLabel.Text = _watch.StatusText;
    }

    private void FillWatch()
    {
        _areaBox.Items.Clear();
        _areaBox.Items.AddRange(_watch.Areas.Cast<object>().ToArray());
        _areaBox.SelectedItem = _watch.Area;
        _pollingHistoryBox.DataSource = _polling.History;
        _logBox.DataSource = _log.Lines;
        _watchStatusLabel.Text = _watch.StatusText;
        SyncRowCounts();
    }

    /// <summary>Push list counts into the virtual grids (O(1), no per-row work).</summary>
    private void SyncRowCounts()
    {
        bool outer = _syncingGrids;
        _syncingGrids = true;
        _entriesGrid.SuspendLayout();
        _resultsGrid.SuspendLayout();
        try
        {
            _entriesGrid.RowCount = _watch.Entries.Count;
            _resultsGrid.RowCount = _watch.Results.Count;
        }
        finally
        {
            _entriesGrid.ResumeLayout();
            _resultsGrid.ResumeLayout();
            _syncingGrids = outer;
        }
    }

    private bool _syncingGrids;

    /// <summary>Live mirror threshold: above this, mirroring is skipped (delete uses union).</summary>
    private const int MirrorSelectionThreshold = 2000;

    private void OnEntriesSelectionChanged(object? sender, EventArgs e)
    {
        MirrorSelection(_entriesGrid, _resultsGrid);
    }

    private void OnResultsSelectionChanged(object? sender, EventArgs e)
    {
        MirrorSelection(_resultsGrid, _entriesGrid);
    }

    private void MirrorSelection(DataGridView source, DataGridView target)
    {
        if (_syncingGrids)
        {
            return;
        }

        int selected = source.SelectedRows.Count;
        if (selected == 0)
        {
            // Deselection is cheap: single clear, no per-row work.
            _syncingGrids = true;
            try
            {
                target.ClearSelection();
            }
            finally
            {
                _syncingGrids = false;
            }

            return;
        }

        // Fast path: all rows selected -> single native call, no per-row loop.
        if (selected == source.RowCount && target.RowCount == source.RowCount)
        {
            _syncingGrids = true;
            try
            {
                target.SelectAll();
            }
            finally
            {
                _syncingGrids = false;
            }

            return;
        }

        // Huge partial selections: skip live mirroring to avoid O(n) per-row
        // assignments on every SelectionChanged (drag would go O(n^2)).
        // Correctness is preserved: delete collects the union of both grids.
        if (selected > MirrorSelectionThreshold)
        {
            return;
        }

        _syncingGrids = true;
        source.SuspendLayout();
        target.SuspendLayout();
        try
        {
            target.ClearSelection();
            foreach (DataGridViewRow row in source.SelectedRows)
            {
                if (row.Index < target.RowCount)
                {
                    target.Rows[row.Index].Selected = true;
                }
            }
        }
        finally
        {
            target.ResumeLayout();
            source.ResumeLayout();
            _syncingGrids = false;
        }
    }

    private void SyncWatchFromUi()
    {
        _watch.SlaveIdText = _watchSlaveBox.Text;
        _watch.Area = _areaBox.SelectedItem is WatchArea area ? area : WatchArea.HoldingRegisters;
        _watch.StartText = _watchStartBox.Text;
        _watch.AddressEndText = _watchEndBox.Text;
        _watch.CountText = _watchCountBox.Text;
        _watch.ExtraText = _watchExtraBox.Text;
        _watch.AutoIntervalMsText = _watchIntervalBox.Text;
    }

    private void OnWatchAdd(object? sender, EventArgs e)
    {
        SyncWatchFromUi();
        _watch.AddEntry();
        SyncRowCounts();
    }

    private async void OnWatchAddRange(object? sender, EventArgs e)
    {
        SyncWatchFromUi();
        _bulkProgressBar.Value = 0;
        using var timer = new System.Windows.Forms.Timer { Interval = 200 };
        timer.Tick += (_, _) => _bulkProgressBar.Value = Math.Clamp((int)_watch.BulkProgress, 0, 100);
        timer.Start();
        try
        {
            await _watch.AddRangeAsync();
        }
        finally
        {
            timer.Stop();
            _bulkProgressBar.Value = 0;
            SyncRowCounts();
        }
    }

    private void OnWatchDelete(object? sender, EventArgs e)
    {
        if (_watch.Entries.Count == 0)
        {
            return;
        }

        // Suppress live mirroring for the whole operation: RowCount changes and
        // ClearSelection would otherwise each trigger a mirrored per-row pass.
        _syncingGrids = true;
        _entriesGrid.SuspendLayout();
        _resultsGrid.SuspendLayout();
        try
        {
            int total = _watch.Entries.Count;
            int selectedEntries = _entriesGrid.SelectedRows.Count;
            int selectedResults = _resultsGrid.SelectedRows.Count;
            if (selectedEntries == 0 && selectedResults == 0)
            {
                return;
            }

            // Fast path: everything selected -> single clear, no enumeration.
            if (selectedEntries >= total || selectedResults >= total)
            {
                _entriesGrid.ClearSelection();
                _resultsGrid.ClearSelection();
                _watch.ClearAll();
                SyncRowCounts();
                return;
            }

            HashSet<int> doomed = new(selectedEntries + selectedResults);
            foreach (DataGridViewRow row in _entriesGrid.SelectedRows)
            {
                if (row.Index < total)
                {
                    doomed.Add(row.Index);
                }
            }

            int resultTotal = _watch.Results.Count;
            foreach (DataGridViewRow row in _resultsGrid.SelectedRows)
            {
                if (row.Index < resultTotal)
                {
                    doomed.Add(row.Index);
                }
            }

            if (doomed.Count == 0)
            {
                return;
            }

            // Deselect BEFORE shrinking RowCount so no selection-cascade fires.
            _entriesGrid.ClearSelection();
            _resultsGrid.ClearSelection();
            _watch.RemoveAtIndices(doomed);
            SyncRowCounts();
        }
        finally
        {
            _resultsGrid.ResumeLayout();
            _entriesGrid.ResumeLayout();
            _syncingGrids = false;
            _entriesGrid.Invalidate();
            _resultsGrid.Invalidate();
        }
    }

    private void OnWatchClearAll(object? sender, EventArgs e)
    {
        _syncingGrids = true;
        _entriesGrid.SuspendLayout();
        _resultsGrid.SuspendLayout();
        try
        {
            _entriesGrid.ClearSelection();
            _resultsGrid.ClearSelection();
            _watch.ClearAll();
            SyncRowCounts();
        }
        finally
        {
            _resultsGrid.ResumeLayout();
            _entriesGrid.ResumeLayout();
            _syncingGrids = false;
            _entriesGrid.Invalidate();
            _resultsGrid.Invalidate();
        }
    }

    private void OnWatchSelectAll(object? sender, EventArgs e)
    {
        // Native grid selection only (no checkbox/IsSelected state).
        _syncingGrids = true;
        try
        {
            _entriesGrid.SelectAll();
            _resultsGrid.SelectAll();
            _watchStatusLabel.Text = $"All {_watch.Entries.Count} entries selected.";
        }
        finally
        {
            _syncingGrids = false;
        }
    }

    private void OnWatchDeselectAll(object? sender, EventArgs e)
    {
        // Native grid deselection only (no checkbox/IsSelected state).
        _syncingGrids = true;
        try
        {
            _entriesGrid.ClearSelection();
            _resultsGrid.ClearSelection();
            _watchStatusLabel.Text = "Selection cleared.";
        }
        finally
        {
            _syncingGrids = false;
        }
    }

    private void OnWatchSave(object? sender, EventArgs e)
    {
        using SaveFileDialog dialog = new()
        {
            Filter = "Watch list (*.json)|*.json",
            FileName = "watchlist.json",
        };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _watch.SaveWatchTo(dialog.FileName);
        }
    }

    private void OnWatchLoad(object? sender, EventArgs e)
    {
        using OpenFileDialog dialog = new()
        {
            Filter = "Watch list (*.json)|*.json",
        };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _syncingGrids = true;
            try
            {
                _entriesGrid.ClearSelection();
                _resultsGrid.ClearSelection();
                _watch.LoadWatchFrom(dialog.FileName);
                SyncRowCounts();
            }
            finally
            {
                _syncingGrids = false;
            }
        }
    }

    private async void OnWatchReadAll(object? sender, EventArgs e)
    {
        SyncWatchFromUi();
        _readAllButton.Enabled = false;
        try
        {
            await _watch.ReadAllAsync();
        }
        finally
        {
            _readAllButton.Enabled = true;
            _resultsGrid.Invalidate();
        }
    }

    private void OnWatchAutoToggled(object? sender, EventArgs e)
    {
        SyncWatchFromUi();
        _watch.AutoRefresh = _autoRefreshCheck.Checked;
    }

    private void OnWatchGridKey(object? sender, KeyEventArgs e)
    {
        if (e.Control && e.KeyCode == Keys.A)
        {
            OnWatchSelectAll(sender, e);
            e.SuppressKeyPress = true;
        }
        else if (e.KeyCode == Keys.Delete)
        {
            OnWatchDelete(sender, e);
            e.SuppressKeyPress = true;
        }
        else if (e.KeyCode == Keys.Escape)
        {
            OnWatchDeselectAll(sender, e);
            e.SuppressKeyPress = true;
        }
    }

    private void OnEntriesCellValue(object? sender, DataGridViewCellValueEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= _watch.Entries.Count)
        {
            return;
        }

        WatchEntry entry = _watch.Entries[e.RowIndex];
        // Strings only: typed values through a text column can raise
        // FormatException on some cultures/column states.
        e.Value = e.ColumnIndex switch
        {
            0 => entry.SlaveId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            1 => entry.Area.ToString(),
            2 => entry.Start.ToString(System.Globalization.CultureInfo.InvariantCulture),
            3 => entry.AddressHex,
            4 => entry.Count.ToString(System.Globalization.CultureInfo.InvariantCulture),
            5 => entry.Extra,
            _ => null,
        };
    }

    private void OnResultsCellValue(object? sender, DataGridViewCellValueEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= _watch.Results.Count)
        {
            return;
        }

        WatchResult row = _watch.Results[e.RowIndex];
        e.Value = e.ColumnIndex switch
        {
            0 => row.Entry.SlaveId.ToString(System.Globalization.CultureInfo.InvariantCulture),
            1 => row.Entry.Area.ToString(),
            2 => row.Entry.Start.ToString(System.Globalization.CultureInfo.InvariantCulture),
            3 => row.ValueDecText,
            4 => row.ValueHexText,
            5 => row.Timestamp,
            6 => row.Status,
            _ => null,
        };
    }

    private async void OnPollingStart(object? sender, EventArgs e)
    {
        _polling.IntervalMsText = _pollingIntervalBox.Text;
        _pollingStartButton.Enabled = false;
        try
        {
            await _polling.StartAsync();
        }
        finally
        {
            RefreshPollingButtons();
        }
    }

    private void OnPollingStop(object? sender, EventArgs e) => _polling.Stop();

    private void OnPollingChanged(object? sender, EventArgs e)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new EventHandler(OnPollingChanged), sender, e);
            return;
        }

        RefreshPollingButtons();
    }

    private void RefreshPollingButtons()
    {
        _pollingStartButton.Enabled = !_polling.IsPolling;
        _pollingStopButton.Enabled = _polling.IsPolling;
    }
}
