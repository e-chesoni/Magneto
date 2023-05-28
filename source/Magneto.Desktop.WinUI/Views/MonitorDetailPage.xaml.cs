using CommunityToolkit.WinUI.UI.Animations;

using Magneto.Desktop.WinUI.Contracts.Services;
using Magneto.Desktop.WinUI.Core.Contracts.Services;
using Magneto.Desktop.WinUI.Core.Models;
using Magneto.Desktop.WinUI.Core.Services;
using Magneto.Desktop.WinUI.ViewModels;

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Magneto.Desktop.WinUI.Views;

public sealed partial class MonitorDetailPage : Page
{
    #region Public Variable

    /// <summary>
    /// Store "global" mission control on this page
    /// </summary>
    public MissionControl MissionControl { get; set; }

    /// <summary>
    /// Page view model
    /// </summary>
    public MonitorDetailViewModel ViewModel { get; }

    #endregion

    #region Constructor

    /// <summary>
    /// Monitor Details Page constructor
    /// </summary>
    public MonitorDetailPage()
    {
        ViewModel = App.GetService<MonitorDetailViewModel>();
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
        base.OnNavigatedTo(e);
        this.RegisterElementForConnectedAnimation("animationKeyContentGrid", itemHero);

        // Get mission control (passed over when navigating from previous page)
        MissionControl = (MissionControl)e.Parameter;

        var msg = string.Format("MonitorDetailPage::OnNavigatedTo -- {0}", MissionControl.FriendlyMessage);
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.DEBUG);
    }

    /// <summary>
    /// Handle page tasks when page is navigated from
    /// </summary>
    /// <param name="e"></param>
    protected override void OnNavigatingFrom(NavigatingCancelEventArgs e)
    {
        base.OnNavigatingFrom(e);
        if (e.NavigationMode == NavigationMode.Back)
        {
            var navigationService = App.GetService<INavigationService>();

            if (ViewModel.Item != null)
            {
                navigationService.SetListDataItemForNextConnectedAnimation(ViewModel.Item);
            }
        }
    }

    #endregion
}
