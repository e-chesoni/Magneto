using System.Xml.Linq;
using Magneto.Desktop.WinUI.Core.Contracts.Services;
using Magneto.Desktop.WinUI.Core.Models;
using Magneto.Desktop.WinUI.Core.Models.Image;
using Magneto.Desktop.WinUI.Core.Models.Motor;
using Magneto.Desktop.WinUI.Core.Services;
using Magneto.Desktop.WinUI.ViewModels;

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Magneto.Desktop.WinUI.Views;

public sealed partial class PrintPage : Page
{
    public ImageModel _currentImage = new();

    public MissionControl MissionControl;



    public PrintViewModel ViewModel
    {
        get;
    }

    public PrintPage()
    {
        ViewModel = App.GetService<PrintViewModel>();
        InitializeComponent(); // This is fine...not sure why there are red lines sometimes
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        MissionControl = (MissionControl)e.Parameter; // get parameter
        MagnetoLogger.Log(MissionControl.FriendlyMessage, LogFactoryLogLevel.LogLevel.DEBUG);
    }

    private void FindPrint_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        string path_to_image = "c:/path/to/image";

        // Add dummy string to text box
        // SelectedPrint is the name of the TextBox in PrintPage.xaml
        SelectedPrint.Text = path_to_image; // This is fine...not sure why there are red lines sometimes

        // Generate fake image manager to get things going
        _currentImage.path_to_image = path_to_image;

        // TODO: Initialize Print
        if (string.IsNullOrEmpty(LayerThickness_TextBox.Text))
        {
            // TODO: Toast Message: Using default thickness of 5mm
            MagnetoLogger.Log("Using default thickness", LogFactoryLogLevel.LogLevel.DEBUG);
            _currentImage.thickness = 5;
        }
        else
        {
            _currentImage.thickness = Convert.ToDouble(LayerThickness_TextBox.Text);
        }

        // Slice image
        MissionControl.SliceImage(_currentImage);
    }

    private void StartPrint_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        MissionControl.StartPrint(_currentImage);
    }

    private void HomeMotors_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        MissionControl.HomeMotors();
    }

    private void IncrementThickness_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        _currentImage.thickness += 1;
    }

    private void DecrementThickness_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        _currentImage.thickness -= 1;
    }
}
