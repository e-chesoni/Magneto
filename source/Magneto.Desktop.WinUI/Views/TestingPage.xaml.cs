using Magneto.Desktop.WinUI.Core;
using Magneto.Desktop.WinUI.Core.Contracts.Services;
using Magneto.Desktop.WinUI.Core.Models;
using Magneto.Desktop.WinUI.ViewModels;

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Windows.UI.ViewManagement;

namespace Magneto.Desktop.WinUI.Views;

/// <summary>
/// Utilities page
/// </summary>
public sealed partial class TestingPage : Page
{
    #region Public Variables

    /// <summary>
    /// Store "global" mission control on this page
    /// </summary>
    public MissionControl? _missionControl { get; set; }

    /// <summary>
    /// Page view model
    /// </summary>
    public TestingViewModel ViewModel { get; }

    #endregion

    #region Constructor

    /// <summary>
    /// UtilitiesPage constructor
    /// </summary>
    public TestingPage()
    {
        ViewModel = App.GetService<TestingViewModel>();
        _missionControl = App.GetService<MissionControl>();
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
        //_missionControl = (MissionControl)e.Parameter;

        //var msg = string.Format("UtilitiesPage::OnNavigatedTo -- {0}", MissionControl.FriendlyMessage);
        //MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.DEBUG);
    }

    /// <summary>
    /// Pass Mission Control to Test Print Page when button is clicked
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void NavigateToTestPrintPage_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        Frame.Navigate(typeof(TestPrintPage), _missionControl);
    }

    /// <summary>
    /// Pass Mission Control to Test WaveRunner Page when button is clicked
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void NavigateToTestWaveRunnerPage_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        Frame.Navigate(typeof(TestWaveRunner), _missionControl);
    }

    /// <summary>
    /// Pass Mission Control to Test Motors Page when button is clicked
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void NavigateToTestMotorsrPage_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        Frame.Navigate(typeof(TestMotorsPage), _missionControl);
    }

    #endregion


}
