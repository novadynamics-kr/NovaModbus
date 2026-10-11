namespace NovaModbus.Client.ViewModels;

/// <summary>Root view model of the client main window.</summary>
public sealed class MainViewModel : ViewModelBase
{
    /// <summary>Serial port settings (left panel).</summary>
    public SerialPortSettingsViewModel SerialSettings { get; } = new();
}
