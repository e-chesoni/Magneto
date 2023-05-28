using Magneto.Desktop.WinUI.Core.Contracts.Services;
using Magneto.Desktop.WinUI.Core.Models;
using Magneto.Desktop.WinUI.Core.Services;
using Magneto.Desktop.WinUI.ViewModels;

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Magneto.Desktop.WinUI.Views;

public sealed partial class LaserMonitorPage : Page
{
    #region Public Variable

    /// <summary>
    /// Store "global" mission control on this page
    /// </summary>
    public MissionControl MissionControl { get; set; }

    /// <summary>
    /// Page view model
    /// </summary>
    public LaserMonitorViewModel ViewModel { get; }

    #endregion

    #region Constructor

    /// <summary>
    /// Laser Monitor constructor
    /// </summary>
    public LaserMonitorPage()
    {
        ViewModel = App.GetService<LaserMonitorViewModel>();
        InitializeComponent();
    }

    #endregion

    #region Navigation Methods

    /// <summary>
    /// Handle page startup tasks
    /// </summary>
    /// <param name="e"></param>
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        // Get mission control (passed over when navigating from previous page)
        base.OnNavigatedTo(e);
        MissionControl = (MissionControl)e.Parameter;

        var msg = string.Format("LaserMonitorPage::OnNavigatedTo -- {0}", MissionControl.FriendlyMessage);
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.DEBUG);
    }

    #endregion
}
