using Magneto.Desktop.WinUI.Core.Contracts.Services;
using Magneto.Desktop.WinUI.Core.Services;
using Magneto.Desktop.WinUI.ViewModels;

using Microsoft.UI.Xaml.Controls;
using Windows.Devices.Enumeration;
using WinUIEx.Messaging;

namespace Magneto.Desktop.WinUI.Views;

public sealed partial class MainPage : Page
{
    //MagnetoLogger _magnetoLogger = new MagnetoLogger();

    private void InitializeMagneto()
    {
        // Set log level
        MagnetoLogger.LogFactoryOutputLevel = LogFactoryOutputLevel.LogOutputLevel.Debug;

        // Set up serial console
        MagnetoSerialConsole.SetDefaultSerialPort();
    }

    public MainViewModel ViewModel
    {
        get;
    }

    public MainPage()
    {
        ViewModel = App.GetService<MainViewModel>();
        InitializeComponent();
        InitializeMagneto();

        // Print some log messages for testing
        MagnetoLogger.Log("PRINTING SAMPLE LOG MESSAGES", LogFactoryLogLevel.LogLevel.VERBOSE);
        MagnetoLogger.Log("This is a debug message", LogFactoryLogLevel.LogLevel.DEBUG);
        MagnetoLogger.Log("This is a verbose message", LogFactoryLogLevel.LogLevel.VERBOSE);
        MagnetoLogger.Log("This is a warning message", LogFactoryLogLevel.LogLevel.WARN);
        MagnetoLogger.Log("This is a error message", LogFactoryLogLevel.LogLevel.ERROR);
        MagnetoLogger.Log("This is a success message", LogFactoryLogLevel.LogLevel.SUCCESS);

    }

    #region Page Navigation
    private void NavigateToPrintPage_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        Frame.Navigate(typeof(PrintPage));
    }

    private void NavigateToMonitorPage_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        Frame.Navigate(typeof(MonitorPage));
    }

    private void NavigateToSettingsPage_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        Frame.Navigate(typeof(SettingsPage));
    }

    private void NavigateToPrintQueuePage_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        Frame.Navigate(typeof(PrintQueuePage));
    }

    private void NavigateToUtilitiesPage_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        Frame.Navigate(typeof(UtilitiesPage));
    }

    #endregion
}
