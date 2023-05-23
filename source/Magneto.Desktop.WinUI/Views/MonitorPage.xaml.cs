using Magneto.Desktop.WinUI.ViewModels;

using Microsoft.UI.Xaml.Controls;

namespace Magneto.Desktop.WinUI.Views;

public sealed partial class MonitorPage : Page
{
    public MonitorViewModel ViewModel
    {
        get;
    }

    public MonitorPage()
    {
        ViewModel = App.GetService<MonitorViewModel>();
        InitializeComponent();
    }

    #region Page Navigation

    private void NavigateToLaserPage_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        Frame.Navigate(typeof(LaserMonitorPage));
    }
    private void NavigateToArgonPage_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        Frame.Navigate(typeof(ArgonMonitorPage));
    }
    private void NavigateToMaterialsPage_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        Frame.Navigate(typeof(MaterialsMonitorPage));
    }

    #endregion
}
