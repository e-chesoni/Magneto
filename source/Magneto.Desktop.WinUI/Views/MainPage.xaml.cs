﻿using Magneto.Desktop.WinUI.Core;
using Magneto.Desktop.WinUI.Core.Contracts.Services;
using Magneto.Desktop.WinUI.Services;
using Magneto.Desktop.WinUI.ViewModels;

using Microsoft.UI.Xaml.Controls;
using Windows.Devices.Enumeration;
using WinUIEx.Messaging;

namespace Magneto.Desktop.WinUI.Views;

public sealed partial class MainPage : Page
{
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
    private void NavigateToNewPrintPage_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        Frame.Navigate(typeof(NewPrintPage));
    }

    private void NavigateToPrintingHistoryPage_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        Frame.Navigate(typeof(PrintingHistoryPage));
    }

    /// <summary>
    /// Pass mission control to print page when monitor page button is clicked
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void NavigateToMonitorPage_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        Frame.Navigate(typeof(MonitorPage));
    }

    /// <summary>
    /// Pass mission control to print page when settings page button is clicked
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void NavigateToSettingsPage_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        Frame.Navigate(typeof(SettingsPage));
    }

    /// <summary>
    /// Pass mission control to print page when print queue button is clicked
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void NavigateToMaintenancePage_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        Frame.Navigate(typeof(MaintenancePage));
    }

    /// <summary>
    /// Pass mission control to print page when utilities page button is clicked
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void NavigateToTestingPage_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        Frame.Navigate(typeof(TestingPage));
    }

    #endregion
}
