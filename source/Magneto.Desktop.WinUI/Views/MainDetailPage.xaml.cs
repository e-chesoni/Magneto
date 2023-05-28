using CommunityToolkit.WinUI.UI.Animations;

using Magneto.Desktop.WinUI.Contracts.Services;
using Magneto.Desktop.WinUI.ViewModels;

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Magneto.Desktop.WinUI.Views;

public sealed partial class MainDetailPage : Page
{
    #region Public Variables

    /// <summary>
    /// Page view model
    /// </summary>
    public MainDetailViewModel ViewModel { get; }

    #endregion

    #region Constructor

    /// <summary>
    /// Main Detail Page constructor
    /// </summary>
    public MainDetailPage()
    {
        ViewModel = App.GetService<MainDetailViewModel>();
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
    }

    /// <summary>
    /// Handle task when page is navigated from
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
