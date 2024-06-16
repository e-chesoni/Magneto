using System.IO.Ports;
using System.Reflection;
using CommunityToolkit.WinUI.UI.Animations;
using Magneto.Desktop.WinUI.Core;
using Magneto.Desktop.WinUI.Core.Contracts.Services;
using Magneto.Desktop.WinUI.Core.Contracts.Services.Motor;
using Magneto.Desktop.WinUI.Core.Helpers;
using Magneto.Desktop.WinUI.Core.Models;
using Magneto.Desktop.WinUI.Core.Models.BuildModels;
using Magneto.Desktop.WinUI.Core.Models.Motor;
using Magneto.Desktop.WinUI.Core.Services;
using Magneto.Desktop.WinUI.Helpers;
using Magneto.Desktop.WinUI.Popups;
using Magneto.Desktop.WinUI.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Newtonsoft.Json.Bson;
using SAMLIGHT_CLIENT_CTRL_EXLib;
using Windows.Devices.SerialCommunication;
using static Magneto.Desktop.WinUI.Core.Models.BuildModels.BuildManager;
using static Magneto.Desktop.WinUI.Views.TestPrintPage;

namespace Magneto.Desktop.WinUI.Views;

/// <summary>
/// Test print page
/// </summary>
public sealed partial class TestPrintPage : Page
{
    #region Motor Variables

    private StepperMotor? _powderMotor;

    private StepperMotor? _buildMotor;

    private StepperMotor? _sweepMotor;

    private BuildManager? _bm;

    private bool _powderMotorSelected = false;

    private bool _buildMotorSelected = false;

    private bool _sweepMotorSelected = false;

    private bool _movingMotorToTarget = false;

    /// <summary>
    /// Struct for motor details
    /// </summary>
    public struct MotorDetails
    {
        public string MotorName
        {
            get;
        }
        public ControllerType ControllerType
        {
            get;
        }
        public int MotorAxis
        {
            get;
        }

        public MotorDetails(string motorName, ControllerType controllerType, int motorAxis)
        {
            MotorName = motorName;
            ControllerType = controllerType;
            MotorAxis = motorAxis;
        }
    }

    #endregion


    #region WaveRunner Variables

    /// <summary>
    /// WaveRunner client control interface
    /// </summary>
    private static readonly ScSamlightClientCtrlEx cci = new();

    /// <summary>
    /// Default job directory (to search for job files)
    /// </summary>
    private string _defaultJobDirectory
    {
        get; set;
    }

    /// <summary>
    /// Default job file name
    /// </summary>
    private string _defaultJobName
    {
        get; set;
    }

    /// <summary>
    /// Job directory (to search for files) -- can be defined by the user
    /// </summary>
    private string _jobDirectory
    {
        get; set;
    }

    /// <summary>
    /// Full file path to entity
    /// </summary>
    private string? _fullJobFilePath
    {
        get; set;
    }

    private bool _redPointerEnabled
    {
        get; set;
    }

    /// <summary>
    /// WaveRunner Execution statuses
    /// </summary>
    public enum ExecStatus
    {
        Success = 0,
        Failure = -1,
    }

    /// <summary>
    /// RedPointer Modes
    /// </summary>
    public enum RedPointerMode
    {
        IndividualOutline = 1,
        TotalOutline = 2,
        IndividualBorder = 3,
        OnlyRedPointerEntities = 4,
        OutermostBorder = 5
    }

    #endregion


    #region Layer Thickness Variables

    private double _layerThickness;

    #endregion


    #region Public Variables

    /// <summary>
    /// Central control that gets passed from page to page
    /// </summary>
    public MissionControl? MissionControl
    {
        get; set;
    }

    /// <summary>
    /// TestPrintViewModel view model
    /// </summary>
    public TestPrintViewModel ViewModel
    {
        get;
    }

    #endregion


    #region Test Page Setup

    /// <summary>
    /// Sets up test motors for powder, build, and sweep operations by retrieving configurations 
    /// and initializing the respective StepperMotor objects. Logs success or error for each motor setup.
    /// Assumes motor order in configuration corresponds to powder, build, and sweep.
    /// </summary>
    private async void SetUpTestMotors()
    {
        if (MissionControl == null)
        {
            MagnetoLogger.Log("MissionControl is null. Unable to set up motors.", LogFactoryLogLevel.LogLevel.ERROR);
            await PopupInfo.ShowContentDialog(this.Content.XamlRoot,"Error", "MissionControl is not Connected.");
            return;
        }

        SetUpMotor("powder", MissionControl.GetPowderMotor(), out _powderMotor);
        SetUpMotor("build", MissionControl.GetBuildMotor(), out _buildMotor);
        SetUpMotor("sweep", MissionControl.GetSweepMotor(), out _sweepMotor);

        _bm = MissionControl.GetBuildManger();

        //GetMotorPositions(); // TOOD: Fix--all positions are 0 on page load even if they're not...
    }

    /// <summary>
    /// Gets motor from mission control and assigns each motor to a private variable for easy access in the TestPrintPage class
    /// </summary>
    /// <param name="motorName">Motor name</param>
    /// <param name="motor">The actual stepper motor</param>
    /// <param name="motorField">Variable to assign motor to</param>
    private void SetUpMotor(string motorName, StepperMotor motor, out StepperMotor motorField)
    {
        if (motor != null)
        {
            motorField = motor;
            var msg = $"Found motor in config with name {motor.GetMotorName()}. Setting this to {motorName} motor in test.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);
        }
        else
        {
            motorField = null;
            MagnetoLogger.Log($"Unable to find {motorName} motor", LogFactoryLogLevel.LogLevel.ERROR);
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

        SetupMotors();
        SetupWaveRunner();
        SetDefaultPrintSettings();
    }

    private void SetupMotors()
    {
        var msg = "Setting up motor variables.";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);

        // Get motor configurations
        var buildMotorConfig = MagnetoConfig.GetMotorByName("build");
        var sweepMotorConfig = MagnetoConfig.GetMotorByName("sweep");

        // Get motor ports, ensuring that the motor configurations are not null
        var buildPort = buildMotorConfig?.COMPort;
        var sweepPort = sweepMotorConfig?.COMPort;

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

    private void SetupWaveRunner()
    {
        var msg = "Setting up WaveRunner variables.";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);

        // Set Job Directory
        _defaultJobDirectory = @"C:\Scanner Application\Scanner Software\jobfiles";
        _jobDirectory = _defaultJobDirectory;
        JobFileSearchDirectory.Text = _jobDirectory;

        // Set Job File
        _defaultJobName = "center_crosshair_OAT.sjf";
        JobFileNameTextBox.Text = _defaultJobName;

        // ASSUMPTION: Red pointer is off when application starts
        // Have not found way to check red pointer status in SAMLight docs 
        // Initialize red pointer to off
        _redPointerEnabled = false;
    }

    private void SetDefaultPrintSettings()
    {
        _layerThickness = MagnetoConfig.GetDefaultPrintThickness();
        LayerThicknessTextBlock.Text = _layerThickness.ToString();
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

        // Set mission control after navigating to new page
        MissionControl = (MissionControl)e.Parameter;
        
        // Initialize motor set up for test page
        SetUpTestMotors();
        
        // Initialize motor map to simplify coordinated calls below
        // Make sure this happens AFTER motor setup
        InitializeMotorMap();

        // Get motor positions
        //TODO: FIX ME -- mixes up motor positions
        //GetMotorPositions();

        var msg = string.Format("TestPrintPage::OnNavigatedTo -- {0}", MissionControl.FriendlyMessage);
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.DEBUG);
    }

    #endregion


    #region Dictionary Methods

    /// <summary>
    /// Initializes the dictionary mapping motor names to their corresponding StepperMotor objects.
    /// This map facilitates the retrieval of motor objects based on their names.
    /// </summary>
    private Dictionary<string, StepperMotor?>? _motorToPosTextBoxMap;

    private void InitializeMotorMap()
    {
        _motorToPosTextBoxMap = new Dictionary<string, StepperMotor?>
        {
            { "build", _buildMotor },
            { "powder", _powderMotor },
            { "sweep", _sweepMotor }
        };
    }

    /// <summary>
    /// Retrieves the corresponding TextBox control for a given motor name.
    /// </summary>
    /// <param name="motorName">The name of the motor for which the corresponding TextBox is needed.</param>
    /// <returns>The corresponding TextBox if found, otherwise null.</returns>
    private TextBox? GetCorrespondingTextBox(string motorName)
    {
        return motorName switch
        {
            "build" => BuildMotorCurrentPositionTextBox,
            "powder" => PowderMotorCurrentPositionTextBox,
            "sweep" => SweepMotorCurrentPositionTextBox,
            _ => null
        };
    }

    #endregion


    #region TextBox Helper Methods

    /// <summary>
    /// Retrieves the corresponding TextBox for a given StepperMotor.
    /// </summary>
    /// <param name="motor">The StepperMotor for which the TextBox is needed.</param>
    /// <returns>The corresponding TextBox if a valid motor name is provided, otherwise null.</returns>
    private TextBox? GetMotorPositionTextBoxHelper(StepperMotor motor)
    {
        if (motor.GetMotorName() == "build")
        {
            return BuildMotorCurrentPositionTextBox;
        }
        else if (motor.GetMotorName() == "powder")
        {
            return PowderMotorCurrentPositionTextBox;
        }
        else if (motor.GetMotorName() == "sweep")
        {
            return SweepMotorCurrentPositionTextBox;
        }
        else
        {
            var msg = "Invalid motor name given. Cannot get position.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
            return null;
        }
    }

    /// <summary>
    /// Retrieves the corresponding increment TextBox for a given StepperMotor.
    /// </summary>
    /// <param name="motor">The StepperMotor for which the TextBox is needed.</param>
    /// <returns>The corresponding TextBox if a valid motor name is provided, otherwise null.</returns>
    private TextBox? GetStepTextBoxHelper(StepperMotor motor) // GetIncrementTextBoxHelper in PrintPage
    {
        if (motor.GetMotorName() == "build")
        {
            return BuildMotorStepTextBox;
        }
        else if (motor.GetMotorName() == "powder")
        {
            return PowderMotorStepTextBox;
        }
        else if (motor.GetMotorName() == "sweep")
        {
            return SweepMotorStepTextBox;
        }
        else
        {
            var msg = "Invalid motor name given. Cannot get position.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
            return null;
        }
    }

    #endregion


    #region Position Helper Methods

    /// <summary>
    /// Retrieves the position of a given StepperMotor and updates the corresponding text box with this position.
    /// Opens the serial port associated with the motor, logs the action, and handles the UI update. 
    /// Logs an error if the serial port cannot be opened or if the corresponding text box for the motor is null.
    /// </summary>
    /// <param name="motor">The StepperMotor object whose position is to be retrieved and displayed.</param>
    private async void GetPositionHelper(StepperMotor motor)
    {
        // Attempt to open the serial port
        if (MagnetoSerialConsole.OpenSerialPort(motor.GetPortName()))
        {
            MagnetoLogger.Log("Port Open!", LogFactoryLogLevel.LogLevel.SUCCESS);

            // Log the action of getting the position
            var msg = "Using StepperMotor to get position";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);

            // Get the motor's position
            var pos = await motor.GetPosAsync();

            var textBox = GetMotorPositionTextBoxHelper(motor);
            if (textBox != null) // Full error checking in UITextHelper
            {
                UpdateUITextHelper.UpdateUIText(textBox, pos.ToString());
            }
        }
        else
        {
            // Log an error if the port could not be opened
            var errorMsg = "Port Closed.";
            MagnetoLogger.Log(errorMsg, LogFactoryLogLevel.LogLevel.ERROR);
        }
    }

    #endregion


    #region Motor Detail Helper Methods

    /// <summary>
    /// Helper to get controller type given motor name
    /// </summary>
    /// <param name="motorName">Name of the motor for which to return the controller type</param>
    /// <returns>Controller type</returns>
    private ControllerType GetControllerTypeHelper(string motorName)
    {
        switch (motorName)
        {
            case "sweep":
                return ControllerType.SWEEP;
            default: return ControllerType.BUILD;
        }
    }

    /// <summary>
    /// Helper to get motor axis
    /// </summary>
    /// <param name="motorName">Name of the motor for which to return the axis</param>
    /// <returns>Motor axis if request is successful; -1 if request failed</returns>
    private int GetMotorAxisHelper(string motorName)
    {
        if (_bm != null)
        {
            switch (motorName)
            {
                case "build":
                    return _bm.GetBuildMotor().GetAxis();
                case "powder":
                    return _bm.GetPowderMotor().GetAxis();
                case "sweep":
                    return _bm.GetSweepMotor().GetAxis();
                default: return _bm.GetPowderMotor().GetAxis();
            }
        }
        else
        {
            var msg = "Unable to get motor axis.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
            return -1;
        }
    }

    /// <summary>
    /// Helper to get motor name, controller type, and motor axis given a motor
    /// </summary>
    /// <param name="motor"></param>
    /// <returns>Tuple containing motor name, controller type, and motor axis</returns>
    public (string motorName, ControllerType controllerType, int motorAxis) GetMotorDetailsHelper(StepperMotor motor)
    {
        // Get the name of the current motor
        var motorName = motor.GetMotorName();

        // Get the controller type using a helper method
        var controllerType = GetControllerTypeHelper(motorName);

        // Get the motor axis using a helper method
        var motorAxis = GetMotorAxisHelper(motorName);

        return (motorName, controllerType, motorAxis);
    }

    #endregion


    #region Position Methods

    private void GetBuildMotorCurrentPositionButton_Click(object sender, RoutedEventArgs e)
    {
        var msg = "GetBuildMotorCurrentPositionButton Clicked...";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);

        if (_buildMotor != null)
        {
            GetPositionHelper(_buildMotor);

            //TODO: Update textbox with position

        }
        else
        {
            MagnetoLogger.Log("Build Motor is null, cannot get position.", LogFactoryLogLevel.LogLevel.ERROR);
        }
    }

    private void GetPowderMotorCurrentPositionButton_Click(object sender, RoutedEventArgs e)
    {

    }

    private void GetSweepMotorCurrentPositionButton_Click(object sender, RoutedEventArgs e)
    {

    }

    #endregion


    #region Motor Movement Methods

    /// <summary>
    /// Get motor details for a given motor to add movement request to queue
    /// </summary>
    /// <param name="motor">Motor to get details about</param>
    /// <returns>Tuple containing validity of request and motor details (if successful)</returns>
    private (bool isValid, MotorDetails? motorDetails) PrepareMotorOperation(StepperMotor motor)
    {
        if (motor == null)
        {
            MagnetoLogger.Log("Invalid motor request or motor is null.", LogFactoryLogLevel.LogLevel.ERROR);
            _ = PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", "No motor selected.");
            return (false, null);
        }

        if (!MagnetoSerialConsole.OpenSerialPort(motor.GetPortName()))
        {
            MagnetoLogger.Log("Failed to open port.", LogFactoryLogLevel.LogLevel.ERROR);
            _ = PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", "Failed to open serial port.");
            return (false, null);
        }

        // Get motor name from motor
        var motorName = motor.GetMotorName();

        // Get motor details based on motor name
        ControllerType controllerType = GetControllerTypeHelper(motorName);
        var motorAxis = GetMotorAxisHelper(motorName);

        return (true, new MotorDetails(motorName, controllerType, motorAxis));
    }

    /// <summary>
    /// Move motors by adding move command to move motors to build manager
    /// </summary>
    /// <param name="motor">Currently selected motor</param>
    /// <param name="isAbsolute">Indicates whether to move motor to absolute position or move motor relative to current position</param>
    /// <param name="value">Position to move to/distance to move</param>
    /// <returns></returns>
    private async Task ExecuteMovementCommand(StepperMotor motor, bool isAbsolute, double value)
    {
        var msg = "";

        if (MissionControl == null)
        {
            msg = "Failed to access mission control.";
            await PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", $"Mission control offline.");
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
            return;
        }
        else
        {
            // Get details for this motor
            var operationResult = PrepareMotorOperation(motor);

            if (!operationResult.isValid || operationResult.motorDetails == null)
            {
                msg = "Motor preparation failed or motor details not available.";
                MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);

                // TODO: Handle the error appropriately

                return;
            }
            else
            {
                // Update movement boolean
                _movingMotorToTarget = true;

                // Ternary used to get control type
                CommandType commandType = isAbsolute ? CommandType.AbsoluteMove : CommandType.RelativeMove;

                // Get controller type from motor details
                ControllerType controllerType = operationResult.motorDetails.Value.ControllerType;

                // Get motor axis from motor details
                var motorAxis = operationResult.motorDetails.Value.MotorAxis;

                // Ask for position to update text box below
                _ = await _bm.AddCommand(controllerType, motorAxis, commandType, value);

                try
                {
                    // Call AddCommand with CommandType.PositionQuery to get the motor's position
                    var position = await _bm.AddCommand(controllerType, motorAxis, CommandType.PositionQuery, 0);

                    MagnetoLogger.Log($"Position of motor on axis {motorAxis} is {position}", LogFactoryLogLevel.LogLevel.SUCCESS);
                }
                catch (Exception ex)
                {
                    MagnetoLogger.Log($"Failed to get motor position: {ex.Message}", LogFactoryLogLevel.LogLevel.ERROR);
                }

                // Update text box
                UpdateMotorPositionTextBox(motor);

                // Update movement boolean
                _movingMotorToTarget = false;
            }
        }
    }

    // TODO: Update these
    private TextBox GetCorrespondingAbsMoveTextBox(StepperMotor motor)
    {
        return BuildMotorCurrentPositionTextBox;
    }

    private TextBox GetCorrespondingRelMoveTextBox(StepperMotor motor)
    {
        return BuildMotorCurrentPositionTextBox;
    }

    /// <summary>
    /// Move motor method executes absolute and relative moves
    /// </summary>
    /// <param name="isAbsolute">Indicates whether move is absolute or relative</param>
    private async void MoveMotor(StepperMotor motor, TextBox absTextBox, TextBox stepTextBox, bool isAbsolute)
    {
        // Exit if no motor is selected
        if (motor == null)
        {
            LogAndDisplayMessage(LogFactoryLogLevel.LogLevel.ERROR, this.Content.XamlRoot, 
                "Invalid motor request or Current Test Motor is null.", "No motor selected.");
            return;
        }

        if (double.TryParse(absTextBox.Text, out var pos))
        {
            var distance = isAbsolute ? double.Parse(absTextBox.Text) : double.Parse(stepTextBox.Text);
            await ExecuteMovementCommand(motor, isAbsolute, distance);
        }
        else
        {
            await PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", $"\"{absTextBox.Text}\" is not a valid position. Please make sure you entered a number in the textbox.");
            return;
        }
    }

    private void UniformMotorMove(bool isAbsolute, double dist)
    {
        if (_powderMotor != null)
        {
            if (_buildMotor != null)
            {
                // Move powder motor up print height
                _ = ExecuteMovementCommand(_powderMotor, false, dist);

                // Move build motor down print height
                _ = ExecuteMovementCommand(_buildMotor, false, dist);
            }
            else
            {
                LogAndDisplayMessage(LogFactoryLogLevel.LogLevel.ERROR, this.XamlRoot, "Build Motor is null.", "Build motor is not connected.");
            }
        }
        else
        {
            LogAndDisplayMessage(LogFactoryLogLevel.LogLevel.ERROR, this.XamlRoot, "Powder Motor is null.", "Powder motor is not connected.");
        }
    }

    /// <summary>
    /// Move motor by incremental value obtained from increment text box
    /// </summary>
    /// <param name="motor">Currently selected motor</param>
    /// <param name="increment">Indicates whether move is incremental (positive direction/up) or decremental (down) (true = increment)</param>
    private async void StepMotor(StepperMotor motor, bool increment)
    {
        TextBox textbox = GetStepTextBoxHelper(motor);
        if (textbox == null || !double.TryParse(textbox.Text, out var dist))
        {
            _ = PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", "Invalid input in increment text box.");
            return;
        }
        // Take the absolute value of distance entered in text box
        dist = Math.Abs(dist);

        // Update text box value to absolute value so user knows
        textbox.Text = dist.ToString();

        // Execute move
        await ExecuteMovementCommand(motor, false, increment ? dist : -dist);
    }

    #endregion


    #region Absolute/Relative Motor Movement Button Methods



    #endregion


    #region Step Motor Button Commands

    private void StepBuildMotorUpButton_Click(object sender, RoutedEventArgs e)
    {
        var inrement = true;
        if (_buildMotor != null)
        {
            StepMotor(_buildMotor, inrement);
        }
    }

    private void StepBuildMotorDownButton_Click(object sender, RoutedEventArgs e)
    {
        var inrement = false;
        if (_buildMotor != null)
        {
            StepMotor(_buildMotor, inrement);
        }
    }

    private void StepPowderMotorUpButton_Click(object sender, RoutedEventArgs e)
    {
        var inrement = true;
        if (_powderMotor != null)
        {
            StepMotor(_powderMotor, inrement);
        }
    }

    private void StepPowderMotorDownButton_Click(object sender, RoutedEventArgs e)
    {
        var inrement = false;
        if (_powderMotor != null)
        {
            StepMotor(_powderMotor, inrement);
        }
    }

    private void StepSweepMotorUpButton_Click(object sender, RoutedEventArgs e)
    {
        var inrement = true;
        if (_sweepMotor != null)
        {
            StepMotor(_sweepMotor, inrement);
        }
    }

    private void StepSweepMotorDownButton_Click(object sender, RoutedEventArgs e)
    {
        var inrement = false;
        if (_sweepMotor != null)
        {
            StepMotor(_sweepMotor, inrement);
        }
    }

    #endregion


    #region Sweep Button Commands

    private void SweepButton_Click(object sender, RoutedEventArgs e)
    {
        var isAbsolute = true;
        if (_sweepMotor != null)
        {
            _ = ExecuteMovementCommand(_sweepMotor, isAbsolute, 280);
        }
        else
        {
            LogAndDisplayMessage(LogFactoryLogLevel.LogLevel.ERROR, this.Content.XamlRoot, "Could not find sweep motor.", "_sweepMotor is null");
        }
    }

    private void HomeSweepButton_Click(object sender, RoutedEventArgs e)
    {
        var isAbsolute = true;
        if (_sweepMotor != null)
        {
            _ = ExecuteMovementCommand(_sweepMotor, isAbsolute, 0);
        }
        else
        {
            LogAndDisplayMessage(LogFactoryLogLevel.LogLevel.ERROR, this.Content.XamlRoot, "Could not find sweep motor.", "_sweepMotor is null");
        }
    }

    private void StopSweepButton_Click(object sender, RoutedEventArgs e)
    {
        var msg = "StopSweepButton_Click Method not implemented.";
        LogAndDisplayMessage(LogFactoryLogLevel.LogLevel.ERROR, this.Content.XamlRoot, msg);
    }

    #endregion


    #region Marking Commands

    private ExecStatus ValidateJob(string fullPath)
    {
        MagnetoLogger.Log("Getting job...", Core.Contracts.Services.LogFactoryLogLevel.LogLevel.VERBOSE);

        if (!Directory.Exists(_jobDirectory))
        {
            MagnetoLogger.Log("Directory does not exist. Cannot get job.", Core.Contracts.Services.LogFactoryLogLevel.LogLevel.ERROR);
            return ExecStatus.Failure;
        }

        if (!File.Exists(fullPath))
        {
            MagnetoLogger.Log($"File not found: {fullPath}", Core.Contracts.Services.LogFactoryLogLevel.LogLevel.ERROR);
            return ExecStatus.Failure;
        }

        return ExecStatus.Success;
    }

    public static int SetRedPointerMode(RedPointerMode mode)
    {
        // returns void
        cci.ScSetLongValue((int)ScComSAMLightClientCtrlValueTypes.scComSAMLightClientCtrlLongValueTypeRedpointerMode, (int)mode);

        // TODO: Replace once we figure out how to interact with error codes form SAM
        return (int)ExecStatus.Success;
    }

    public int StartRedPointer()
    {
        LogMessage("Starting red pointer", Core.Contracts.Services.LogFactoryLogLevel.LogLevel.SUCCESS);

        if (cci.ScIsRunning() == 0)
        {
            LogMessage("Cannot Mark; WaveRunner is closed.", Core.Contracts.Services.LogFactoryLogLevel.LogLevel.ERROR, "SAMLight not found");
            StartMarkButton.IsEnabled = false;
        }

        LogMessage("Sending Objects!", Core.Contracts.Services.LogFactoryLogLevel.LogLevel.SUCCESS); // Update UI with status

        // load demo job file
        cci.ScLoadJob(_fullJobFilePath, 1, 1, 0);

        // returns void
        cci.ScExecCommand((int)ScComSAMLightClientCtrlExecCommandConstants.scComSAMLightClientCtrlExecCommandRedPointerStart);

        // TODO: Replace once we figure out how to interact with error codes form SAM
        return (int)ExecStatus.Success;
    }

    public int StopRedPointer()
    {
        LogMessage("Stopping red pointer", Core.Contracts.Services.LogFactoryLogLevel.LogLevel.SUCCESS);

        // returns void
        cci.ScExecCommand((int)ScComSAMLightClientCtrlExecCommandConstants.scComSAMLightClientCtrlExecCommandRedPointerStop);

        // make sure laser does not mark when stopping red pointer
        cci.ScStopMarking();

        // TODO: Replace once we figure out how to interact with error codes form SAM
        return (int)ExecStatus.Success;
    }

    public async Task<ExecStatus> MarkEntityAsync()
    {
        if (cci.ScIsRunning() == 0)
        {
            LogMessage("Cannot Mark; WaveRunner is closed.", Core.Contracts.Services.LogFactoryLogLevel.LogLevel.ERROR, "SAMLight not found");
            StartMarkButton.IsEnabled = false;
            return ExecStatus.Failure;
        }

        LogMessage("Sending Objects!", Core.Contracts.Services.LogFactoryLogLevel.LogLevel.SUCCESS); // Update UI with status

        // load demo job file
        cci.ScLoadJob(_fullJobFilePath, 1, 1, 0);

        var msg = $"Loaded file at path: {_fullJobFilePath} for marking...";

        MagnetoLogger.Log(msg, Core.Contracts.Services.LogFactoryLogLevel.LogLevel.WARN);

        try
        {
            // TODO: Test 1
            cci.ScMarkEntityByName("", 0); // 0 returns control to the user immediately
            LogMessage("Marking!", Core.Contracts.Services.LogFactoryLogLevel.LogLevel.WARN, "SAMLight is Marking...");

            // Wait for marking to complete
            while (cci.ScIsMarking() != 0)
            {
                await Task.Delay(100); // Use a delay to throttle the loop for checking marking status
            }

            cci.ScStopMarking();
            LogMessage("Done Marking", Core.Contracts.Services.LogFactoryLogLevel.LogLevel.SUCCESS, "SAMLight is done marking.");
            StartMarkButton.IsEnabled = true; // Allow retrying

            return ExecStatus.Success;
        }
        catch (System.Runtime.InteropServices.COMException comEx)
        {
            LogMessage($"COM Exception: {comEx.Message}", Core.Contracts.Services.LogFactoryLogLevel.LogLevel.ERROR);
            StartMarkButton.IsEnabled = true; // Allow retrying
            return ExecStatus.Failure;
        }
    }

    #endregion


    #region Marking Buttons

    private void UseDefaultJobButton_Click(object sender, RoutedEventArgs e)
    {
        string path_to_image = "c:/path/to/test_print.sjf";
        var msg = "";

        // TODO: Check if path is valid
        var _validPath = true;

        if (_validPath)
        {
            // Add dummy string to text box
            // SelectedPrint is the name of the TextBox in PrintPage.xaml
            JobFileNameTextBox.Text = path_to_image;

            // Put a new image on the build manager
            MissionControl.CreateImageModel(path_to_image);

            // TODO: Toast Message: Using default thickness of {} get from config
            msg = "Setting every print layer's thickness to default thickness from MagnetoConfig";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.DEBUG);
            MissionControl.SetImageThickness(MissionControl.GetDefaultPrintLayerThickness());

            // Slice image
            MissionControl.SliceImage(); // TODO: IMAGE HANDLER references Magneto Config to control slice number: SliceImage calls SliceImage in build controller which calls ImageHandler
            //StartPrintButton.IsEnabled = true;

        }
        else
        {
            msg = "Cannot find print: Invalid file path.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
            return;
        }
    }

    private void UpdateDirectoryButton_Click(object sender, RoutedEventArgs e)
    {
        LogAndDisplayMessage(LogFactoryLogLevel.LogLevel.VERBOSE, this.Content.XamlRoot, "Job directory updated.");
        _jobDirectory = JobFileSearchDirectory.Text;
        StartMarkButton.IsEnabled = false;
    }

    private void GetJobButton_Click(object sender, RoutedEventArgs e)
    {
        var fullFilePath = Path.Combine(_jobDirectory, JobFileNameTextBox.Text);

        if (ValidateJob(fullFilePath) == ExecStatus.Success)
        {
            _fullJobFilePath = fullFilePath; // Assuming _fullJobFilePath is a class member
            StartMarkButton.IsEnabled = true;
            ToggleRedPointerButton.IsEnabled = true;
        }
        else
        {
            StartMarkButton.IsEnabled = false;
            ToggleRedPointerButton.IsEnabled = false;
        }
    }

    private void ToggleRedPointerButton_Click(object sender, RoutedEventArgs e)
    {
        _redPointerEnabled = !_redPointerEnabled;

        if (_redPointerEnabled)
        {
            MagnetoLogger.Log("Starting Red Pointer", Core.Contracts.Services.LogFactoryLogLevel.LogLevel.SUCCESS);
            StartRedPointer();
            ToggleRedPointerButton.Background = new SolidColorBrush(Colors.Red);
            StartMarkButton.IsEnabled = false; // Assume job validation does not change.
        }
        else
        {
            MagnetoLogger.Log("Stopping Red Pointer", Core.Contracts.Services.LogFactoryLogLevel.LogLevel.SUCCESS);
            StopRedPointer();
            ToggleRedPointerButton.Background = (SolidColorBrush)Microsoft.UI.Xaml.Application.Current.Resources["ButtonBackgroundThemeBrush"];
            // Re-enable StartMarkButton only if _fullJobFilePath is still valid
            StartMarkButton.IsEnabled = !string.IsNullOrEmpty(_fullJobFilePath) && File.Exists(_fullJobFilePath);
        }
    }


    private void StartMarkButton_Click(object sender, RoutedEventArgs e)
    {
        // File exists, proceed with marking
        var msg = $"Starting mark for file: {_fullJobFilePath}";
        MagnetoLogger.Log(msg, Core.Contracts.Services.LogFactoryLogLevel.LogLevel.VERBOSE);
        _ = MarkEntityAsync();
    }

    private void StopMarkButton_Click(object sender, RoutedEventArgs e)
    {
        var msg = "";

        if (cci.ScIsRunning() == 0)
        {
            msg = "SAMLight not found";
            MagnetoLogger.Log(msg, Core.Contracts.Services.LogFactoryLogLevel.LogLevel.ERROR);
            return;
        }

        LogMessage("Stopping Mark", Core.Contracts.Services.LogFactoryLogLevel.LogLevel.SUCCESS);

        msg = "SAMLight is stopping mark";
        MagnetoLogger.Log(msg, Core.Contracts.Services.LogFactoryLogLevel.LogLevel.WARN);

        cci.ScStopMarking();
    }

    #endregion


    #region Move to Next Layer Button Commands 

    private void UpdateLayerThicknessButton_Click(object sender, RoutedEventArgs e)
    {
        _layerThickness = int.Parse(SetLayerThicknessTextBox.Text);
        LayerThicknessTextBlock.Text = _layerThickness.ToString();
    }

    private void MoveToNextLayerStartPositionButton_Click(object sender, RoutedEventArgs e)
    {
        UniformMotorMove(false, _layerThickness);
    }

    #endregion


    #region Reset Button Commands

    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        UniformMotorMove(true, 0);

        // TODO: Clear page settings to start new print

    }

    #endregion


    #region UI Update Methods

    /// <summary>
    /// Updates the text box associated with a given motor name with the motor's current position.
    /// </summary>
    /// <param name="motorName">Name of the motor whose position needs to be updated in the UI.</param>
    /// <param name="motor">The motor object whose position is to be retrieved and displayed.</param>
    private void UpdateMotorPositionTextBox(StepperMotor motor)
    {
        MagnetoLogger.Log("Updating motor position text box.", LogFactoryLogLevel.LogLevel.SUCCESS);
        var motorName = motor.GetMotorName();
        var textBox = GetCorrespondingTextBox(motorName);
        if (textBox != null)
            textBox.Text = motor.GetCurrentPos().ToString();
    }

    #endregion

    
    #region Logging Methods
    private string GetPopupMessageType(LogFactoryLogLevel.LogLevel LogLevel)
    {
        switch (LogLevel)
        {
            case LogFactoryLogLevel.LogLevel.DEBUG:
                return "Debug";
            case LogFactoryLogLevel.LogLevel.VERBOSE:
                return "Info";
            case LogFactoryLogLevel.LogLevel.WARN:
                return "Warning";
            case LogFactoryLogLevel.LogLevel.ERROR:
                return "Error";
            case LogFactoryLogLevel.LogLevel.SUCCESS:
                return "Success";
            default:
                return "Unknown";
        }
    }

    private async void LogAndDisplayMessage(LogFactoryLogLevel.LogLevel LogLevel, XamlRoot xamlRoot, string LogMessage, string PopupMessage)
    {
        var PopupMessageType = GetPopupMessageType(LogLevel);

        MagnetoLogger.Log(LogMessage, LogLevel);
        await PopupInfo.ShowContentDialog(xamlRoot, PopupMessageType, PopupMessage);
    }

    private async void LogAndDisplayMessage(LogFactoryLogLevel.LogLevel LogLevel, XamlRoot xamlRoot, string msg)
    {
        var PopupMessageType = GetPopupMessageType(LogLevel);

        MagnetoLogger.Log(msg, LogLevel);
        await PopupInfo.ShowContentDialog(xamlRoot, PopupMessageType, msg);
    }

    private void LogMessage(string uiMessage, Core.Contracts.Services.LogFactoryLogLevel.LogLevel logLevel, string logMessage = null)
    {
        // Update UI with the message
        UpdateUITextHelper.UpdateUIText(IsMarkingText, uiMessage);

        // Use the provided log level for logging
        MagnetoLogger.Log(logMessage ?? uiMessage, logLevel);
    }

    #endregion
}
