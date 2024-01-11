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
using Microsoft.UI.Xaml;
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

    private StepperMotor _powderMotor;

    private StepperMotor _buildMotor;

    private StepperMotor _sweepMotor;

    private StepperMotor _currTestMotor;

    private bool _powderMotorSelected = false;

    private bool _buildMotorSelected = false;

    private bool _sweepMotorSelected = false;

    private bool _movingMotorToTarget = false;

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

    private void SetUpTestMotors()
    {
        var msg = "";

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
        //SelectPowderMotorHelper();
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

        // Get motor ports
        var buildPort = MagnetoConfig.GetMotorByName("build").COMPort;
        var sweepPort = MagnetoConfig.GetMotorByName("sweep").COMPort;

        // Register event handlers on page
        foreach (SerialPort port in MagnetoSerialConsole.GetAvailablePorts())
        {
            // Get default motor (build motor) to get port
            if (port.PortName.Equals(buildPort, StringComparison.OrdinalIgnoreCase))
            {
                MagnetoSerialConsole.AddEventHandler(port);
                msg = $"Requesting addition of event hander or port {port.PortName}";
                MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);
            }
            else if (port.PortName.Equals(sweepPort, StringComparison.OrdinalIgnoreCase))
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
        SetUpTestMotors();
        var msg = string.Format("TestPrintPage::OnNavigatedTo -- {0}", MissionControl.FriendlyMessage);
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.DEBUG);
    }

    #endregion

    #region Helper Methods

    private TextBox GetMotorTextBoxHelper(StepperMotor motor)
    {
        if (motor.GetMotorName() == "build")
        {
            return BuildPositionTextBox;
        }
        else if (motor.GetMotorName() == "powder")
        {
            return PowderPositionTextBox;
        }
        else if (motor.GetMotorName() == "sweep")
        {
            return SweepPositionTextBox;
        }
        else
        {
            var msg = "Invalid motor name given. Cannot get position.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
            return null;
        }
    }

    private void GetMotorPositionHelper(StepperMotor motor)
    {
        var msg = "Using StepperMotor to get position";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);

        var pos = motor.GetPos();
        // TODO: Create position text boxes for each motor -> set current position text box like motor (same with desire text box)
        GetMotorTextBoxHelper(motor).Text = pos.ToString();
    }

    private void HomeMotorHelper(StepperMotor motor)
    {
        var msg = "Using helper to home motors...";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);

        if (MagnetoSerialConsole.OpenSerialPort(_currTestMotor.GetPortName()))
        {
            _ = motor.HomeMotor();
        }
        else
        {
            MagnetoLogger.Log("Serial port not open.",
                LogFactoryLogLevel.LogLevel.ERROR);
        }
    }

    #endregion

    #region Select Motor Button Methods

    private void SelectMotorHelper(StepperMotor motor, Button selectedButton, TextBox positionTextBox, ref bool thisMotorSelected)
    {
        // Clear position text box
        positionTextBox.Text = "";

        _currTestMotor = motor;
        var msg = $"Setting current motor to {_currTestMotor.GetMotorName()} motor";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);

        // Update button backgrounds and selection flags
        SelectPowderMotorButton.Background = new SolidColorBrush(_powderMotor == motor ? Colors.Green : Colors.DimGray);
        _powderMotorSelected = _powderMotor == motor;

        SelectBuildMotorButton.Background = new SolidColorBrush(_buildMotor == motor ? Colors.Green : Colors.DimGray);
        _buildMotorSelected = _buildMotor == motor;

        SelectSweepMotorButton.Background = new SolidColorBrush(_sweepMotor == motor ? Colors.Green : Colors.DimGray);
        _sweepMotorSelected = _sweepMotor == motor;

        // Update the selection flag for this motor
        thisMotorSelected = !thisMotorSelected;
    }

    private void SelectPowderMotorButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        SelectMotorHelper(_powderMotor, SelectPowderMotorButton, PowderPositionTextBox, ref _powderMotorSelected);
    }

    private void SelectBuildMotorButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        SelectMotorHelper(_buildMotor, SelectBuildMotorButton, BuildPositionTextBox, ref _buildMotorSelected);
    }

    private void SelectSweepMotorButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        SelectMotorHelper(_sweepMotor, SelectSweepMotorButton, SweepPositionTextBox, ref _sweepMotorSelected);
    }


    /// <summary>
    /// Home the motor currently being tested
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void HomeMotorButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        MagnetoLogger.Log("Homing Motor.", LogFactoryLogLevel.LogLevel.VERBOSE);
        HomeMotorHelper(_currTestMotor);
    }

    #endregion

    private void HomeAllMotorsButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var msg = "Homing all motors";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);

        HomeMotorHelper(_buildMotor);
        HomeMotorHelper(_powderMotor);
        HomeMotorHelper(_sweepMotor);
    }

    private int ValidMotorRequest(StepperMotor motor)
    {
        if (motor == null)
        {
            var msg = "No motor selected.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
            return -1;
        }
        else 
        {
            return 0;
        }
    }

    private void MoveMotorAbsButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var msg = "Request to move motor to an absolute position submitted...";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);

        var dist = ViewModel.DistanceText;
        msg = $"Commanded distance to move: {dist}";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);

        if (!(ValidMotorRequest(_currTestMotor)==0))
        {
            return;
        }

        if (MagnetoSerialConsole.OpenSerialPort(_currTestMotor.GetPortName()))
        {
            MagnetoLogger.Log("Port Open!", LogFactoryLogLevel.LogLevel.SUCCESS);
            _movingMotorToTarget = true;

            if (!_movingMotorToTarget)
            {
                MoveMotorAbsButton.Background = new SolidColorBrush(Colors.Green);
                _movingMotorToTarget = true;
            }
            else
            {
                MoveMotorAbsButton.Background = new SolidColorBrush(Colors.DimGray);
                _movingMotorToTarget = false;
            }

            // Get current motor name to move motor and update text box accordingly
            if (_currTestMotor.GetMotorName() == "build")
            {
                _buildMotor.MoveMotorAbsAsync(double.Parse(AbsDistTextBox.Text));
                BuildPositionTextBox.Text = _buildMotor.GetCurrentPos().ToString();
            }
            else if (_currTestMotor.GetMotorName() == "powder")
            {
                _powderMotor.MoveMotorAbsAsync(double.Parse(AbsDistTextBox.Text));
                PowderPositionTextBox.Text = _powderMotor.GetCurrentPos().ToString();
            }
            else if (_currTestMotor.GetMotorName() == "sweep")
            {
                _sweepMotor.MoveMotorAbsAsync(double.Parse(AbsDistTextBox.Text));
                SweepPositionTextBox.Text = _sweepMotor.GetCurrentPos().ToString();
            }

            // TODO:keep button green until motor is done moving
            _movingMotorToTarget = false;
        }
        else
        {
            MagnetoLogger.Log("Port Closed.", LogFactoryLogLevel.LogLevel.ERROR);
        }
    }

    private void GetPositionHelper(StepperMotor motor)
    {
        if (MagnetoSerialConsole.OpenSerialPort(motor.GetPortName()))
        {
            GetMotorPositionHelper(motor);
            MagnetoLogger.Log("Port Open!", LogFactoryLogLevel.LogLevel.SUCCESS);
        }
        else
        {
            var msg = "Port Closed.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
        }
    }

    private void GetBuildPositionButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var msg = "GetBuildPositionButton_Click Clicked...";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);
        GetPositionHelper(_buildMotor);
    }

    private void GetPowderPositionButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var msg = "GetPowderPositionButton_Click Clicked...";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);
        GetPositionHelper(_powderMotor);
    }

    private void GetSweepPositionButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var msg = "GetSweepPositionButton_Click Clicked...";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);
        GetPositionHelper(_sweepMotor);
    }

    private void MoveMotorRelativeButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {

    }
}
