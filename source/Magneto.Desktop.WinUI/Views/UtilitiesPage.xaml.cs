using Magneto.Desktop.WinUI.Core.Contracts.Services;
using Magneto.Desktop.WinUI.Core.Models;
using Magneto.Desktop.WinUI.Core.Services;
using Magneto.Desktop.WinUI.ViewModels;

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Magneto.Desktop.WinUI.Views;

/// <summary>
/// Utilities page
/// </summary>
public sealed partial class UtilitiesPage : Page
{
    #region Public Variables

    /// <summary>
    /// Store "global" mission control on this page
    /// </summary>
    public MissionControl MissionControl { get; set; }

    /// <summary>
    /// Page view model
    /// </summary>
    public UtilitiesViewModel ViewModel { get; }

    #endregion

    #region Constructor

    /// <summary>
    /// UtilitiesPage constructor
    /// </summary>
    public UtilitiesPage()
    {
        ViewModel = App.GetService<UtilitiesViewModel>();
        InitializeComponent();
    }

    #endregion

    #region Page Navigation

    /// <summary>
    /// Handle page startup tasks
    /// </summary>
    /// <param name="e"></param>
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        // Get mission control (passed over when navigating from previous page)
        base.OnNavigatedTo(e);
        MissionControl = (MissionControl)e.Parameter;

        var msg = string.Format("UtilitiesPage::OnNavigatedTo -- {0}", MissionControl.FriendlyMessage);
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.DEBUG);
    }

    /// <summary>
    /// Pass Mission Control to Cleaning Page when button is clicked
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void NavigateToCleaningPage_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        Frame.Navigate(typeof(CleaningPage), MissionControl);
    }

    /// <summary>
    /// Pass Mission Control to Test Print Page when button is clicked
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void NavigateToTestPrintPage_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        Frame.Navigate(typeof(TestPrintPage), MissionControl);
    }

    /// <summary>
    /// Pass Mission Control to Test WaveRunner Page when button is clicked
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void NavigateToTestWaveRunnerPage_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        Frame.Navigate(typeof(TestWaveRunner), MissionControl);
    }

    #endregion


}
