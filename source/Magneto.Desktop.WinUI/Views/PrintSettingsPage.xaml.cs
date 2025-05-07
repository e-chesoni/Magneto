using Magneto.Desktop.WinUI.Core;
using Magneto.Desktop.WinUI.Core.Contracts.Services;
using Magneto.Desktop.WinUI.Core.Models;
using Magneto.Desktop.WinUI.ViewModels;

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Magneto.Desktop.WinUI.Views;

public sealed partial class PrintSettingsPage : Page
{
    public MissionControl? _missionControl { get; set; }
    public PrintSettingsViewModel ViewModel { get; }

    #region Constructor

    /// <summary>
    /// Print Settings Page constructor
    /// </summary>
    public PrintSettingsPage()
    {
        ViewModel = App.GetService<PrintSettingsViewModel>();
        _missionControl = App.GetService<MissionControl>();
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
    }

    #endregion
}
