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

    #region Page Navigation

    private void NavigateToCleaningPage_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        Frame.Navigate(typeof(CleaningPage));
    }
    private void NavigateToTestPrintPage_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        Frame.Navigate(typeof(TestPrintPage));
    }

    #endregion
}
