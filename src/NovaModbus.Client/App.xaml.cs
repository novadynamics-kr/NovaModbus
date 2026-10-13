using Microsoft.UI.Xaml;

namespace NovaModbus.Client;

/// <summary>Application entry.</summary>
public partial class App : Application
{
    private Window? _window;

    /// <summary>Initializes the application.</summary>
    public App()
    {
        InitializeComponent();
    }

    /// <summary>Invoked when the application is launched.</summary>
    /// <param name="args">Launch arguments.</param>
    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        _window = new MainWindow();
        _window.Activate();
    }
}
