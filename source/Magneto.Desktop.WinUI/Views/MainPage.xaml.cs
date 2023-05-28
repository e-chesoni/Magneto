using Magneto.Desktop.WinUI.Core.Contracts.Services;
using Magneto.Desktop.WinUI.Core.Services;
using Magneto.Desktop.WinUI.Services;
using Magneto.Desktop.WinUI.ViewModels;

using Microsoft.UI.Xaml.Controls;
using Windows.Devices.Enumeration;
using WinUIEx.Messaging;

namespace Magneto.Desktop.WinUI.Views;

public sealed partial class MainPage : Page
{
    #region Private Variables

    /// <summary>
    /// Boolean to indicate whether to call InitializeMagneto when page loads
    /// </summary>
    private bool _initialPageLoaded = false;

    /// <summary>
    /// Tasks to handle when application starts up
    /// TODO: May want to store in an "App Startup" class in the future
    /// </summary>
    private void InitializeMagneto()
    {
        // Set log level
        MagnetoLogger.LogFactoryOutputLevel = LogFactoryOutputLevel.LogOutputLevel.Debug;

        // Set up serial console
        MagnetoSerialConsole.SetDefaultSerialPort();

        // Set initial page loaded to true
        _initialPageLoaded = true;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Page view model
    /// </summary>
    public MainViewModel ViewModel { get; }

    #endregion

    #region Constructor

    /// <summary>
    /// Main Page constructor
    /// </summary>
    public MainPage()
    {
        ViewModel = App.GetService<MainViewModel>();
        InitializeComponent();
        if (!_initialPageLoaded) { InitializeMagneto(); }
        
        // Print some log messages for testing
        MagnetoLogger.Log("PRINTING SAMPLE LOG MESSAGES", LogFactoryLogLevel.LogLevel.VERBOSE);
        MagnetoLogger.Log("This is a debug message", LogFactoryLogLevel.LogLevel.DEBUG);
        MagnetoLogger.Log("This is a verbose message", LogFactoryLogLevel.LogLevel.VERBOSE);
        MagnetoLogger.Log("This is a warning message", LogFactoryLogLevel.LogLevel.WARN);
        MagnetoLogger.Log("This is a error message", LogFactoryLogLevel.LogLevel.ERROR);
        MagnetoLogger.Log("This is a success message", LogFactoryLogLevel.LogLevel.SUCCESS);

    }
    
    #endregion

    #region Page Navigation

    /// <summary>
    /// Pass mission control to print page when print page button is clicked
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void NavigateToPrintPage_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        Frame.Navigate(typeof(PrintPage), ViewModel.missionControl);
    }

    /// <summary>
    /// Pass mission control to print page when monitor page button is clicked
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void NavigateToMonitorPage_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        Frame.Navigate(typeof(MonitorPage), ViewModel.missionControl);
    }

    /// <summary>
    /// Pass mission control to print page when settings page button is clicked
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void NavigateToSettingsPage_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        Frame.Navigate(typeof(SettingsPage), ViewModel.missionControl);
    }

    /// <summary>
    /// Pass mission control to print page when print queue button is clicked
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void NavigateToPrintQueuePage_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        Frame.Navigate(typeof(PrintQueuePage), ViewModel.missionControl);
    }

    /// <summary>
    /// Pass mission control to print page when utilities page button is clicked
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void NavigateToUtilitiesPage_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        Frame.Navigate(typeof(UtilitiesPage), ViewModel.missionControl);
    }

    #endregion
}
