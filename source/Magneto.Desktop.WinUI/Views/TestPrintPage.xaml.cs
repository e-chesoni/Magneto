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

    private StepperMotor? _powderMotor;

    private StepperMotor? _buildMotor;

    private StepperMotor? _sweepMotor;

    private StepperMotor? _currTestMotor;

    private bool _powderMotorSelected = false;

    private bool _buildMotorSelected = false;

    private bool _sweepMotorSelected = false;

    private bool _movingMotorToTarget = false;

    #endregion

    #region Public Variables

    /// <summary>
    /// Central control that gets passed from page to page
    /// </summary>
    public MissionControl? MissionControl { get; set; }

    /// <summary>
    /// TestPrintViewModel view model
    /// </summary>
    public TestPrintViewModel ViewModel { get; }

    #endregion

    #region Motor Setup

    /// <summary>
    /// Sets up test motors for powder, build, and sweep operations by retrieving configurations 
    /// and initializing the respective StepperMotor objects. Logs success or error for each motor setup.
    /// Assumes motor order in configuration corresponds to powder, build, and sweep.
    /// </summary>
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
    }

    #endregion

    #region Constructor

    /// <summary>
    /// Constructor for TestPrintPage. Initializes the ViewModel, sets up UI components, logs the page visit,
    /// retrieves configuration for build and sweep motors, and registers event handlers for their respective ports.
    /// </summary>
    public TestPrintPage()
    {
        ViewModel = App.GetService<TestPrintViewModel>();
        InitializeComponent();

        var msg = "Landed on Test Print Page";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.DEBUG);
        MagnetoSerialConsole.LogAvailablePorts();

        // Get motor configurations
        var buildMotorConfig = MagnetoConfig.GetMotorByName("build");
        var sweepMotorConfig = MagnetoConfig.GetMotorByName("sweep");

        // Get motor ports, ensuring that the motor configurations are not null
        var buildPort = buildMotorConfig?.COMPort;
        var sweepPort = sweepMotorConfig?.COMPort;
        
        // Initialize motor map to simplify coordinated calls below
        InitializeMotorMap();

        // Register event handlers on page
        foreach (SerialPort port in MagnetoSerialConsole.GetAvailablePorts())
        {
            if (port.PortName.Equals(buildPort, StringComparison.OrdinalIgnoreCase))
            {
                MagnetoSerialConsole.AddEventHandler(port);
                msg = $"Requesting addition of event handler for port {port.PortName}";
                MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);
            }
            else if (port.PortName.Equals(sweepPort, StringComparison.OrdinalIgnoreCase))
            {
                MagnetoSerialConsole.AddEventHandler(port);
                msg = $"Requesting addition of event handler for port {port.PortName}";
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

    #region Dictionary Methods

    /// <summary>
    /// Initializes the dictionary mapping motor names to their corresponding StepperMotor objects.
    /// This map facilitates the retrieval of motor objects based on their names.
    /// </summary>
    private Dictionary<string, StepperMotor?> _motorMap;

    private void InitializeMotorMap()
    {
        _motorMap = new Dictionary<string, StepperMotor?>
        {
            { "build", _buildMotor },
            { "powder", _powderMotor },
            { "sweep", _sweepMotor }
        };
    }

    /// <summary>
    /// Retrieves the corresponding TextBox control for a given motor name.
    /// Returns the TextBox associated with the 'build', 'powder', or 'sweep' motor names.
    /// Returns null if the motor name does not match any of the predefined names.
    /// </summary>
    /// <param name="motorName">The name of the motor for which the corresponding TextBox is needed.</param>
    /// <returns>The corresponding TextBox if found, otherwise null.</returns>
    private TextBox? GetCorrespondingTextBox(string motorName)
    {
        return motorName switch
        {
            "build" => BuildPositionTextBox,
            "powder" => PowderPositionTextBox,
            "sweep" => SweepPositionTextBox,
            _ => null
        };
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Retrieves the corresponding TextBox for a given StepperMotor based on its name.
    /// Returns the BuildPositionTextBox, PowderPositionTextBox, or SweepPositionTextBox
    /// for 'build', 'powder', or 'sweep' motors respectively. Logs an error and returns null
    /// for invalid motor names.
    /// </summary>
    /// <param name="motor">The StepperMotor for which the TextBox is needed.</param>
    /// <returns>The corresponding TextBox if a valid motor name is provided, otherwise null.</returns>
    private TextBox? GetMotorTextBoxHelper(StepperMotor motor)
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

    private void GetPositionHelper(StepperMotor motor)
    {
        // Attempt to open the serial port
        if (MagnetoSerialConsole.OpenSerialPort(motor.GetPortName()))
        {
            MagnetoLogger.Log("Port Open!", LogFactoryLogLevel.LogLevel.SUCCESS);

            // Log the action of getting the position
            var msg = "Using StepperMotor to get position";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);

            // Get the motor's position
            var pos = motor.GetPos();

            // Safely update the corresponding text box
            var textBox = GetMotorTextBoxHelper(motor);
            if (textBox != null)
            {
                textBox.Text = pos.ToString();
            }
            else
            {
                msg = "Motor text box is null.";
                MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
            }
        }
        else
        {
            // Log an error if the port could not be opened
            var errorMsg = "Port Closed.";
            MagnetoLogger.Log(errorMsg, LogFactoryLogLevel.LogLevel.ERROR);
        }
    }

    private void HomeMotorHelper(StepperMotor motor)
    {
        var msg = "Using helper to home motors...";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);

        var currMotor = _currTestMotor;

        if (currMotor != null)
        {
            if (MagnetoSerialConsole.OpenSerialPort(currMotor.GetPortName()))
            {
                _ = motor.HomeMotor();
            }
            else
            {
                msg = "Serial port not open.";
                MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
            }
        }
        else
        {
            msg = "Current Test Motor is null.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
        }
        
    }

    private void SelectMotorHelper(StepperMotor motor, TextBox positionTextBox, ref bool thisMotorSelected)
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

    #endregion

    #region Select Motor Button Methods

    private void SelectPowderMotorButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (_powderMotor != null)
        {
            SelectMotorHelper(_powderMotor, PowderPositionTextBox, ref _powderMotorSelected);
        }
        else
        {
            var msg = "Powder Motor is null.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
        }
    }

    private void SelectBuildMotorButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (_buildMotor != null)
        {
            SelectMotorHelper(_buildMotor, BuildPositionTextBox, ref _buildMotorSelected);
        }
        else
        {
            var msg = "Build Motor is null.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
        }
    }

    private void SelectSweepMotorButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (_sweepMotor != null)
        {
            SelectMotorHelper(_sweepMotor, SweepPositionTextBox, ref _sweepMotorSelected);
        }
        else
        {
            var msg = "Sweep Motor is null.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
        }
    }

    #endregion

    #region Motor Movement Buttons

    /// <summary>
    /// Validates if the provided StepperMotor object is not null, indicating a valid motor request.
    /// Logs an error message if the motor is null and returns -1, indicating an invalid request.
    /// Returns 0 for a valid motor request.
    /// </summary>
    /// <param name="motor">The StepperMotor object to validate.</param>
    /// <returns>An integer indicating the validity of the motor request (-1 for invalid, 0 for valid).</returns>
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

    /// <summary>
    /// Moves the current test motor by a specified distance, which can be absolute or relative based on the parameter.
    /// This method handles validation of the motor request, opens the serial port, initiates the motor movement,
    /// updates the UI with the motor's position, and handles any exceptions that might occur during the process.
    /// </summary>
    /// <param name="isAbsolute">Determines whether the movement is absolute or relative. True for absolute, false for relative.</param>
    private void MoveMotor(bool isAbsolute)
    {
        var movementType = isAbsolute ? "absolute" : "relative";
        var msg = $"Request to move motor to an {movementType} position submitted...";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);

        if (_currTestMotor == null || !(ValidMotorRequest(_currTestMotor) == 0))
        {
            MagnetoLogger.Log("Invalid motor request or Current Test Motor is null.", LogFactoryLogLevel.LogLevel.ERROR);
            return;
        }

        if (MagnetoSerialConsole.OpenSerialPort(_currTestMotor.GetPortName()))
        {
            MagnetoLogger.Log("Port Open!", LogFactoryLogLevel.LogLevel.SUCCESS);
            _movingMotorToTarget = true;
            var motorName = _currTestMotor.GetMotorName();

            try
            {
                if (_motorMap.TryGetValue(motorName, out var motor) && motor != null)
                {
                    var distance = double.Parse(AbsDistTextBox.Text);
                    if (isAbsolute)
                        motor.MoveMotorAbsAsync(distance);
                    else
                        motor.MoveMotorRelAsync(distance);

                    UpdateMotorPositionTextBox(motorName, motor);
                }
                else
                {
                    MagnetoLogger.Log($"Motor '{motorName}' not initialized or not found.", LogFactoryLogLevel.LogLevel.ERROR);
                }
            }
            catch (Exception ex)
            {
                MagnetoLogger.Log($"An exception occurred: {ex.Message}", LogFactoryLogLevel.LogLevel.ERROR);
            }

            _movingMotorToTarget = false;
            UpdateButtonBackground(isAbsolute);
        }
        else
        {
            MagnetoLogger.Log("Port Closed.", LogFactoryLogLevel.LogLevel.ERROR);
        }
    }

    /// <summary>
    /// Updates the text box associated with a given motor name with the motor's current position.
    /// </summary>
    /// <param name="motorName">Name of the motor whose position needs to be updated in the UI.</param>
    /// <param name="motor">The motor object whose position is to be retrieved and displayed.</param>
    private void UpdateMotorPositionTextBox(string motorName, StepperMotor motor)
    {
        var textBox = GetCorrespondingTextBox(motorName);
        if (textBox != null)
            textBox.Text = motor.GetCurrentPos().ToString();
    }

    /// <summary>
    /// Updates the background color of the motor control button based on the current state of motor movement.
    /// Sets the color to green if the motor is moving, and to dim gray once the movement is completed.
    /// </summary>
    /// <param name="isAbsolute">Determines which button's background to update, based on whether the movement is absolute or relative.</param>
    private void UpdateButtonBackground(bool isAbsolute)
    {
        var button = isAbsolute ? MoveMotorAbsButton : MoveMotorRelativeButton;
        button.Background = _movingMotorToTarget ? new SolidColorBrush(Colors.Green) : new SolidColorBrush(Colors.DimGray);
    }

    /// <summary>
    /// Event handler for the 'Move Motor Absolute' button click.
    /// Initiates the process of moving the motor to an absolute position.
    /// </summary>
    private void MoveMotorAbsButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        MoveMotor(isAbsolute: true);
    }

    /// <summary>
    /// Event handler for the 'Move Motor Relative' button click.
    /// Initiates the process of moving the motor to a position relative to its current position.
    /// </summary>
    private void MoveMotorRelativeButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        MoveMotor(isAbsolute: false);
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

    private void HomeAllMotorsButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var msg = "Homing all motors";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);

        HomeMotorHelper(_buildMotor);
        HomeMotorHelper(_powderMotor);
        HomeMotorHelper(_sweepMotor);
    }

    #endregion

    #region Position Buttons

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

    #endregion
}
