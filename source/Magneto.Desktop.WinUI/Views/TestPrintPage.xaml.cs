using System.IO.Ports;
using System.Reflection;
using CommunityToolkit.WinUI.UI.Animations;
using Magneto.Desktop.WinUI.Core;
using Magneto.Desktop.WinUI.Core.Contracts.Services;
using Magneto.Desktop.WinUI.Core.Contracts.Services.Motor;
using Magneto.Desktop.WinUI.Core.Helpers;
using Magneto.Desktop.WinUI.Core.Models;
using Magneto.Desktop.WinUI.Core.Models.Motor;
using Magneto.Desktop.WinUI.Core.Services;
using Magneto.Desktop.WinUI.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Devices.SerialCommunication;

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
    private StepperMotor _powderMotor;

    private StepperMotor _buildMotor;

    private StepperMotor _sweepMotor;

    private StepperMotor _currTestMotor;

    private bool _powderMotorSelected = false;

    private bool _buildMotorSelected = false;

    private bool _sweepMotorSelected = false;

    private bool _movingMotorToTarget = false;

    private bool _homigMotor = false;

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

    #region Motor Setup

    private void InitializeMagnetoMotors()
    {
        var msg = "";

        //MagnetoLogger.Log(MissionControl.FriendlyMessage, LogFactoryLogLevel.LogLevel.DEBUG);
        List<MagnetoMotorConfig> motorConfigs = new List<MagnetoMotorConfig>();
        List<string> motorNames = new List<string>();

        foreach (var m in MagnetoConfig.GetAllMotors())
        {
            motorConfigs.Add(m);
            motorNames.Add(m.motorName);
        }

        StepperMotor p = MissionControl.GetPowderMotor();

        if (p != null)
        {
            msg = $"Found motor in config with name {motorNames[0]}. Setting this to powder motor in test.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);
            _powderMotor = p;
        }
        else
        {
            msg = "Unable to find powder motor";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
        }

        StepperMotor b = MissionControl.GetBuildMotor();

        if (b != null)
        {
            msg = $"Found motor in config with name {motorNames[1]}. Setting this to build motor in test.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);
            _buildMotor = b;
        }
        else
        {
            msg = "Unable to find build motor";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
        }

        StepperMotor s = MissionControl.GetSweepMotor();

        if (s != null)
        {
            msg = $"Found motor in config with name {motorNames[2]}. Setting this to sweep motor in test.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);
            _sweepMotor = s;
        }
        else
        {
            msg = "Unable to find sweep motor";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
        }

        // Set current test motor to _powderMotor by default
        SelectPowderMotorHelper();
    }

    #endregion

    #region Constructor

    /// <summary>
    /// TestPrintViewModel Constructor
    /// </summary>
    public TestPrintPage()
    {
        ViewModel = App.GetService<TestPrintViewModel>();
        InitializeComponent();

        var msg = "";
        
        msg = "Landed on Test Print Page";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.DEBUG);
        MagnetoSerialConsole.LogAvailablePorts();

        // Register event handlers on page
        foreach (SerialPort port in MagnetoSerialConsole.GetAvailablePorts())
        {
            // Get default motor (build motor) to get port
            if (port.PortName.Equals("COM4", StringComparison.OrdinalIgnoreCase))
            {
                MagnetoSerialConsole.AddEventHandler(port);
                msg = $"Requesting addition of event hander or port {port.PortName}";
                MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);
            }
            else if (port.PortName.Equals("COM7", StringComparison.OrdinalIgnoreCase))
            {
                MagnetoSerialConsole.AddEventHandler(port);
                msg = $"Requesting addition of event hander or port {port.PortName}";
                MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);
            }
        }
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
        InitializeMagnetoMotors();
        var msg = string.Format("TestPrintPage::OnNavigatedTo -- {0}", MissionControl.FriendlyMessage);
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.DEBUG);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Helper that calls the MoveMotorRelAsync method on the motor attached to selected axis
    /// </summary>
    /// <param name="axis"></param>
    private void MoveMotorHelper(int axis, string dist)
    {
        MagnetoLogger.Log("TestPrintPage::MoveMotorHelper", LogFactoryLogLevel.LogLevel.VERBOSE);

        _currTestMotor.SetAxis(axis);
        _currTestMotor.MoveMotorRelAsync(double.Parse(dist));
    }

    private void GetMotorPositionHelper(int axis)
    {
        MagnetoLogger.Log("TestPrintPage::MoveMotorHelper", LogFactoryLogLevel.LogLevel.VERBOSE);

        _currTestMotor.SetAxis(axis);
        _currTestMotor.GetPos();
    }

    #endregion

    #region Button Methods

    private void SelectPowderMotorHelper()
    {
        _currTestMotor = _powderMotor;

        if (!_powderMotorSelected)
        {
            SelectPowderMotorButton.Background = new SolidColorBrush(Colors.Green);
            _powderMotorSelected = true;

            SelectBuildMotorButton.Background = new SolidColorBrush(Colors.DimGray);
            _buildMotorSelected = false;

            SelectSweepMotorButton.Background = new SolidColorBrush(Colors.DimGray);
            _sweepMotorSelected = false;
        }
        else
        {
            SelectPowderMotorButton.Background = new SolidColorBrush(Colors.DimGray);
            _powderMotorSelected = false;
        }
    }

    /// <summary>
    /// Set the test motor axis to 1
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void SelectPowderMotorButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        SelectPowderMotorHelper();
    }

    /// <summary>
    /// Set the test motor axis to 2
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void SelectBuildMotorButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        _currTestMotor = _buildMotor;

        if (!_buildMotorSelected)
        {
            SelectBuildMotorButton.Background = new SolidColorBrush(Colors.Green);
            _buildMotorSelected = true;

            SelectPowderMotorButton.Background = new SolidColorBrush(Colors.DimGray);
            _powderMotorSelected = false;

            SelectSweepMotorButton.Background = new SolidColorBrush(Colors.DimGray);
            _sweepMotorSelected = false;
        }
        else
        {
            SelectBuildMotorButton.Background = new SolidColorBrush(Colors.DimGray);
            _buildMotorSelected = false;
        }
    }

    /// <summary>
    /// Set the test motor to the sweep motor
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void SelectSweepMotorButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        // TODO: Update this method to set the test motor to the powder distribution motor
        // A the time this code was written, the third motor has not yet arrived
        _currTestMotor = _sweepMotor;

        if (!_sweepMotorSelected)
        {
            SelectSweepMotorButton.Background = new SolidColorBrush(Colors.Green);
            _sweepMotorSelected = true;

            SelectPowderMotorButton.Background = new SolidColorBrush(Colors.DimGray);
            _powderMotorSelected = false;

            SelectBuildMotorButton.Background = new SolidColorBrush(Colors.DimGray);
            _buildMotorSelected = false;
        }
        else
        {
            SelectSweepMotorButton.Background = new SolidColorBrush(Colors.DimGray);
            _sweepMotorSelected = false;
        }
    }

    /// <summary>
    /// Home the motor currently being tested
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void HomeMotorButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        MagnetoLogger.Log("Homing Motor.", LogFactoryLogLevel.LogLevel.VERBOSE);
        _homigMotor = true;

        if (!_homigMotor)
        {
            HomeMotorButton.Background = new SolidColorBrush(Colors.Green);
            _homigMotor = true;
        }
        else
        {
            HomeMotorButton.Background = new SolidColorBrush(Colors.DimGray);
            _homigMotor = false;
        }

        if (MagnetoSerialConsole.OpenSerialPort(_currTestMotor.GetPortName()))
        {
            _ = _currTestMotor.HomeMotor();
        }
        else
        {
            MagnetoLogger.Log("Serial port not open.",
                LogFactoryLogLevel.LogLevel.ERROR);
        }

        // TODO: keep button green until motor is done moving
        _homigMotor = false;
    }

    /// <summary>
    /// Moves motor attached to selected access (default axis is 0, which no motor is attached to)
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void MoveMotorButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        string dist = ViewModel.DistanceText;
        MagnetoLogger.Log($"Commanded distance to move: {dist}", LogFactoryLogLevel.LogLevel.VERBOSE);

        if (MagnetoSerialConsole.OpenSerialPort(_currTestMotor.GetPortName()))
        {
            MagnetoLogger.Log("Port Open!", LogFactoryLogLevel.LogLevel.SUCCESS);
            _movingMotorToTarget = true;

            if (!_movingMotorToTarget)
            {
                MoveMotorButton.Background = new SolidColorBrush(Colors.Green);
                _movingMotorToTarget = true;
            }
            else
            {
                MoveMotorButton.Background = new SolidColorBrush(Colors.DimGray);
                _movingMotorToTarget = false;
            }

            switch (_currTestMotor.GetAxis())
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

            // TODO:keep button green until motor is done moving
            _movingMotorToTarget = false;
        }
        else
        {
            MagnetoLogger.Log("Port Closed.", LogFactoryLogLevel.LogLevel.ERROR);
        }
    }

    #endregion

    private void GetPositionButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var msg = "";
        msg = "GetPositionButton Clicked...";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);

        if (MagnetoSerialConsole.OpenSerialPort(_currTestMotor.GetPortName()))
        {
            MagnetoLogger.Log("Port Open!", LogFactoryLogLevel.LogLevel.SUCCESS);
            switch (_currTestMotor.GetAxis())
            {
                case 0:
                    MagnetoLogger.Log("No axis selected",
                        LogFactoryLogLevel.LogLevel.WARN);
                    break;
                case 1:
                    GetMotorPositionHelper(1);
                    break;
                case 2:
                    GetMotorPositionHelper(2);
                    break;
                case 3:
                    GetMotorPositionHelper(3);
                    break;
                default:
                    break;
            }
        }
        else
        {
            msg = "Port Closed.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
        }
    }
}
