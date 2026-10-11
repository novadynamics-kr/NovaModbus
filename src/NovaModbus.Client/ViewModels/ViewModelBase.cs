using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace NovaModbus.Client.ViewModels;

/// <summary>Manual INotifyPropertyChanged base (no external MVVM toolkit).</summary>
public abstract class ViewModelBase : INotifyPropertyChanged
{
    /// <inheritdoc />
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Set field and raise PropertyChanged when changed.</summary>
    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        return true;
    }
}
