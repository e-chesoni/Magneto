using CommunityToolkit.WinUI.UI.Animations;
using Magneto.Desktop.WinUI.Core.Contracts.Services;
using Magneto.Desktop.WinUI.Core.Models;
using Magneto.Desktop.WinUI.Core.Services;
using Magneto.Desktop.WinUI.ViewModels;

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Magneto.Desktop.WinUI.Views;

// TODO: Change the grid as appropriate for your app. Adjust the column definitions on DataGridPage.xaml.
// For more details, see the documentation at https://docs.microsoft.com/windows/communitytoolkit/controls/datagrid.
public sealed partial class CompletedPrintsPage : Page
{
    #region Public Variables

    /// <summary>
    /// Store "global" mission control on this page
    /// </summary>
    public MissionControl MissionControl { get; set; }

    /// <summary>
    /// Page view model
    /// </summary>
    public CompletedPrintsViewModel ViewModel { get; }

    #endregion

    #region Constructor

    /// <summary>
    /// Completed Print Page constructor
    /// </summary>
    public CompletedPrintsPage()
    {
        ViewModel = App.GetService<CompletedPrintsViewModel>();
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

        var msg = string.Format("CompletedPrintsPage::OnNavigatedTo -- {0}", MissionControl.FriendlyMessage);
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.DEBUG);
    }

    #endregion
}
