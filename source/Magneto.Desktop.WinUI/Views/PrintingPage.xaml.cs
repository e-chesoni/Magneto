using Magneto.Desktop.WinUI.ViewModels;

using Microsoft.UI.Xaml.Controls;

namespace Magneto.Desktop.WinUI.Views;

public sealed partial class PrintingPage : Page
{
    public PrintingViewModel ViewModel
    {
        get;
    }

    public PrintingPage()
    {
        ViewModel = App.GetService<PrintingViewModel>();
        InitializeComponent();
    }
}
