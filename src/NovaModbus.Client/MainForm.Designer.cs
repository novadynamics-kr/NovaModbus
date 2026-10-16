using NovaModbus.Client.ViewModels;

namespace NovaModbus.Client;

#nullable disable

partial class MainForm
{
    private System.ComponentModel.IContainer components;
    private Panel _settingsPanel;
    private FlowLayoutPanel _settingsFlow;
    private Label _portLabel;
    private FlowLayoutPanel _portRow;
    private ComboBox _portBox;
    private Button _refreshButton;
    private Label _baudLabel;
    private ComboBox _baudBox;
    private Label _dataBitsLabel;
    private ComboBox _dataBitsBox;
    private Label _parityLabel;
    private ComboBox _parityBox;
    private Label _stopBitsLabel;
    private ComboBox _stopBitsBox;
    private Label _portStatusLabel;
    private Panel _connectPanel;
    private CheckBox _testSlaveCheck;
    private Button _connectButton;
    private Button _disconnectButton;
    private Label _connectionStatusLabel;
    private TabControl _tabs;
    private TabPage _readWritePage;
    private FlowLayoutPanel _readWriteTop;
    private ComboBox _functionBox;
    private TextBox _slaveBox;
    private TextBox _startBox;
    private TextBox _countBox;
    private Button _executeButton;
    private Label _hintLabel;
    private TextBox _valuesBox;
    private TextBox _resultBox;
    private TabPage _pollingPage;
    private FlowLayoutPanel _pollingTop;
    private TextBox _pollingIntervalBox;
    private Button _pollingStartButton;
    private Button _pollingStopButton;
    private ListBox _pollingHistoryBox;
    private TabPage _logPage;
    private ListBox _logBox;
    private TabPage _watchPage;
    private FlowLayoutPanel _watchTop;
    private TextBox _watchSlaveBox;
    private ComboBox _areaBox;
    private TextBox _watchStartBox;
    private TextBox _watchEndBox;
    private TextBox _watchCountBox;
    private TextBox _watchExtraBox;
    private Button _watchAddButton;
    private Button _watchAddRangeButton;
    private Button _watchDeleteButton;
    private Button _watchClearAllButton;
    private Button _watchSelectAllButton;
    private Button _watchDeselectAllButton;
    private Button _watchSaveButton;
    private Button _watchLoadButton;
    private FlowLayoutPanel _watchMid;
    private Button _readAllButton;
    private CheckBox _autoRefreshCheck;
    private TextBox _watchIntervalBox;
    private ProgressBar _bulkProgressBar;
    private Label _watchStatusLabel;
    private SplitContainer _watchSplit;
    private DataGridView _entriesGrid;
    private DataGridView _resultsGrid;
    private StatusStrip _status;
    private ToolStripStatusLabel _statusLabel;

    /// <summary>Clean up any resources being used.</summary>
    /// <param name="disposing">Whether managed resources should be disposed.</param>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            components?.Dispose();
        }

        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        _settingsPanel = new Panel();
        _settingsFlow = new FlowLayoutPanel();
        _portLabel = new Label();
        _portRow = new FlowLayoutPanel();
        _portBox = new ComboBox();
        _refreshButton = new Button();
        _baudLabel = new Label();
        _baudBox = new ComboBox();
        _dataBitsLabel = new Label();
        _dataBitsBox = new ComboBox();
        _parityLabel = new Label();
        _parityBox = new ComboBox();
        _stopBitsLabel = new Label();
        _stopBitsBox = new ComboBox();
        _portStatusLabel = new Label();
        _connectPanel = new Panel();
        _testSlaveCheck = new CheckBox();
        _connectButton = new Button();
        _disconnectButton = new Button();
        _connectionStatusLabel = new Label();
        _tabs = new TabControl();
        _readWritePage = new TabPage();
        _readWriteTop = new FlowLayoutPanel();
        _functionBox = new ComboBox();
        _slaveBox = new TextBox();
        _startBox = new TextBox();
        _countBox = new TextBox();
        _executeButton = new Button();
        _hintLabel = new Label();
        _valuesBox = new TextBox();
        _resultBox = new TextBox();
        _pollingPage = new TabPage();
        _pollingTop = new FlowLayoutPanel();
        _pollingIntervalBox = new TextBox();
        _pollingStartButton = new Button();
        _pollingStopButton = new Button();
        _pollingHistoryBox = new ListBox();
        _logPage = new TabPage();
        _logBox = new ListBox();
        _watchPage = new TabPage();
        _watchTop = new FlowLayoutPanel();
        _watchSlaveBox = new TextBox();
        _areaBox = new ComboBox();
        _watchStartBox = new TextBox();
        _watchEndBox = new TextBox();
        _watchCountBox = new TextBox();
        _watchExtraBox = new TextBox();
        _watchAddButton = new Button();
        _watchAddRangeButton = new Button();
        _watchDeleteButton = new Button();
        _watchClearAllButton = new Button();
        _watchSelectAllButton = new Button();
        _watchDeselectAllButton = new Button();
        _watchSaveButton = new Button();
        _watchLoadButton = new Button();
        _watchMid = new FlowLayoutPanel();
        _readAllButton = new Button();
        _autoRefreshCheck = new CheckBox();
        _watchIntervalBox = new TextBox();
        _bulkProgressBar = new ProgressBar();
        _watchStatusLabel = new Label();
        _watchSplit = new SplitContainer();
        _entriesGrid = new DataGridView();
        _resultsGrid = new DataGridView();
        _status = new StatusStrip();
        _statusLabel = new ToolStripStatusLabel();
        _settingsPanel.SuspendLayout();
        _settingsFlow.SuspendLayout();
        _portRow.SuspendLayout();
        _connectPanel.SuspendLayout();
        _tabs.SuspendLayout();
        _readWritePage.SuspendLayout();
        _readWriteTop.SuspendLayout();
        _status.SuspendLayout();
        SuspendLayout();

        _settingsPanel.Dock = DockStyle.Left;
        _settingsPanel.Width = 270;
        _settingsPanel.Padding = new Padding(8);

        _settingsFlow.Dock = DockStyle.Fill;
        _settingsFlow.FlowDirection = FlowDirection.TopDown;
        _settingsFlow.WrapContents = false;
        _settingsFlow.AutoScroll = true;

        _portLabel.Text = "COM Port";
        _portLabel.AutoSize = true;
        _portRow.AutoSize = true;
        _portBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _portBox.Width = 150;
        _refreshButton.Text = "Refresh";
        _refreshButton.AutoSize = true;
        _refreshButton.Click += OnRefreshPorts;
        _portRow.Controls.Add(_portBox);
        _portRow.Controls.Add(_refreshButton);

        _baudLabel.Text = "Baud Rate";
        _baudLabel.AutoSize = true;
        _baudBox.DropDownStyle = ComboBoxStyle.DropDown;
        _baudBox.Width = 238;

        _dataBitsLabel.Text = "Data Bits";
        _dataBitsLabel.AutoSize = true;
        _dataBitsBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _dataBitsBox.Width = 238;

        _parityLabel.Text = "Parity";
        _parityLabel.AutoSize = true;
        _parityBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _parityBox.Width = 238;

        _stopBitsLabel.Text = "Stop Bits";
        _stopBitsLabel.AutoSize = true;
        _stopBitsBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _stopBitsBox.Width = 238;

        _portStatusLabel.AutoSize = true;
        _portStatusLabel.ForeColor = SystemColors.GrayText;

        _settingsFlow.Controls.Add(_portLabel);
        _settingsFlow.Controls.Add(_portRow);
        _settingsFlow.Controls.Add(_baudLabel);
        _settingsFlow.Controls.Add(_baudBox);
        _settingsFlow.Controls.Add(_dataBitsLabel);
        _settingsFlow.Controls.Add(_dataBitsBox);
        _settingsFlow.Controls.Add(_parityLabel);
        _settingsFlow.Controls.Add(_parityBox);
        _settingsFlow.Controls.Add(_stopBitsLabel);
        _settingsFlow.Controls.Add(_stopBitsBox);
        _settingsFlow.Controls.Add(_portStatusLabel);

        _connectPanel.Dock = DockStyle.Bottom;
        _connectPanel.Height = 130;
        _connectPanel.Padding = new Padding(0, 8, 0, 0);
        _testSlaveCheck.Text = "Built-in test slave";
        _testSlaveCheck.Checked = true;
        _testSlaveCheck.Dock = DockStyle.Top;
        _testSlaveCheck.CheckedChanged += OnTestSlaveToggled;
        _connectButton.Text = "Connect";
        _connectButton.Dock = DockStyle.Top;
        _connectButton.Height = 28;
        _connectButton.Click += OnConnect;
        _disconnectButton.Text = "Disconnect";
        _disconnectButton.Dock = DockStyle.Top;
        _disconnectButton.Height = 28;
        _disconnectButton.Click += OnDisconnect;
        _connectionStatusLabel.AutoSize = true;
        _connectionStatusLabel.Dock = DockStyle.Top;
        _connectPanel.Controls.Add(_connectionStatusLabel);
        _connectPanel.Controls.Add(_disconnectButton);
        _connectPanel.Controls.Add(_connectButton);
        _connectPanel.Controls.Add(_testSlaveCheck);

        _settingsPanel.Controls.Add(_settingsFlow);
        _settingsPanel.Controls.Add(_connectPanel);

        _tabs.Dock = DockStyle.Fill;
        _readWritePage.Text = "Read / Write";
        _readWritePage.UseVisualStyleBackColor = true;
        _pollingPage.Text = "Polling";
        _pollingPage.UseVisualStyleBackColor = true;
        _logPage.Text = "Log";
        _logPage.UseVisualStyleBackColor = true;
        _watchPage.Text = "Watch";
        _watchPage.UseVisualStyleBackColor = true;
        _tabs.TabPages.AddRange([_readWritePage, _pollingPage, _logPage, _watchPage]);

        _pollingTop.Dock = DockStyle.Top;
        _pollingTop.AutoSize = true;
        _pollingTop.Padding = new Padding(8);
        _pollingIntervalBox.Width = 100;
        _pollingIntervalBox.Text = "1000";
        _pollingStartButton.Text = "Start";
        _pollingStartButton.AutoSize = true;
        _pollingStartButton.Click += OnPollingStart;
        _pollingStopButton.Text = "Stop";
        _pollingStopButton.AutoSize = true;
        _pollingStopButton.Click += OnPollingStop;
        _pollingHistoryBox.Dock = DockStyle.Fill;
        _pollingHistoryBox.Font = new Font("Consolas", 9F);
        _pollingTop.Controls.Add(Labeled("Interval (ms)", _pollingIntervalBox));
        _pollingTop.Controls.Add(_pollingStartButton);
        _pollingTop.Controls.Add(_pollingStopButton);
        _pollingStartButton.Margin = new Padding(0, 15, 0, 0);
        _pollingStopButton.Margin = new Padding(0, 15, 0, 0);
        _pollingPage.Controls.Add(_pollingHistoryBox);
        _pollingPage.Controls.Add(_pollingTop);

        _logBox.Dock = DockStyle.Fill;
        _logBox.Font = new Font("Consolas", 9F);
        _logPage.Controls.Add(_logBox);

        _watchTop.Dock = DockStyle.Top;
        _watchTop.AutoSize = true;
        _watchTop.Padding = new Padding(8);
        _watchSlaveBox.Width = 70;
        _watchSlaveBox.Text = "1";
        _areaBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _areaBox.Width = 140;
        _watchStartBox.Width = 80;
        _watchStartBox.Text = "0";
        _watchEndBox.Width = 80;
        _watchEndBox.Text = "9";
        _watchCountBox.Width = 80;
        _watchCountBox.Text = "1";
        _watchExtraBox.Width = 140;
        _watchAddButton.Text = "Add";
        _watchAddButton.AutoSize = true;
        _watchAddButton.Click += OnWatchAdd;
        _watchAddRangeButton.Text = "Add Range";
        _watchAddRangeButton.AutoSize = true;
        _watchAddRangeButton.Click += OnWatchAddRange;
        _watchDeleteButton.Text = "Del";
        _watchDeleteButton.AutoSize = true;
        _watchDeleteButton.Click += OnWatchDelete;
        _watchClearAllButton.Text = "Clear All";
        _watchClearAllButton.AutoSize = true;
        _watchClearAllButton.Click += OnWatchClearAll;
        _watchSelectAllButton.Text = "Select All";
        _watchSelectAllButton.AutoSize = true;
        _watchSelectAllButton.Click += OnWatchSelectAll;
        _watchDeselectAllButton.Text = "Deselect All";
        _watchDeselectAllButton.AutoSize = true;
        _watchDeselectAllButton.Click += OnWatchDeselectAll;
        _watchSaveButton.Text = "Save";
        _watchSaveButton.AutoSize = true;
        _watchSaveButton.Click += OnWatchSave;
        _watchLoadButton.Text = "Load";
        _watchLoadButton.AutoSize = true;
        _watchLoadButton.Click += OnWatchLoad;
        _watchTop.Controls.Add(Labeled("Slave ID", _watchSlaveBox));
        _watchTop.Controls.Add(Labeled("Area", _areaBox));
        _watchTop.Controls.Add(Labeled("Address (dec/0x)", _watchStartBox));
        _watchTop.Controls.Add(Labeled("End (dec/0x)", _watchEndBox));
        _watchTop.Controls.Add(Labeled("Count / Record", _watchCountBox));
        _watchTop.Controls.Add(Labeled("Extra (diag data / file len)", _watchExtraBox));
        _watchTop.Controls.Add(_watchAddButton);
        _watchTop.Controls.Add(_watchAddRangeButton);
        _watchTop.Controls.Add(_watchDeleteButton);
        _watchTop.Controls.Add(_watchClearAllButton);
        _watchTop.Controls.Add(_watchSelectAllButton);
        _watchTop.Controls.Add(_watchDeselectAllButton);
        _watchTop.Controls.Add(_watchSaveButton);
        _watchTop.Controls.Add(_watchLoadButton);

        _watchMid.Dock = DockStyle.Top;
        _watchMid.AutoSize = true;
        _watchMid.Padding = new Padding(8, 0, 8, 0);
        _readAllButton.Text = "Read All";
        _readAllButton.AutoSize = true;
        _readAllButton.Click += OnWatchReadAll;
        _autoRefreshCheck.Text = "Auto-refresh";
        _autoRefreshCheck.AutoSize = true;
        _autoRefreshCheck.CheckedChanged += OnWatchAutoToggled;
        _watchIntervalBox.Width = 80;
        _watchIntervalBox.Text = "1000";
        _bulkProgressBar.Width = 120;
        _bulkProgressBar.Minimum = 0;
        _bulkProgressBar.Maximum = 100;
        _watchStatusLabel.AutoSize = true;
        _watchMid.Controls.Add(_readAllButton);
        _watchMid.Controls.Add(_autoRefreshCheck);
        _watchMid.Controls.Add(_watchIntervalBox);
        _watchMid.Controls.Add(new Label { Text = "ms", AutoSize = true });
        _watchMid.Controls.Add(_bulkProgressBar);
        _watchMid.Controls.Add(_watchStatusLabel);

        _watchSplit.Dock = DockStyle.Fill;
        _watchSplit.Orientation = Orientation.Horizontal;
        SetupWatchGrid(_entriesGrid, true);
        SetupWatchGrid(_resultsGrid, false);
        _watchSplit.Panel1.Controls.Add(_entriesGrid);
        _watchSplit.Panel2.Controls.Add(_resultsGrid);

        _watchPage.Controls.Add(_watchSplit);
        _watchPage.Controls.Add(_watchMid);
        _watchPage.Controls.Add(_watchTop);

        _readWriteTop.Dock = DockStyle.Top;
        _readWriteTop.AutoSize = true;
        _readWriteTop.Padding = new Padding(8);

        _functionBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _functionBox.Width = 180;
        _functionBox.SelectedIndexChanged += OnFunctionChanged;
        _slaveBox.Width = 70;
        _slaveBox.Text = "1";
        _startBox.Width = 80;
        _startBox.Text = "0";
        _countBox.Width = 80;
        _countBox.Text = "10";
        _executeButton.Text = "Execute";
        _executeButton.AutoSize = true;
        _executeButton.Margin = new Padding(0, 15, 0, 0);
        _executeButton.Click += OnExecute;
        _readWriteTop.Controls.Add(Labeled("Function", _functionBox));
        _readWriteTop.Controls.Add(Labeled("Slave ID", _slaveBox));
        _readWriteTop.Controls.Add(Labeled("Start", _startBox));
        _readWriteTop.Controls.Add(Labeled("Count", _countBox));
        _readWriteTop.Controls.Add(_executeButton);

        _hintLabel.Dock = DockStyle.Top;
        _hintLabel.Padding = new Padding(8, 4, 8, 0);
        _hintLabel.AutoSize = true;
        _valuesBox.Dock = DockStyle.Top;
        _valuesBox.Margin = new Padding(8, 0, 8, 0);
        _resultBox.Dock = DockStyle.Fill;
        _resultBox.Multiline = true;
        _resultBox.ReadOnly = true;
        _resultBox.ScrollBars = ScrollBars.Both;
        _resultBox.Font = new Font("Consolas", 9F);
        _resultBox.WordWrap = false;
        _readWritePage.Controls.Add(_resultBox);
        _readWritePage.Controls.Add(_valuesBox);
        _readWritePage.Controls.Add(_hintLabel);
        _readWritePage.Controls.Add(_readWriteTop);

        _statusLabel.Text = "Disconnected";
        _status.Items.Add(_statusLabel);

        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1000, 650);
        Controls.Add(_tabs);
        Controls.Add(_settingsPanel);
        Controls.Add(_status);
        Name = "MainForm";
        Text = "NovaModbus Client";
        _settingsPanel.ResumeLayout(false);
        _settingsFlow.ResumeLayout(false);
        _settingsFlow.PerformLayout();
        _portRow.ResumeLayout(false);
        _portRow.PerformLayout();
        _connectPanel.ResumeLayout(false);
        _connectPanel.PerformLayout();
        _tabs.ResumeLayout(false);
        _readWritePage.ResumeLayout(false);
        _readWritePage.PerformLayout();
        _readWriteTop.ResumeLayout(false);
        _readWriteTop.PerformLayout();
        _status.ResumeLayout(false);
        _status.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    private void SetupWatchGrid(DataGridView grid, bool isEntries)
    {
        grid.Dock = DockStyle.Fill;
        grid.AutoGenerateColumns = false;
        grid.AllowUserToAddRows = false;
        grid.AllowUserToDeleteRows = false;
        grid.RowHeadersVisible = false;
        grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        grid.MultiSelect = false;
        grid.BackgroundColor = SystemColors.Window;
        grid.KeyDown += OnWatchGridKey;
        grid.CurrentCellDirtyStateChanged += OnWatchGridDirty;

        DataGridViewCheckBoxColumn selected = new()
        {
            DataPropertyName = nameof(WatchEntry.IsSelected),
            HeaderText = "✓",
            Width = 30,
            ReadOnly = false,
        };
        grid.Columns.Add(selected);

        if (isEntries)
        {
            grid.Columns.Add(TextColumn(nameof(WatchEntry.SlaveId), "Slave", 60));
            grid.Columns.Add(TextColumn(nameof(WatchEntry.Area), "Area", 120));
            grid.Columns.Add(TextColumn(nameof(WatchEntry.Start), "Address", 70));
            grid.Columns.Add(TextColumn(nameof(WatchEntry.AddressHex), "AddressHex", 80));
            grid.Columns.Add(TextColumn(nameof(WatchEntry.Count), "Count", 60));
            grid.Columns.Add(TextColumn(nameof(WatchEntry.Extra), "Extra", -1));
        }
        else
        {
            grid.Columns.Add(TextColumn(null, "Slave", 60));
            grid.Columns.Add(TextColumn(null, "Area", 120));
            grid.Columns.Add(TextColumn(null, "Address", 70));
            grid.Columns.Add(TextColumn(null, "Dec", -1));
            grid.Columns.Add(TextColumn(null, "Hex", 220));
            grid.Columns.Add(TextColumn(null, "Time", 110));
            grid.Columns.Add(TextColumn(null, "Status", 140));
            grid.CellFormatting += OnResultsFormatting;
        }
    }

    private static DataGridViewTextBoxColumn TextColumn(string property, string header, int width)
    {
        DataGridViewTextBoxColumn column = new()
        {
            HeaderText = header,
            ReadOnly = true,
        };
        if (property is not null)
        {
            column.DataPropertyName = property;
        }

        if (width < 0)
        {
            column.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
        }
        else
        {
            column.Width = width;
        }

        return column;
    }

    private static Panel Labeled(string text, Control input)
    {
        Panel panel = new() { Width = input.Width, Height = 44 };
        Label label = new() { Text = text, AutoSize = true, Location = new Point(0, 0) };
        input.Location = new Point(0, 20);
        panel.Controls.Add(label);
        panel.Controls.Add(input);
        return panel;
    }
}
