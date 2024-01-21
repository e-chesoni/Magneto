using Magneto.Desktop.WinUI.Core.Contracts.Services;
using Magneto.Desktop.WinUI.Core.Models;
using Magneto.Desktop.WinUI.Core.Services;
using Magneto.Desktop.WinUI.ViewModels;

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Magneto.Desktop.WinUI.Views;

public sealed partial class MonitorPage : Page
{
    #region Public Variables

    /// <summary>
    /// Store "global" mission control on this page
    /// </summary>
    public MissionControl? MissionControl { get; set; }

    /// <summary>
    /// Page view model
    /// </summary>
    public MonitorViewModel ViewModel { get; }

    #endregion

    #region Constructor

    /// <summary>
    /// Monitor Page constructor
    /// </summary>
    public MonitorPage()
    {
        ViewModel = App.GetService<MonitorViewModel>();
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

        var msg = string.Format("MonitorPage::OnNavigatedTo -- {0}", MissionControl.FriendlyMessage);
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.DEBUG);
    }

    #endregion

    #region Button Methods

    /// <summary>
    /// Pass mission control to the laser monitor page when button is clicked
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void NavigateToLaserPage_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        Frame.Navigate(typeof(LaserMonitorPage), MissionControl);
    }

    /// <summary>
    /// Pass mission control to the argon monitor page when button is clicked
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void NavigateToArgonPage_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        Frame.Navigate(typeof(ArgonMonitorPage), MissionControl);
    }

    /// <summary>
    /// Pass mission control to the materials monitor page when button is clicked
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void NavigateToMaterialsPage_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        Frame.Navigate(typeof(MaterialsMonitorPage), MissionControl);
    }

    #endregion
}
