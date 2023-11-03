using System.Reflection;
using CommunityToolkit.WinUI.UI.Animations;
using Magneto.Desktop.WinUI.Core.Contracts.Services;
using Magneto.Desktop.WinUI.Core.Helpers;
using Magneto.Desktop.WinUI.Core.Models;
using Magneto.Desktop.WinUI.Core.Models.Motor;
using Magneto.Desktop.WinUI.Core.Services;
using Magneto.Desktop.WinUI.ViewModels;

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;

namespace Magneto.Desktop.WinUI.Views;

/// <summary>
/// Test print page
/// </summary>
public sealed partial class TestPrintPage : Page
{
    #region Private Variables

    /// <summary>
    /// Place holder motor for test carried out through this page
    /// The default axis is 0, which runs the test on both motors
    /// </summary>
    private StepperMotor stepMotor1 = new StepperMotor("COM4", 1, 35, 0, 0);

    private StepperMotor stepMotor2 = new StepperMotor("COM4", 2, 35, 0, 0);

    private StepperMotor sweepMotor = new StepperMotor("COM7", 1, 50, -50, 0);

    private StepperMotor currTestMotor;

    #endregion

    #region Public Variables

    /// <summary>
    /// Central control that gets passed from page to page
    /// </summary>
    public MissionControl MissionControl { get; set; }

    /// <summary>
    /// TestPrintViewModel view model
    /// </summary>
    public TestPrintViewModel ViewModel { get; }

    #endregion

    #region Constructor

    /// <summary>
    /// TestPrintViewModel Constructor
    /// </summary>
    public TestPrintPage()
    {
        ViewModel = App.GetService<TestPrintViewModel>();
        InitializeComponent();
        
        MagnetoLogger.Log("Landed on Test Print Page", LogFactoryLogLevel.LogLevel.DEBUG);

        // Set current test motor to stepMotor1 by default
        currTestMotor = stepMotor1;
    }

    #endregion

    #region Navigation Methods

    /// <summary>
    /// Handle page startup tasks
    /// </summary>
    /// <param name="e"></param>
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        // Get mission control (passed over when navigating from previous page)
        base.OnNavigatedTo(e);
        MissionControl = (MissionControl)e.Parameter;

        var msg = string.Format("TestPrintPage::OnNavigatedTo -- {0}", MissionControl.FriendlyMessage);
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.DEBUG);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Helper that calls the MoveMotorRel method on the motor attached to selected axis
    /// </summary>
    /// <param name="axis"></param>
    private void MoveMotorHelper(int axis, string dist)
    {
        MagnetoLogger.Log("TestPrintPage::MoveMotorHelper", LogFactoryLogLevel.LogLevel.VERBOSE);

        // Create test motor object that will (hopefully) be destroyed after run completes
        // TODO: Use debugger to make sure test motor is destroyed after loop exits
        // We don't want a bunch of unused motors hanging around in the app

        currTestMotor.SetAxis(axis);
        currTestMotor.MoveMotorRel(double.Parse(dist));
    }

    #endregion

    #region Button Methods

    /// <summary>
    /// Set the test motor axis to 1
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void SetMotorAxis1Button_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        currTestMotor = stepMotor1;
    }

    /// <summary>
    /// Set the test motor axis to 2
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void SetMotorAxis2Button_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        currTestMotor = stepMotor2;

    }

    /// <summary>
    /// Set the test motor to the sweep motor
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void SetMotorAxis3Button_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        // TODO: Update this method to set the test motor to the powder distribution motor
        // A the time this code was written, the third motor has not yet arrived
        currTestMotor = sweepMotor;
    }

    /// <summary>
    /// Home the motor currently being tested
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void HomeMotorButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        MagnetoLogger.Log("Homing Motor.", LogFactoryLogLevel.LogLevel.VERBOSE);

        if (MagnetoSerialConsole.OpenSerialPort(currTestMotor.GetPortName()))
        {
            _ = currTestMotor.HomeMotor();
        }
        else
        {
            MagnetoLogger.Log("Serial port not open.",
                LogFactoryLogLevel.LogLevel.ERROR);
        }
    }

    /// <summary>
    /// Moves motor attached to selected access (default axis is 0, which no motor is attached to)
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void MoveMotorButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        string dist = ViewModel.DistanceText;
        MagnetoLogger.Log($"Got distance: {dist}", LogFactoryLogLevel.LogLevel.VERBOSE);

        if (MagnetoSerialConsole.OpenSerialPort(currTestMotor.GetPortName()))
        {
            MagnetoLogger.Log("Port Open!", LogFactoryLogLevel.LogLevel.SUCCESS);

            switch (currTestMotor.GetAxis())
            {
                case 0:
                    MagnetoLogger.Log("No axis selected", 
                        LogFactoryLogLevel.LogLevel.WARN);
                    break;
                case 1:
                    MoveMotorHelper(1, dist);
                    break; 
                case 2:
                    MoveMotorHelper(2, dist);
                    break;
                case 3:
                    MoveMotorHelper(3, dist);
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

    #endregion
}
