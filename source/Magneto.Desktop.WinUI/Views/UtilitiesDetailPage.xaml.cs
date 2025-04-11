using CommunityToolkit.WinUI.UI.Animations;

using Magneto.Desktop.WinUI.Contracts.Services;
using Magneto.Desktop.WinUI.Core;
using Magneto.Desktop.WinUI.Core.Contracts.Services;
using Magneto.Desktop.WinUI.Core.Models;
using Magneto.Desktop.WinUI.ViewModels;

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Magneto.Desktop.WinUI.Views;

public sealed partial class UtilitiesDetailPage : Page
{
    #region Public Methods
    /// <summary>
    /// Store "global" mission control on this page
    /// </summary>
    public MissionControl? MissionControl { get; set; }

    /// <summary>
    /// Page view model
    /// </summary>
    public UtilitiesDetailViewModel ViewModel { get; }

    #endregion

    #region Constructor

    /// <summary>
    /// Utilities Detail Page constructor 
    /// </summary>
    public UtilitiesDetailPage()
    {
        ViewModel = App.GetService<UtilitiesDetailViewModel>();
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

        var msg = string.Format("UtilitiesDetailPage::OnNavigatedTo -- {0}", MissionControl.FriendlyMessage);
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.DEBUG);

    }

    /// <summary>
    /// Handle tasks when page is left
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
