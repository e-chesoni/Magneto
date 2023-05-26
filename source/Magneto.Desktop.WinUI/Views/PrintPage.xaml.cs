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
    public ImageModel _currentImage = new ImageModel();

    public MissionControl MissionControl;

    StepperMotor testMotor = new StepperMotor(1);

    public PrintViewModel ViewModel
    {
        get;
    }

    public PrintPage()
    {
        ViewModel = App.GetService<PrintViewModel>();
        InitializeComponent();
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        MissionControl = (MissionControl)e.Parameter; // get parameter
        var msg = string.Format("PrintPage::OnNavigatedTo -- {0}", MissionControl.FriendlyMessage);
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.DEBUG);
    }

    private void FindPrint_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        string path_to_image = "c:/path/to/image";

        // Add dummy string to text box
        SelectedPrint.Text = path_to_image;

        // Generate fake image manager to get things going
        _currentImage.path_to_image = path_to_image;
    }

    private void StartPrint_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        MissionControl.StartPrint(_currentImage);
    }

    private void HomeMotors_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        MissionControl.HomeMotors();
    }
}
