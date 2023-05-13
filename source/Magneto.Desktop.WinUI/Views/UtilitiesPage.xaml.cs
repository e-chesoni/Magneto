using Magneto.Desktop.WinUI.ViewModels;

using Microsoft.UI.Xaml.Controls;

namespace Magneto.Desktop.WinUI.Views;

public sealed partial class UtilitiesPage : Page
{
    public UtilitiesViewModel ViewModel
    {
        get;
    }

    public UtilitiesPage()
    {
        ViewModel = App.GetService<UtilitiesViewModel>();
        InitializeComponent();
    }
}
