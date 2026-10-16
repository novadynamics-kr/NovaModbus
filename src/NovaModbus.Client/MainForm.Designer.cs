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
    private TabPage _logPage;
    private TabPage _watchPage;
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
        _logPage = new TabPage();
        _watchPage = new TabPage();
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

    private static Panel Labeled(string text, Control input)
    {
        Panel panel = new() { AutoSize = true };
        Label label = new() { Text = text, Dock = DockStyle.Top, AutoSize = true };
        input.Dock = DockStyle.Top;
        panel.Controls.Add(input);
        panel.Controls.Add(label);
        return panel;
    }
}
