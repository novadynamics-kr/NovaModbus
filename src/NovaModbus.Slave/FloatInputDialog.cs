using NovaModbus.Slave.ViewModels;

namespace NovaModbus.Slave;

/// <summary>Modal dialog: float value + word order for one holding address.</summary>
public sealed class FloatInputDialog : Form
{
    private readonly TextBox _valueBox;
    private readonly ComboBox _orderBox;
    private readonly Button _okButton;
    private readonly Button _cancelButton;

    /// <summary>Initializes the dialog for the given address.</summary>
    public FloatInputDialog(ushort address, FloatOrderOption current)
    {
        ArgumentNullException.ThrowIfNull(current);
        Text = $"Write Float @ {address}";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(300, 170);

        Label addressLabel = new()
        {
            Text = $"Address: {address} (2 registers)",
            AutoSize = true,
            Location = new Point(12, 12),
        };
        Label valueLabel = new()
        {
            Text = "Float value",
            AutoSize = true,
            Location = new Point(12, 40),
        };
        _valueBox = new TextBox
        {
            Text = "0",
            Location = new Point(12, 60),
            Width = 200,
        };
        Label orderLabel = new()
        {
            Text = "Word order",
            AutoSize = true,
            Location = new Point(12, 90),
        };
        _orderBox = new ComboBox
        {
            DropDownStyle = ComboBoxStyle.DropDownList,
            Location = new Point(12, 110),
            Width = 260,
            DisplayMember = "Display",
        };
        _orderBox.Items.AddRange(FloatOrderOption.All.Cast<object>().ToArray());
        _orderBox.SelectedItem = FloatOrderOption.All.FirstOrDefault(o => o.Order == current.Order)
            ?? FloatOrderOption.All[0];
        _okButton = new Button
        {
            Text = "OK",
            DialogResult = DialogResult.OK,
            Location = new Point(116, 140 - 4),
            Width = 80,
        };
        _okButton.Click += OnOk;
        _cancelButton = new Button
        {
            Text = "Cancel",
            DialogResult = DialogResult.Cancel,
            Location = new Point(202, 140 - 4),
            Width = 80,
        };
        AcceptButton = _okButton;
        CancelButton = _cancelButton;
        Controls.Add(addressLabel);
        Controls.Add(valueLabel);
        Controls.Add(_valueBox);
        Controls.Add(orderLabel);
        Controls.Add(_orderBox);
        Controls.Add(_okButton);
        Controls.Add(_cancelButton);
    }

    /// <summary>Parsed float value (valid when the dialog returns OK).</summary>
    public float FloatValue { get; private set; }

    /// <summary>Selected word order.</summary>
    public FloatOrderOption Order => (FloatOrderOption)_orderBox.SelectedItem!;

    private void OnOk(object? sender, EventArgs e)
    {
        if (!float.TryParse(_valueBox.Text, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out float value))
        {
            MessageBox.Show(this, $"Invalid float: '{_valueBox.Text}'.", Text,
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            DialogResult = DialogResult.None;
            return;
        }

        FloatValue = value;
    }
}
