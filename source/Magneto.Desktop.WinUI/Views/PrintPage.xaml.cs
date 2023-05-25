using Magneto.Desktop.WinUI.Core.Models.Image;
using Magneto.Desktop.WinUI.ViewModels;

using Microsoft.UI.Xaml.Controls;

namespace Magneto.Desktop.WinUI.Views;

public sealed partial class PrintPage : Page
{
    public ImageModel _currentImage = new ImageModel();

    public PrintViewModel ViewModel
    {
        get;
    }

    public PrintPage()
    {
        ViewModel = App.GetService<PrintViewModel>();
        InitializeComponent();
    }

    private void FindPrint_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        string path_to_image = "c:/path/to/image";

        // Add dummy string to textbox
        SelectedPrint.Text = path_to_image;

        // Generate fake im to get things going
        _currentImage.path_to_image = path_to_image;
    }

    private void StartPrint_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        ViewModel.missionControl.StartPrint(_currentImage);
    }
}
