namespace NovaModbus.Slave;

#nullable disable

partial class MainForm
{
    private System.ComponentModel.IContainer components;
    private Panel _settingsPanel;
    private TabControl _tabs;
    private TabPage _holdingPage;
    private TabPage _coilsPage;
    private TabPage _countersPage;
    private TabPage _logPage;
    private TabPage _floatPage;
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
        _tabs = new TabControl();
        _holdingPage = new TabPage();
        _coilsPage = new TabPage();
        _countersPage = new TabPage();
        _logPage = new TabPage();
        _floatPage = new TabPage();
        _status = new StatusStrip();
        _statusLabel = new ToolStripStatusLabel();
        _status.SuspendLayout();
        _tabs.SuspendLayout();
        SuspendLayout();

        _settingsPanel.Dock = DockStyle.Left;
        _settingsPanel.Width = 270;
        _settingsPanel.Padding = new Padding(8);
        _settingsPanel.BackColor = SystemColors.Control;

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
        _status.ResumeLayout(false);
        _status.PerformLayout();
        _tabs.ResumeLayout(false);
        ResumeLayout(false);
        PerformLayout();
    }
}
