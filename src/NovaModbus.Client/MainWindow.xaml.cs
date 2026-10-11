using System.Windows;
using NovaModbus.Client.ViewModels;

namespace NovaModbus.Client;

/// <summary>Interaction logic for MainWindow.xaml.</summary>
public partial class MainWindow : Window
{
    /// <summary>Initializes the window with its root view model.</summary>
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel();
    }
}
