using Magneto.Desktop.WinUI.ViewModels;

using Microsoft.UI.Xaml.Controls;

namespace Magneto.Desktop.WinUI.Views;

public sealed partial class ArgonMonitorPage : Page
{
    public ArgonMonitorViewModel ViewModel
    {
        get;
    }

    public ArgonMonitorPage()
    {
        ViewModel = App.GetService<ArgonMonitorViewModel>();
        InitializeComponent();
    }
}
