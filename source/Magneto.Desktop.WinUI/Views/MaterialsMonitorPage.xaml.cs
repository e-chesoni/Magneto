using Magneto.Desktop.WinUI.ViewModels;

using Microsoft.UI.Xaml.Controls;

namespace Magneto.Desktop.WinUI.Views;

public sealed partial class MaterialsMonitorPage : Page
{
    public MaterialsMonitorViewModel ViewModel
    {
        get;
    }

    public MaterialsMonitorPage()
    {
        ViewModel = App.GetService<MaterialsMonitorViewModel>();
        InitializeComponent();
    }
}
