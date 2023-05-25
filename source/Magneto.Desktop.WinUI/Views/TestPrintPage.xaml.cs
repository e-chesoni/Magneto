using CommunityToolkit.WinUI.UI.Animations;
using Magneto.Desktop.WinUI.Core.Contracts.Services;
using Magneto.Desktop.WinUI.Core.Models;
using Magneto.Desktop.WinUI.Core.Models.Motor;
using Magneto.Desktop.WinUI.Core.Services;
using Magneto.Desktop.WinUI.ViewModels;

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Magneto.Desktop.WinUI.Views;

public sealed partial class TestPrintPage : Page
{
    public MissionControl MissionControl { get; set; }

    public TestPrintViewModel ViewModel { get; }

    StepperMotor testMotor = new StepperMotor(0);

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
        testMotor.motorAxis = 1;
    }

    private void SetMotorAxis2Button_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        testMotor.motorAxis = 2;
    }

    private void SetMotorAxis3Button_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        testMotor.motorAxis = 3;
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

    private void MoveMotorHelper(int axis)
    {
        MagnetoLogger.Log("TestPrintPage::MoveMotorHelper", LogFactoryLogLevel.LogLevel.VERBOSE);

        // Create test motor object that will (hopefully) be destroyed after run completes
        // TODO: Use debugger to make sure testmotor is destroyed after loop exits
        // We don't want a bunch of unused motors hanging around in the app
        testMotor.motorAxis = axis;
        testMotor.MoveMotorRel(10);
    }
    private void MoveMotorButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {

        if (MagnetoSerialConsole.OpenSerialPort())
        {
            MagnetoLogger.Log("Port Open!", LogFactoryLogLevel.LogLevel.SUCCESS);
            // Write hard-coded move command
            switch (testMotor.motorAxis)
            {
                case 0:
                    MagnetoLogger.Log("TestPrintPage::MoveMotorButton_Click -- No axis selected", 
                        LogFactoryLogLevel.LogLevel.WARN);
                    break;
                case 1:
                    MoveMotorHelper(1);
                    break; 
                case 2:
                    MoveMotorHelper(2);
                    break;
                case 3:
                    MoveMotorHelper(3);
                    break;
                default: 
                    break;
            }
        }
        else
        {
            MagnetoLogger.Log("Port Closed.", LogFactoryLogLevel.LogLevel.ERROR);
        }
    }
}
