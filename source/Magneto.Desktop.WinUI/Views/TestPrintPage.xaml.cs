using Magneto.Desktop.WinUI.Core.Services;
using Magneto.Desktop.WinUI.ViewModels;

using Microsoft.UI.Xaml.Controls;

namespace Magneto.Desktop.WinUI.Views;

public sealed partial class TestPrintPage : Page
{
    MagnetoLogger magnetoLogger = new MagnetoLogger();

    public TestPrintViewModel ViewModel
    {
        get;
    }

    public TestPrintPage()
    {
        ViewModel = App.GetService<TestPrintViewModel>();
        InitializeComponent();
        System.Diagnostics.Debug.WriteLine("test page landed");
    }

    private void SetMotorAxis1Button_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {

    }

    private void SetMotorAxis2Button_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {

    }

    private void SetMotorAxis3Button_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {

    }

    private void HomeMotorButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {

    }
    private void MoveMotorButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        // TODO: MOVE THIS TO MISSION CONTROL
        //magnetoLogger.LogToConsole(MagnetoLogger.logLevels.CRITICAL, "Moving Motor!");
        System.Diagnostics.Debug.WriteLine("Moving Motor!","error");

        if (MagnetoSerialConsole.OpenSerialPort())
        {
            System.Diagnostics.Debug.WriteLine("Port open!");
            // Write hard-coded move command
            MagnetoSerialConsole.SerialWrite("1MVA20"); // success!
        }
        else
        {
            System.Diagnostics.Debug.WriteLine("Port not open.");
            //MagnetoSerialConsole.SerialWrite("Serial port not open; cannot complete the mission. Try again later.");
            magnetoLogger.LogToConsole(MagnetoLogger.logLevels.ERROR, "Serial port not open; cannot complete the mission. Try again later.");
        }
    }
}
