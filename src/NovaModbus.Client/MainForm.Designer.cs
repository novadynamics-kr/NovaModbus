namespace NovaModbus.Client;

#nullable disable

partial class MainForm
{
    private System.ComponentModel.IContainer components;
    private Panel _settingsPanel = null!;
    private TabControl _tabs = null!;
    private TabPage _readWritePage = null!;
    private TabPage _pollingPage = null!;
    private TabPage _logPage = null!;
    private TabPage _watchPage = null!;
    private StatusStrip _status = null!;
    private ToolStripStatusLabel _statusLabel = null!;

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
        _readWritePage = new TabPage();
        _pollingPage = new TabPage();
        _logPage = new TabPage();
        _watchPage = new TabPage();
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
        _readWritePage.Text = "Read / Write";
        _readWritePage.UseVisualStyleBackColor = true;
        _pollingPage.Text = "Polling";
        _pollingPage.UseVisualStyleBackColor = true;
        _logPage.Text = "Log";
        _logPage.UseVisualStyleBackColor = true;
        _watchPage.Text = "Watch";
        _watchPage.UseVisualStyleBackColor = true;
        _tabs.TabPages.AddRange([_readWritePage, _pollingPage, _logPage, _watchPage]);

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
        _status.ResumeLayout(false);
        _status.PerformLayout();
        _tabs.ResumeLayout(false);
        ResumeLayout(false);
        PerformLayout();
    }
}
