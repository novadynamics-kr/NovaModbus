namespace NovaModbus.Slave.ViewModels;

/// <summary>Editable coil row writing through to the store.</summary>
public sealed class CoilRow : ViewModelBase
{
    private readonly Action<ushort, bool> _commit;
    private bool _isOn;

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
}
