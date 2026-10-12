namespace NovaModbus.Slave.ViewModels;

using NovaModbus.Rtu.Collections;

/// <summary>Editable holding-register row writing through to the store.</summary>
public sealed class RegisterRow : ViewModelBase, ISelectable
{
    private readonly Action<ushort, ushort> _commit;
    private string _valueText;
    private bool _isSelected;
    private bool _suppressSelectionNotification;

    /// <summary>Initializes a row.</summary>
    public RegisterRow(ushort address, ushort value, Action<ushort, ushort> commit)
    {
        ArgumentNullException.ThrowIfNull(commit);
        Address = address;
        _valueText = value.ToString(System.Globalization.CultureInfo.InvariantCulture);
        _hexText = ToHex(value);
        _commit = commit;
    }

    /// <summary>Register address (read-only).</summary>
    public ushort Address { get; }

    /// <summary>Address as hex (read-only, e.g. 0x000A).</summary>
    public string AddressHex => $"0x{Address:X4}";

    /// <summary>Hex value (read-only, e.g. 0x022B).</summary>
    public string HexText
    {
        get => _hexText;
        private set => SetProperty(ref _hexText, value);
    }

    private string _hexText;

    private static string ToHex(ushort value) => $"0x{value:X4}";

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
                HexText = ToHex(parsed);
            }
            catch (Exception)
            {
                NotifyPropertyChanged();
            }
        }
    }

    /// <summary>Selection state for batched UI selection.</summary>
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected != value)
            {
                _isSelected = value;
                if (!_suppressSelectionNotification)
                {
                    NotifyPropertyChanged(nameof(IsSelected));
                }
            }
        }
    }

    /// <inheritdoc />
    public bool SuppressSelectionNotification
    {
        get => _suppressSelectionNotification;
        set => _suppressSelectionNotification = value;
    }
}
