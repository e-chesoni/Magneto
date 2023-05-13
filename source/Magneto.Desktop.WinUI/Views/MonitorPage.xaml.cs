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
}
