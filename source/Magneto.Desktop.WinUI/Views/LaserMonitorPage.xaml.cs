using Magneto.Desktop.WinUI.ViewModels;

using Microsoft.UI.Xaml.Controls;

namespace Magneto.Desktop.WinUI.Views;

public sealed partial class LaserMonitorPage : Page
{
    public LaserMonitorViewModel ViewModel
    {
        get;
    }

    public LaserMonitorPage()
    {
        ViewModel = App.GetService<LaserMonitorViewModel>();
        InitializeComponent();
    }
}
