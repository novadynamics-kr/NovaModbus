namespace NovaModbus.SlaveWPF.ViewModels;

using NovaModbus.Rtu.Collections;

/// <summary>Editable coil row writing through to the store.</summary>
public sealed class CoilRow : ViewModelBase, ISelectable
{
    private readonly Action<ushort, bool> _commit;
    private bool _isOn;
    private bool _isSelected;
    private bool _suppressSelectionNotification;

    /// <summary>Initializes a row.</summary>
    public CoilRow(ushort address, bool isOn, Action<ushort, bool> commit)
    {
        ArgumentNullException.ThrowIfNull(commit);
        Address = address;
        _isOn = isOn;
        _commit = commit;
    }

    /// <summary>Coil address (read-only).</summary>
    public ushort Address { get; }

    /// <summary>Coil state; commits on change.</summary>
    public bool IsOn
    {
        get => _isOn;
        set
        {
            bool previous = _isOn;
            if (!SetProperty(ref _isOn, value))
            {
                return;
            }

            try
            {
                _commit(Address, value);
            }
            catch (Exception)
            {
                _isOn = previous;
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
