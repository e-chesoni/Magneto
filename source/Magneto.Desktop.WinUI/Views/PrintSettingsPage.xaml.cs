using Magneto.Desktop.WinUI.ViewModels;

using Microsoft.UI.Xaml.Controls;

namespace Magneto.Desktop.WinUI.Views;

public sealed partial class PrintSettingsPage : Page
{
    public PrintSettingsViewModel ViewModel
    {
        get;
    }

    public PrintSettingsPage()
    {
        ViewModel = App.GetService<PrintSettingsViewModel>();
        InitializeComponent();
    }
}
