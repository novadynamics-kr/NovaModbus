namespace NovaModbus.Slave;

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
    private Panel _listenPanel;
    private Button _startButton;
    private Button _stopButton;
    private Label _listenerStatusLabel;
    private Label _slaveIdsLabel;
    private ListBox _slaveIdBox;
    private FlowLayoutPanel _slaveIdRow;
    private TextBox _newSlaveIdBox;
    private Button _addSlaveButton;
    private Button _removeSlaveButton;
    private CheckBox _ignoreCrcCheck;
    private CheckBox _busyCheck;
    private TabControl _tabs;
    private TabPage _holdingPage;
    private FlowLayoutPanel _holdingTop;
    private TextBox _holdingAddressBox;
    private TextBox _holdingEndBox;
    private TextBox _holdingValueBox;
    private Button _holdingAddButton;
    private Button _holdingAddRangeButton;
    private Button _holdingDeleteButton;
    private Button _holdingSelectAllButton;
    private Button _holdingDeselectAllButton;
    private Button _holdingSaveButton;
    private Button _holdingLoadButton;
    private ProgressBar _holdingProgressBar;
    private Label _holdingStatusLabel;
    private DataGridView _holdingGrid;
    private TabPage _coilsPage;
    private FlowLayoutPanel _coilsTop;
    private TextBox _coilAddressBox;
    private TextBox _coilEndBox;
    private Button _coilAddButton;
    private Button _coilAddRangeButton;
    private Button _coilDeleteButton;
    private Button _coilSelectAllButton;
    private Button _coilDeselectAllButton;
    private ProgressBar _coilProgressBar;
    private Label _coilStatusLabel;
    private DataGridView _coilGrid;
    private TabPage _countersPage;
    private TextBox _countersBox;
    private TabPage _logPage;
    private ListBox _logBox;
    private TabPage _floatPage;
    private FlowLayoutPanel _floatPanel;
    private TextBox _floatAddressBox;
    private TextBox _floatValueBox;
    private ComboBox _floatOrderBox;
    private Button _floatWriteButton;
    private Label _floatStatusLabel;
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
        _listenPanel = new Panel();
        _startButton = new Button();
        _stopButton = new Button();
        _listenerStatusLabel = new Label();
        _slaveIdsLabel = new Label();
        _slaveIdBox = new ListBox();
        _slaveIdRow = new FlowLayoutPanel();
        _newSlaveIdBox = new TextBox();
        _addSlaveButton = new Button();
        _removeSlaveButton = new Button();
        _ignoreCrcCheck = new CheckBox();
        _busyCheck = new CheckBox();
        _tabs = new TabControl();
        _holdingPage = new TabPage();
        _holdingTop = new FlowLayoutPanel();
        _holdingAddressBox = new TextBox();
        _holdingEndBox = new TextBox();
        _holdingValueBox = new TextBox();
        _holdingAddButton = new Button();
        _holdingAddRangeButton = new Button();
        _holdingDeleteButton = new Button();
        _holdingSelectAllButton = new Button();
        _holdingDeselectAllButton = new Button();
        _holdingSaveButton = new Button();
        _holdingLoadButton = new Button();
        _holdingProgressBar = new ProgressBar();
        _holdingStatusLabel = new Label();
        _holdingGrid = new DataGridView();
        _coilsPage = new TabPage();
        _coilsTop = new FlowLayoutPanel();
        _coilAddressBox = new TextBox();
        _coilEndBox = new TextBox();
        _coilAddButton = new Button();
        _coilAddRangeButton = new Button();
        _coilDeleteButton = new Button();
        _coilSelectAllButton = new Button();
        _coilDeselectAllButton = new Button();
        _coilProgressBar = new ProgressBar();
        _coilStatusLabel = new Label();
        _coilGrid = new DataGridView();
        _countersPage = new TabPage();
        _countersBox = new TextBox();
        _logPage = new TabPage();
        _logBox = new ListBox();
        _floatPage = new TabPage();
        _floatPanel = new FlowLayoutPanel();
        _floatAddressBox = new TextBox();
        _floatValueBox = new TextBox();
        _floatOrderBox = new ComboBox();
        _floatWriteButton = new Button();
        _floatStatusLabel = new Label();
        _status = new StatusStrip();
        _statusLabel = new ToolStripStatusLabel();
        _settingsPanel.SuspendLayout();
        _settingsFlow.SuspendLayout();
        _portRow.SuspendLayout();
        _listenPanel.SuspendLayout();
        _slaveIdRow.SuspendLayout();
        _tabs.SuspendLayout();
        _holdingPage.SuspendLayout();
        _holdingTop.SuspendLayout();
        _coilsPage.SuspendLayout();
        _coilsTop.SuspendLayout();
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

        _listenPanel.Dock = DockStyle.Bottom;
        _listenPanel.Height = 340;
        _listenPanel.Padding = new Padding(0, 8, 0, 0);
        _startButton.Text = "Start";
        _startButton.Dock = DockStyle.Top;
        _startButton.Height = 28;
        _startButton.Click += OnStart;
        _stopButton.Text = "Stop";
        _stopButton.Dock = DockStyle.Top;
        _stopButton.Height = 28;
        _stopButton.Click += OnStop;
        _listenerStatusLabel.AutoSize = true;
        _listenerStatusLabel.Dock = DockStyle.Top;
        _slaveIdsLabel.Text = "Slave IDs";
        _slaveIdsLabel.AutoSize = true;
        _slaveIdsLabel.Dock = DockStyle.Top;
        _slaveIdBox.Dock = DockStyle.Top;
        _slaveIdBox.Height = 80;
        _slaveIdBox.SelectedIndexChanged += OnSlaveIdSelected;
        _slaveIdRow.AutoSize = true;
        _slaveIdRow.Dock = DockStyle.Top;
        _newSlaveIdBox.Width = 80;
        _newSlaveIdBox.Text = "1";
        _addSlaveButton.Text = "Add";
        _addSlaveButton.AutoSize = true;
        _addSlaveButton.Click += OnAddSlave;
        _removeSlaveButton.Text = "Del";
        _removeSlaveButton.AutoSize = true;
        _removeSlaveButton.Click += OnRemoveSlave;
        _slaveIdRow.Controls.Add(_newSlaveIdBox);
        _slaveIdRow.Controls.Add(_addSlaveButton);
        _slaveIdRow.Controls.Add(_removeSlaveButton);
        _ignoreCrcCheck.Text = "Ignore CRC errors";
        _ignoreCrcCheck.Checked = true;
        _ignoreCrcCheck.Dock = DockStyle.Top;
        _ignoreCrcCheck.CheckedChanged += OnErrorInjectionToggled;
        _busyCheck.Text = "Simulate busy";
        _busyCheck.Dock = DockStyle.Top;
        _busyCheck.CheckedChanged += OnErrorInjectionToggled;
        _listenPanel.Controls.Add(_busyCheck);
        _listenPanel.Controls.Add(_ignoreCrcCheck);
        _listenPanel.Controls.Add(_slaveIdRow);
        _listenPanel.Controls.Add(_slaveIdBox);
        _listenPanel.Controls.Add(_slaveIdsLabel);
        _listenPanel.Controls.Add(_listenerStatusLabel);
        _listenPanel.Controls.Add(_stopButton);
        _listenPanel.Controls.Add(_startButton);

        _settingsPanel.Controls.Add(_settingsFlow);
        _settingsPanel.Controls.Add(_listenPanel);

        _tabs.Dock = DockStyle.Fill;
        _holdingPage.Text = "Holding";
        _holdingPage.UseVisualStyleBackColor = true;
        _coilsPage.Text = "Coils";
        _coilsPage.UseVisualStyleBackColor = true;
        _countersPage.Text = "Counters";
        _countersPage.UseVisualStyleBackColor = true;
        _logPage.Text = "Log";
        _logPage.UseVisualStyleBackColor = true;
        _floatPage.Text = "Float";
        _floatPage.UseVisualStyleBackColor = true;
        _tabs.TabPages.AddRange([_holdingPage, _coilsPage, _countersPage, _logPage, _floatPage]);

        _holdingTop.Dock = DockStyle.Top;
        _holdingTop.AutoSize = true;
        _holdingTop.Padding = new Padding(8);
        _holdingAddressBox.Width = 80;
        _holdingAddressBox.Text = "0";
        _holdingEndBox.Width = 80;
        _holdingEndBox.Text = "9";
        _holdingValueBox.Width = 100;
        _holdingValueBox.Text = "0";
        _holdingAddButton.Text = "Add";
        _holdingAddButton.AutoSize = true;
        _holdingAddButton.Click += OnAddHolding;
        _holdingAddRangeButton.Text = "Add Range";
        _holdingAddRangeButton.AutoSize = true;
        _holdingAddRangeButton.Click += OnAddHoldingRange;
        _holdingDeleteButton.Text = "Del";
        _holdingDeleteButton.AutoSize = true;
        _holdingDeleteButton.Click += OnDeleteHolding;
        _holdingSelectAllButton.Text = "Select All";
        _holdingSelectAllButton.AutoSize = true;
        _holdingSelectAllButton.Click += OnSelectAllHolding;
        _holdingDeselectAllButton.Text = "Deselect All";
        _holdingDeselectAllButton.AutoSize = true;
        _holdingDeselectAllButton.Click += OnDeselectAllHolding;
        _holdingSaveButton.Text = "Save";
        _holdingSaveButton.AutoSize = true;
        _holdingSaveButton.Click += OnSaveMap;
        _holdingLoadButton.Text = "Load";
        _holdingLoadButton.AutoSize = true;
        _holdingLoadButton.Click += OnLoadMap;
        _holdingTop.Controls.Add(Labeled("Address (dec/0x)", _holdingAddressBox));
        _holdingTop.Controls.Add(Labeled("End (dec/0x)", _holdingEndBox));
        _holdingTop.Controls.Add(Labeled("Value (dec/0x)", _holdingValueBox));
        _holdingTop.Controls.Add(_holdingAddButton);
        _holdingTop.Controls.Add(_holdingAddRangeButton);
        _holdingTop.Controls.Add(_holdingDeleteButton);
        _holdingTop.Controls.Add(_holdingSelectAllButton);
        _holdingTop.Controls.Add(_holdingDeselectAllButton);
        _holdingTop.Controls.Add(_holdingSaveButton);
        _holdingTop.Controls.Add(_holdingLoadButton);

        _holdingProgressBar.Dock = DockStyle.Top;
        _holdingProgressBar.Height = 10;
        _holdingProgressBar.Minimum = 0;
        _holdingProgressBar.Maximum = 100;
        _holdingStatusLabel.Dock = DockStyle.Top;
        _holdingStatusLabel.AutoSize = true;
        _holdingStatusLabel.Padding = new Padding(8, 0, 8, 0);

        SetupHoldingGrid();
        _holdingPage.Controls.Add(_holdingGrid);
        _holdingPage.Controls.Add(_holdingStatusLabel);
        _holdingPage.Controls.Add(_holdingProgressBar);
        _holdingPage.Controls.Add(_holdingTop);

        _coilsTop.Dock = DockStyle.Top;
        _coilsTop.AutoSize = true;
        _coilsTop.Padding = new Padding(8);
        _coilAddressBox.Width = 80;
        _coilAddressBox.Text = "0";
        _coilEndBox.Width = 80;
        _coilEndBox.Text = "9";
        _coilAddButton.Text = "Add";
        _coilAddButton.AutoSize = true;
        _coilAddButton.Click += OnAddCoil;
        _coilAddRangeButton.Text = "Add Range";
        _coilAddRangeButton.AutoSize = true;
        _coilAddRangeButton.Click += OnAddCoilRange;
        _coilDeleteButton.Text = "Del";
        _coilDeleteButton.AutoSize = true;
        _coilDeleteButton.Click += OnDeleteCoil;
        _coilSelectAllButton.Text = "Select All";
        _coilSelectAllButton.AutoSize = true;
        _coilSelectAllButton.Click += OnSelectAllCoils;
        _coilDeselectAllButton.Text = "Deselect All";
        _coilDeselectAllButton.AutoSize = true;
        _coilDeselectAllButton.Click += OnDeselectAllCoils;
        _coilsTop.Controls.Add(Labeled("Address (dec/0x)", _coilAddressBox));
        _coilsTop.Controls.Add(Labeled("End (dec/0x)", _coilEndBox));
        _coilsTop.Controls.Add(_coilAddButton);
        _coilsTop.Controls.Add(_coilAddRangeButton);
        _coilsTop.Controls.Add(_coilDeleteButton);
        _coilsTop.Controls.Add(_coilSelectAllButton);
        _coilsTop.Controls.Add(_coilDeselectAllButton);

        _coilProgressBar.Dock = DockStyle.Top;
        _coilProgressBar.Height = 10;
        _coilProgressBar.Minimum = 0;
        _coilProgressBar.Maximum = 100;
        _coilStatusLabel.Dock = DockStyle.Top;
        _coilStatusLabel.AutoSize = true;
        _coilStatusLabel.Padding = new Padding(8, 0, 8, 0);

        SetupCoilGrid();
        _coilsPage.Controls.Add(_coilGrid);
        _coilsPage.Controls.Add(_coilStatusLabel);
        _coilsPage.Controls.Add(_coilProgressBar);
        _coilsPage.Controls.Add(_coilsTop);

        _countersBox.Dock = DockStyle.Fill;
        _countersBox.Multiline = true;
        _countersBox.ReadOnly = true;
        _countersBox.ScrollBars = ScrollBars.Both;
        _countersBox.Font = new Font("Consolas", 9F);
        _countersBox.WordWrap = false;
        _countersBox.Margin = new Padding(8);
        _countersPage.Controls.Add(_countersBox);

        _logBox.Dock = DockStyle.Fill;
        _logBox.Font = new Font("Consolas", 9F);
        _logPage.Controls.Add(_logBox);

        _floatPanel.Dock = DockStyle.Fill;
        _floatPanel.FlowDirection = FlowDirection.TopDown;
        _floatPanel.WrapContents = false;
        _floatPanel.Padding = new Padding(8);
        _floatAddressBox.Width = 120;
        _floatAddressBox.Text = "0";
        _floatValueBox.Width = 200;
        _floatValueBox.Text = "0";
        _floatOrderBox.DropDownStyle = ComboBoxStyle.DropDownList;
        _floatOrderBox.Width = 280;
        _floatOrderBox.DisplayMember = "Display";
        _floatWriteButton.Text = "Write Float";
        _floatWriteButton.Width = 200;
        _floatWriteButton.Click += OnWriteFloat;
        _floatStatusLabel.AutoSize = true;
        _floatPanel.Controls.Add(Labeled("Address (2 registers)", _floatAddressBox));
        _floatPanel.Controls.Add(Labeled("Float value", _floatValueBox));
        _floatPanel.Controls.Add(Labeled("Word order", _floatOrderBox));
        _floatPanel.Controls.Add(_floatWriteButton);
        _floatPanel.Controls.Add(_floatStatusLabel);
        _floatPage.Controls.Add(_floatPanel);

        _statusLabel.Text = "Stopped";
        _status.Items.Add(_statusLabel);

        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1000, 650);
        Controls.Add(_tabs);
        Controls.Add(_settingsPanel);
        Controls.Add(_status);
        Name = "MainForm";
        Text = "NovaModbus Slave";
        _settingsPanel.ResumeLayout(false);
        _settingsFlow.ResumeLayout(false);
        _settingsFlow.PerformLayout();
        _portRow.ResumeLayout(false);
        _portRow.PerformLayout();
        _listenPanel.ResumeLayout(false);
        _listenPanel.PerformLayout();
        _slaveIdRow.ResumeLayout(false);
        _slaveIdRow.PerformLayout();
        _tabs.ResumeLayout(false);
        _holdingPage.ResumeLayout(false);
        _holdingPage.PerformLayout();
        _holdingTop.ResumeLayout(false);
        _holdingTop.PerformLayout();
        _coilsPage.ResumeLayout(false);
        _coilsPage.PerformLayout();
        _coilsTop.ResumeLayout(false);
        _coilsTop.PerformLayout();
        _status.ResumeLayout(false);
        _status.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    private void SetupHoldingGrid()
    {
        _holdingGrid.SuspendLayout();
        _holdingGrid.Dock = DockStyle.Fill;
        _holdingGrid.VirtualMode = true;
        _holdingGrid.AutoGenerateColumns = false;
        _holdingGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
        _holdingGrid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
        _holdingGrid.AllowUserToAddRows = false;
        _holdingGrid.AllowUserToDeleteRows = false;
        _holdingGrid.RowHeadersVisible = false;
        _holdingGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _holdingGrid.MultiSelect = true;
        _holdingGrid.BackgroundColor = SystemColors.Window;
        _holdingGrid.KeyDown += OnHoldingGridKey;
        _holdingGrid.MouseDown += OnHoldingRightClick;
        _holdingGrid.Columns.Add(ReadOnlyTextColumn("Address", 70));
        _holdingGrid.Columns.Add(ReadOnlyTextColumn("AddressHex", 80));
        _holdingGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Value(Dec)",
            Width = 120,
            ReadOnly = false,
        });
        _holdingGrid.Columns.Add(ReadOnlyTextColumn("Value(Hex)", 100));
        _holdingGrid.CellValueNeeded += OnHoldingCellValue;
        _holdingGrid.CellValuePushed += OnHoldingCellPushed;
        _holdingGrid.ResumeLayout();
    }

    private void SetupCoilGrid()
    {
        _coilGrid.SuspendLayout();
        _coilGrid.Dock = DockStyle.Fill;
        _coilGrid.VirtualMode = true;
        _coilGrid.AutoGenerateColumns = false;
        _coilGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
        _coilGrid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
        _coilGrid.AllowUserToAddRows = false;
        _coilGrid.AllowUserToDeleteRows = false;
        _coilGrid.RowHeadersVisible = false;
        _coilGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _coilGrid.MultiSelect = true;
        _coilGrid.BackgroundColor = SystemColors.Window;
        _coilGrid.KeyDown += OnCoilGridKey;
        _coilGrid.CurrentCellDirtyStateChanged += OnCoilGridDirty;
        _coilGrid.Columns.Add(ReadOnlyTextColumn("Address", 70));
        _coilGrid.Columns.Add(ReadOnlyTextColumn("AddressHex", 80));
        _coilGrid.Columns.Add(new DataGridViewCheckBoxColumn
        {
            HeaderText = "On",
            Width = 60,
            ReadOnly = false,
        });
        _coilGrid.CellValueNeeded += OnCoilCellValue;
        _coilGrid.CellValuePushed += OnCoilCellPushed;
        _coilGrid.ResumeLayout();
    }

    private static DataGridViewTextBoxColumn ReadOnlyTextColumn(string header, int width)
    {
        return new DataGridViewTextBoxColumn
        {
            HeaderText = header,
            Width = width,
            ReadOnly = true,
        };
    }

    private static Panel Labeled(string text, Control input)
    {
        Label label = new() { Text = text, AutoSize = true, Location = new Point(0, 0) };
        // The label is often wider than the input; size the panel to fit both.
        int width = Math.Max(input.Width, TextRenderer.MeasureText(text, label.Font).Width);
        Panel panel = new() { Width = width, Height = 44 };
        input.Location = new Point(0, 20);
        panel.Controls.Add(label);
        panel.Controls.Add(input);
        return panel;
    }
}
