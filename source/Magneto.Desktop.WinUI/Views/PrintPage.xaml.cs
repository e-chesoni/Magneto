using Magneto.Desktop.WinUI.ViewModels;

using Microsoft.UI.Xaml.Controls;

namespace Magneto.Desktop.WinUI.Views;

public sealed partial class PrintPage : Page
{
    public PrintViewModel ViewModel
    {
        get;
    }

    public PrintPage()
    {
        ViewModel = App.GetService<PrintViewModel>();
        InitializeComponent();
    }
}
