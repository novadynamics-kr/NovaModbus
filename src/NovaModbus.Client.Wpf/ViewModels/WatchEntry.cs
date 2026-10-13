using System.ComponentModel;

namespace NovaModbus.Client.Wpf.ViewModels;

using NovaModbus.Rtu.Collections;

/// <summary>One polled address range with selection support.</summary>
public sealed class WatchEntry : INotifyPropertyChanged, IEquatable<WatchEntry>, ISelectable
{
    private bool _isSelected;
    private bool _suppressSelectionNotification;

    public WatchEntry(byte slaveId, WatchArea area, ushort start, ushort count, string extra = "")
    {
        SlaveId = slaveId;
        Area = area;
        Start = start;
        Count = count;
        Extra = extra;
    }

    public byte SlaveId { get; }
    public WatchArea Area { get; }
    public ushort Start { get; }
    public ushort Count { get; }
    public string Extra { get; }
    public string AddressHex => $"0x{Start:X4}";

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
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsSelected)));
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

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool Equals(WatchEntry? other)
    {
        if (other is null) return false;
        return SlaveId == other.SlaveId && Area == other.Area && Start == other.Start && Count == other.Count && Extra == other.Extra;
    }

    public override bool Equals(object? obj) => Equals(obj as WatchEntry);

    public override int GetHashCode() => HashCode.Combine(SlaveId, Area, Start, Count, Extra);
}
