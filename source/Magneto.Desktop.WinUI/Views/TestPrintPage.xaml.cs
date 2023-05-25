using Magneto.Desktop.WinUI.Core.Contracts.Services;
using Magneto.Desktop.WinUI.Core.Models;
using Magneto.Desktop.WinUI.Core.Services;
using Magneto.Desktop.WinUI.ViewModels;

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Magneto.Desktop.WinUI.Views;

public sealed partial class TestPrintPage : Page
{
    public MissionControl MissionControl { get; set; }

    public TestPrintViewModel ViewModel { get; }

    public TestPrintPage()
    {
        ViewModel = App.GetService<TestPrintViewModel>();
        InitializeComponent();
        
        MagnetoLogger.Log("Landed on Test Print Page", LogFactoryLogLevel.LogLevel.DEBUG);
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        // Get mission control (passed over when navigating from previous page)
        base.OnNavigatedTo(e);
        MissionControl = (MissionControl)e.Parameter;

        var msg = string.Format("TestPrintPage::OnNavigatedTo -- {0}", MissionControl.FriendlyMessage);
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.DEBUG);
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
        // TODO: MISSION CONTROL HANDLES THIS
        MagnetoLogger.Log("Homing Motor.", LogFactoryLogLevel.LogLevel.DEBUG);

        if (MagnetoSerialConsole.OpenSerialPort())
        {
            // Write hard-coded move command
            MagnetoSerialConsole.SerialWrite("1MVA0"); // success!
        }
        else
        {
            MagnetoLogger.Log("Serial port not open; cannot complete the mission. Try again later.", LogFactoryLogLevel.LogLevel.ERROR);
        }
    }
    private void MoveMotorButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        // TODO: MOVE THIS TO MISSION CONTROL
        MagnetoLogger.Log("Moving Motor!", LogFactoryLogLevel.LogLevel.DEBUG);

        if (MagnetoSerialConsole.OpenSerialPort())
        {
            MagnetoLogger.Log("Port Open!", LogFactoryLogLevel.LogLevel.SUCCESS);
            // Write hard-coded move command
            MagnetoSerialConsole.SerialWrite("1MVA20"); // success!
        }
        else
        {
            MagnetoLogger.Log("Port Closed.", LogFactoryLogLevel.LogLevel.ERROR);
        }
    }
}
