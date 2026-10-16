using NovaModbus.Rtu;
using NovaModbus.Rtu.Transport;
using NovaModbus.Slave.Services;
using NovaModbus.Slave.ViewModels;

namespace NovaModbus.Slave;

/// <summary>Main window: serial listener + slave editors.</summary>
public partial class MainForm : Form
{
    private readonly SlavePortOptions _options = new();
    private readonly WinFormsLogger _log;
    private readonly SlaveStore _store;
    private ModbusRtuSlave? _slave;
    private SlaveServer? _server;
    private CancellationTokenSource? _serveCts;
    private readonly System.Windows.Forms.Timer _counterTimer;
    private bool _disposed;

    /// <summary>Initializes the main form.</summary>
    public MainForm()
    {
        InitializeComponent();
        _log = new WinFormsLogger(this);
        _store = new SlaveStore(() => _slave);
        _store.StatusChanged += OnStoreStatus;
        FillSettings();
        RefreshSlaveIds();
        _store.RefreshRows();
        SyncHoldingRows();
        SyncCoilRows();
        FillExtras();
        RefreshStartStopButtons();
        _counterTimer = new System.Windows.Forms.Timer { Interval = 1000 };
        _counterTimer.Tick += OnCounterTick;
        _counterTimer.Start();
        FormClosing += OnFormClosing;
    }

    private void FillExtras()
    {
        _logBox.DataSource = _log.Lines;
        _floatOrderBox.Items.Clear();
        _floatOrderBox.Items.AddRange(_store.FloatOrderOptions.Cast<object>().ToArray());
        _floatOrderBox.SelectedItem = _store.FloatOrder;
    }

    private void OnCounterTick(object? sender, EventArgs e)
    {
        _countersBox.Text = _store.BuildCountersText();
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

    private void OnRefreshPorts(object? sender, EventArgs e)
    {
        SyncOptionsFromUi();
        _options.RefreshPorts();
        FillSettings();
    }

    private async void OnStart(object? sender, EventArgs e)
    {
        SyncOptionsFromUi();
        if (_slave is not null)
        {
            return;
        }

        SerialSettings settings;
        try
        {
            settings = _options.BuildSettings();
        }
        catch (Exception ex)
        {
            SetStatus($"Error: {ex.Message}");
            return;
        }

        _startButton.Enabled = false;
        try
        {
            ISerialTransport transport = await TransportFactory.OpenSerialAsync(settings);
            try
            {
                ModbusRtuSlave slave = new(transport, _log)
                {
                    BaudRate = settings.BaudRate,
                    IgnoreCrcErrors = _ignoreCrcCheck.Checked,
                    SimulateBusy = _busyCheck.Checked,
                };
                foreach (byte id in _store.SlaveIds)
                {
                    ModbusDataStore? store = _store.GetStore(id);
                    if (store is not null)
                    {
                        slave.Engine.AddSlave(id, store);
                    }
                }

                _slave = slave;
                _server = new SlaveServer(slave);
                _serveCts = new CancellationTokenSource();
                _ = _server.RunAsync(_serveCts.Token);
                _store.SetRunningStatus($"Listening on {settings.PortName} @ {settings.BaudRate}.");
            }
            catch
            {
                await transport.DisposeAsync();
                throw;
            }
        }
        catch (Exception ex)
        {
            SetStatus($"Error: {ex.Message}");
        }
        finally
        {
            RefreshStartStopButtons();
        }
    }

    private async void OnStop(object? sender, EventArgs e)
    {
        _stopButton.Enabled = false;
        try
        {
            await StopListenerAsync();
            _store.SetRunningStatus("Stopped");
        }
        catch (Exception ex)
        {
            SetStatus($"Error: {ex.Message}");
        }
        finally
        {
            RefreshStartStopButtons();
        }
    }

    private async Task StopListenerAsync()
    {
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
            await _slave.DisposeAsync();
            _slave = null;
        }

        _server = null;
    }

    private void RefreshStartStopButtons()
    {
        _startButton.Enabled = _slave is null;
        _stopButton.Enabled = _slave is not null;
    }

    private void SetStatus(string text)
    {
        _statusLabel.Text = text;
        _listenerStatusLabel.Text = text;
    }

    private void OnStoreStatus(object? sender, EventArgs e)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new EventHandler(OnStoreStatus), sender, e);
            return;
        }

        _listenerStatusLabel.Text = _store.StatusText;
        _holdingStatusLabel.Text = _store.StatusText;
        _coilStatusLabel.Text = _store.StatusText;
    }

    private void OnErrorInjectionToggled(object? sender, EventArgs e)
    {
        if (_slave is null)
        {
            return;
        }

        _slave.IgnoreCrcErrors = _ignoreCrcCheck.Checked;
        _slave.SimulateBusy = _busyCheck.Checked;
    }

    private void RefreshSlaveIds()
    {
        _slaveIdBox.Items.Clear();
        _slaveIdBox.Items.AddRange(_store.SlaveIds.Cast<object>().ToArray());
        _slaveIdBox.SelectedItem = _store.SelectedSlaveId;
        _removeSlaveButton.Enabled = _store.SelectedSlaveId is not null;
    }

    private void OnAddSlave(object? sender, EventArgs e)
    {
        _store.NewSlaveIdText = _newSlaveIdBox.Text;
        _store.AddSlave();
        RefreshSlaveIds();
    }

    private void OnRemoveSlave(object? sender, EventArgs e)
    {
        _store.RemoveSelectedSlave();
        RefreshSlaveIds();
        _store.RefreshRows();
        SyncHoldingRows();
        SyncCoilRows();
    }

    private void OnSlaveIdSelected(object? sender, EventArgs e)
    {
        _store.SelectedSlaveId = _slaveIdBox.SelectedItem is byte id ? id : null;
        _removeSlaveButton.Enabled = _store.SelectedSlaveId is not null;
        _store.RefreshRows();
        SyncHoldingRows();
        SyncCoilRows();
    }

    private void SyncWatchFromUi()
    {
        _store.HoldingAddressText = _holdingAddressBox.Text;
        _store.HoldingValueText = _holdingValueBox.Text;
        _store.HoldingEndText = _holdingEndBox.Text;
        _store.CoilAddressText = _coilAddressBox.Text;
        _store.CoilEndText = _coilEndBox.Text;
    }

    /// <summary>Push holding row count into the virtual grid (O(1), no per-row work).</summary>
    private void SyncHoldingRows()
    {
        _holdingGrid.SuspendLayout();
        try
        {
            _holdingGrid.RowCount = _store.HoldingRows.Count;
        }
        finally
        {
            _holdingGrid.ResumeLayout();
        }
    }

    /// <summary>Push coil row count into the virtual grid (O(1), no per-row work).</summary>
    private void SyncCoilRows()
    {
        _coilGrid.SuspendLayout();
        try
        {
            _coilGrid.RowCount = _store.CoilRows.Count;
        }
        finally
        {
            _coilGrid.ResumeLayout();
        }
    }

    private void OnAddHolding(object? sender, EventArgs e)
    {
        SyncWatchFromUi();
        _store.AddHolding();
        SyncHoldingRows();
    }

    private void OnAddCoil(object? sender, EventArgs e)
    {
        SyncWatchFromUi();
        _store.AddCoil();
        SyncCoilRows();
    }

    private static HashSet<int> CollectSelected(DataGridView grid, int total)
    {
        HashSet<int> indices = new(grid.SelectedRows.Count);
        foreach (DataGridViewRow row in grid.SelectedRows)
        {
            if (row.Index < total)
            {
                indices.Add(row.Index);
            }
        }

        return indices;
    }

    private void OnDeleteHolding(object? sender, EventArgs e)
    {
        if (_store.HoldingRows.Count == 0)
        {
            return;
        }

        _holdingGrid.SuspendLayout();
        try
        {
            // Fast path: everything selected -> no enumeration.
            if (_holdingGrid.Rows.GetRowCount(DataGridViewElementStates.Selected) >= _store.HoldingRows.Count)
            {
                _holdingGrid.ClearSelection();
                _store.RemoveHoldingsAt(AllIndices(_store.HoldingRows.Count));
            }
            else
            {
                HashSet<int> doomed = CollectSelected(_holdingGrid, _store.HoldingRows.Count);
                if (doomed.Count == 0)
                {
                    return;
                }

                // Deselect BEFORE shrinking RowCount so no selection cascade fires.
                _holdingGrid.ClearSelection();
                _store.RemoveHoldingsAt(doomed);
            }

            SyncHoldingRows();
        }
        finally
        {
            _holdingGrid.ResumeLayout();
            _holdingGrid.Invalidate();
        }
    }

    private void OnDeleteCoil(object? sender, EventArgs e)
    {
        if (_store.CoilRows.Count == 0)
        {
            return;
        }

        _coilGrid.SuspendLayout();
        try
        {
            if (_coilGrid.Rows.GetRowCount(DataGridViewElementStates.Selected) >= _store.CoilRows.Count)
            {
                _coilGrid.ClearSelection();
                _store.RemoveCoilsAt(AllIndices(_store.CoilRows.Count));
            }
            else
            {
                HashSet<int> doomed = CollectSelected(_coilGrid, _store.CoilRows.Count);
                if (doomed.Count == 0)
                {
                    return;
                }

                _coilGrid.ClearSelection();
                _store.RemoveCoilsAt(doomed);
            }

            SyncCoilRows();
        }
        finally
        {
            _coilGrid.ResumeLayout();
            _coilGrid.Invalidate();
        }
    }

    private static HashSet<int> AllIndices(int count)
    {
        HashSet<int> indices = new(count);
        for (int i = 0; i < count; i++)
        {
            indices.Add(i);
        }

        return indices;
    }

    private void OnSelectAllHolding(object? sender, EventArgs e)
    {
        _holdingGrid.SelectAll();
        _holdingStatusLabel.Text = $"All {_store.HoldingRows.Count} holding entries selected.";
    }

    private void OnDeselectAllHolding(object? sender, EventArgs e)
    {
        _holdingGrid.ClearSelection();
        _holdingStatusLabel.Text = "Holding selection cleared.";
    }

    private void OnSelectAllCoils(object? sender, EventArgs e)
    {
        _coilGrid.SelectAll();
        _coilStatusLabel.Text = $"All {_store.CoilRows.Count} coil entries selected.";
    }

    private void OnDeselectAllCoils(object? sender, EventArgs e)
    {
        _coilGrid.ClearSelection();
        _coilStatusLabel.Text = "Coil selection cleared.";
    }

    private void OnHoldingGridKey(object? sender, KeyEventArgs e)
    {
        if (e.Control && e.KeyCode == Keys.A)
        {
            OnSelectAllHolding(sender, e);
            e.SuppressKeyPress = true;
        }
        else if (e.KeyCode == Keys.Delete)
        {
            OnDeleteHolding(sender, e);
            e.SuppressKeyPress = true;
        }
        else if (e.KeyCode == Keys.Escape)
        {
            OnDeselectAllHolding(sender, e);
            e.SuppressKeyPress = true;
        }
    }

    private void OnCoilGridKey(object? sender, KeyEventArgs e)
    {
        if (e.Control && e.KeyCode == Keys.A)
        {
            OnSelectAllCoils(sender, e);
            e.SuppressKeyPress = true;
        }
        else if (e.KeyCode == Keys.Delete)
        {
            OnDeleteCoil(sender, e);
            e.SuppressKeyPress = true;
        }
        else if (e.KeyCode == Keys.Escape)
        {
            OnDeselectAllCoils(sender, e);
            e.SuppressKeyPress = true;
        }
    }

    private void OnHoldingCellValue(object? sender, DataGridViewCellValueEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= _store.HoldingRows.Count)
        {
            return;
        }

        RegisterRow row = _store.HoldingRows[e.RowIndex];
        // Strings only: typed values through a text column can raise FormatException.
        e.Value = e.ColumnIndex switch
        {
            0 => row.Address.ToString(System.Globalization.CultureInfo.InvariantCulture),
            1 => row.AddressHex,
            2 => row.ValueText,
            3 => row.HexText,
            _ => null,
        };
    }

    private void OnHoldingCellPushed(object? sender, DataGridViewCellValueEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= _store.HoldingRows.Count || e.ColumnIndex != 2)
        {
            return;
        }

        RegisterRow row = _store.HoldingRows[e.RowIndex];
        row.ValueText = e.Value?.ToString() ?? string.Empty;
        // Repaint value + hex (the row rejects invalid input, restoring old text).
        _holdingGrid.UpdateCellValue(2, e.RowIndex);
        _holdingGrid.UpdateCellValue(3, e.RowIndex);
    }

    private void OnCoilCellValue(object? sender, DataGridViewCellValueEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= _store.CoilRows.Count)
        {
            return;
        }

        CoilRow row = _store.CoilRows[e.RowIndex];
        e.Value = e.ColumnIndex switch
        {
            0 => row.Address.ToString(System.Globalization.CultureInfo.InvariantCulture),
            1 => row.AddressHex,
            2 => row.IsOn,
            _ => null,
        };
    }

    private void OnCoilCellPushed(object? sender, DataGridViewCellValueEventArgs e)
    {
        if (e.RowIndex < 0 || e.RowIndex >= _store.CoilRows.Count || e.ColumnIndex != 2)
        {
            return;
        }

        CoilRow row = _store.CoilRows[e.RowIndex];
        if (e.Value is bool on)
        {
            row.IsOn = on;
        }

        _coilGrid.UpdateCellValue(2, e.RowIndex);
    }

    private void OnCoilGridDirty(object? sender, EventArgs e)
    {
        // Commit checkbox clicks immediately so CellValuePushed fires on click.
        if (_coilGrid.IsCurrentCellDirty)
        {
            _coilGrid.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }
    }

    private async void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _counterTimer.Stop();
        _counterTimer.Dispose();
        _store.Dispose();
        await StopListenerAsync();
    }

    private async void OnAddHoldingRange(object? sender, EventArgs e)
    {
        SyncWatchFromUi();
        await RunWithProgress(_holdingProgressBar, () => _store.AddHoldingRangeAsync());
        SyncHoldingRows();
    }

    private async void OnAddCoilRange(object? sender, EventArgs e)
    {
        SyncWatchFromUi();
        await RunWithProgress(_coilProgressBar, () => _store.AddCoilRangeAsync());
        SyncCoilRows();
    }

    private async Task RunWithProgress(ProgressBar bar, Func<Task> work)
    {
        bar.Value = 0;
        using var timer = new System.Windows.Forms.Timer { Interval = 200 };
        timer.Tick += (_, _) => bar.Value = Math.Clamp((int)_store.BulkProgress, 0, 100);
        timer.Start();
        try
        {
            await work();
        }
        finally
        {
            timer.Stop();
            bar.Value = 0;
        }
    }

    private void OnSaveMap(object? sender, EventArgs e)
    {
        SyncWatchFromUi();
        using SaveFileDialog dialog = new()
        {
            Filter = "Register map (*.json)|*.json",
            FileName = $"slave{_store.SelectedSlaveId}-map.json",
        };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _store.SaveMapTo(dialog.FileName);
        }
    }

    private void OnLoadMap(object? sender, EventArgs e)
    {
        using OpenFileDialog dialog = new()
        {
            Filter = "Register map (*.json)|*.json",
        };
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _store.LoadMapFrom(dialog.FileName);
            SyncHoldingRows();
            SyncCoilRows();
        }
    }

    private void OnWriteFloat(object? sender, EventArgs e)
    {
        _store.FloatAddressText = _floatAddressBox.Text;
        _store.FloatValueText = _floatValueBox.Text;
        _store.FloatOrder = _floatOrderBox.SelectedItem as FloatOrderOption ?? _store.FloatOrderOptions[0];
        if (!TryParseAddress(_store.FloatAddressText, out ushort address))
        {
            return;
        }

        if (!float.TryParse(_store.FloatValueText, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out float value))
        {
            _store.SetRunningStatus($"Invalid float: '{_store.FloatValueText}'.");
            return;
        }

        _store.WriteFloatAt(address, value, _store.FloatOrder.Order);
        SyncHoldingRows();
        _floatStatusLabel.Text = _store.StatusText;
    }

    private bool TryParseAddress(string text, out ushort address)
    {
        try
        {
            address = ModbusAddress.Parse(text, ushort.MaxValue + 1);
            return true;
        }
        catch (ArgumentException)
        {
            address = 0;
            _store.SetRunningStatus($"Invalid address: '{text}'.");
            return false;
        }
    }

    private void OnHoldingRightClick(object? sender, MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Right)
        {
            return;
        }

        DataGridView.HitTestInfo hit = _holdingGrid.HitTest(e.X, e.Y);
        if (hit.RowIndex < 0 || hit.RowIndex >= _store.HoldingRows.Count)
        {
            return;
        }

        ushort address = _store.HoldingRows[hit.RowIndex].Address;
        using FloatInputDialog dialog = new(address, _store.FloatOrder);
        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            _store.WriteFloatAt(address, dialog.FloatValue, dialog.Order.Order);
            SyncHoldingRows();
        }
    }
}
