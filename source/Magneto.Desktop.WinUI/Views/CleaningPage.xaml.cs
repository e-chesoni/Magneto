using Magneto.Desktop.WinUI.ViewModels;

using Microsoft.UI.Xaml.Controls;

namespace Magneto.Desktop.WinUI.Views;

public sealed partial class CleaningPage : Page
{
    public CleaningViewModel ViewModel
    {
        get;
    }

    public CleaningPage()
    {
        ViewModel = App.GetService<CleaningViewModel>();
        InitializeComponent();
    }
}
