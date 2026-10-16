using System.Globalization;
using System.Windows;
using NovaModbus.SlaveWPF.ViewModels;

namespace NovaModbus.SlaveWPF.Views;

/// <summary>Float write dialog for one holding address.</summary>
public partial class FloatInputDialog : Window
{
    /// <summary>Initializes the dialog for the given address.</summary>
    public FloatInputDialog(ushort address)
    {
        InitializeComponent();
        Address = address;
        AddressText.Text = $"Address {address} (2 registers)";
        OrderBox.ItemsSource = FloatOrderOption.All;
        OrderBox.SelectedIndex = 0;
        ValueBox.Focus();
    }

    /// <summary>Target address.</summary>
    public ushort Address { get; }

    /// <summary>Entered value (valid after OK).</summary>
    public float Value { get; private set; }

    /// <summary>Selected order (valid after OK).</summary>
    public FloatOrderOption Order { get; private set; } = FloatOrderOption.All[0];

    private void OnOk(object sender, RoutedEventArgs e)
    {
        if (!float.TryParse(ValueBox.Text, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
        {
            ErrorText.Text = $"Invalid float: '{ValueBox.Text}'.";
            return;
        }

        if (OrderBox.SelectedItem is not FloatOrderOption order)
        {
            ErrorText.Text = "Select word order.";
            return;
        }

        Value = value;
        Order = order;
        DialogResult = true;
    }
}
