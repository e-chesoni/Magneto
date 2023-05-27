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
    //public ImageModel _currentImage = new();

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

        // Put a new image on the build manager
        MissionControl.CreateImageModel(path_to_image);

        // TODO: Initialize Print
        if (string.IsNullOrEmpty(LayerThickness_TextBox.Text))
        {
            // TODO: Toast Message: Using default thickness of 5mm
            MagnetoLogger.Log("Using default thickness", LogFactoryLogLevel.LogLevel.DEBUG);
            MissionControl.SetImageThickness(5);
        }
        else
        {
            // Check that text box entry is a number
            var textBoxValue = LayerThickness_TextBox.Text;
            double value;
            if (double.TryParse(textBoxValue, out value))
            {
                // Conversion succeeded, do something with 'value'
                MissionControl.SetImageThickness(value);
            }
            else
            {
                // Conversion failed, handle the error
                MagnetoLogger.Log("Conversion failed. Are you sure you entered a number?", 
                    LogFactoryLogLevel.LogLevel.ERROR);
            }
        }
        // Slice image
        MissionControl.SliceImage();
    }

    private void StartPrint_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        // Calls build manager in method to handle print
        // Build manager should have an image at this point!
        // TODO: Clear images from build manager after print (in done and cancel states)
        MissionControl.StartPrint();
    }

    private void HomeMotors_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        MissionControl.HomeMotors();
    }

    private void IncrementThickness_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var newThickness = MissionControl.GetImageThickness();
        newThickness += 1;
        MissionControl.SetImageThickness(newThickness);
    }

    private void DecrementThickness_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var newThickness = MissionControl.GetImageThickness();
        newThickness -= 1;
        MissionControl.SetImageThickness(newThickness);
    }
}
