namespace NovaModbus.Slave.ViewModels;

/// <summary>Editable holding-register row writing through to the store.</summary>
public sealed class RegisterRow : ViewModelBase
{
    private readonly Action<ushort, ushort> _commit;
    private string _valueText;

    /// <summary>Initializes a row.</summary>
    public RegisterRow(ushort address, ushort value, Action<ushort, ushort> commit)
    {
        ArgumentNullException.ThrowIfNull(commit);
        Address = address;
        _valueText = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        _commit = commit;
    }

    /// <summary>Register address (read-only).</summary>
    public ushort Address { get; }

    /// <summary>Value text (decimal or 0x hex); commits on valid input.</summary>
    public string ValueText
    {
        get => _valueText;
        set
        {
            string text = value.Trim();
            ushort parsed;
            try
            {
                parsed = text.StartsWith("0x", StringComparison.OrdinalIgnoreCase)
                    ? Convert.ToUInt16(text[2..], 16)
                    : ushort.Parse(text, null);
            }
            catch (Exception ex) when (ex is FormatException or OverflowException)
            {
                // Reject: refresh the grid cell back.
                NotifyPropertyChanged();
                return;
            }

            try
            {
                _commit(Address, parsed);
                SetProperty(ref _valueText, text);
            }
            catch (Exception)
            {
                NotifyPropertyChanged();
            }
        }
    }
}
