using Magneto.Desktop.WinUI.Core.Contracts.Services;
using Magneto.Desktop.WinUI.Core.Models;
using Magneto.Desktop.WinUI.Core.Services;
using Magneto.Desktop.WinUI.ViewModels;

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Magneto.Desktop.WinUI.Views;

public sealed partial class MonitorPage : Page
{
    public MissionControl MissionControl { get; set; }

    public MonitorViewModel ViewModel { get; }

    public MonitorPage()
    {
        ViewModel = App.GetService<MonitorViewModel>();
        InitializeComponent();
    }

    #region Page Navigation

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        // Get mission control (passed over when navigating from previous page)
        base.OnNavigatedTo(e);
        MissionControl = (MissionControl)e.Parameter;

        var msg = string.Format("MonitorPage::OnNavigatedTo -- {0}", MissionControl.FriendlyMessage);
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.DEBUG);
    }

    private void NavigateToLaserPage_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        Frame.Navigate(typeof(LaserMonitorPage), MissionControl);
    }
    private void NavigateToArgonPage_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        Frame.Navigate(typeof(ArgonMonitorPage), MissionControl);
    }
    private void NavigateToMaterialsPage_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        Frame.Navigate(typeof(MaterialsMonitorPage), MissionControl);
    }

    #endregion
}
