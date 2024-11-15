using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Magneto.Desktop.WinUI.Core.Contracts.Services;
using Magneto.Desktop.WinUI.Core.Models.BuildModels;
using Magneto.Desktop.WinUI.Core.Models.Motor;
using Magneto.Desktop.WinUI.Core.Services;
using static Magneto.Desktop.WinUI.Core.Models.BuildModels.ActuationManager;
using Microsoft.UI.Xaml.Controls;
using Magneto.Desktop.WinUI.Helpers;
using Magneto.Desktop.WinUI.Popups;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;
using Magneto.Desktop.WinUI.Core;
using CommunityToolkit.WinUI.UI.Controls.TextToolbarSymbols;

namespace Magneto.Desktop.WinUI;
public class MotorPageService
{
    private ActuationManager? _actuationManager;

    public StepperMotor? buildMotor;
    public StepperMotor? powderMotor;
    public StepperMotor? sweepMotor;

    private bool _buildMotorSelected = false;
    private bool _powderMotorSelected = false;
    private bool _sweepMotorSelected = false;
    
    private bool _movingMotorToTarget = false;

    #region UI Variables

    public Button selectBuildMotorButton { get; set; }
    public Button selectPowderMotorButton { get; set; }
    public Button selectSweepMotorButton { get; set; }

    public Button selectBuildMotorInPrintButton { get; set; }
    public Button selectPowderMotorInPrintButton { get; set; }
    public Button selectSweepMotorInPrintButton { get; set; }

    public TextBox buildPositionTextBox { get; set; }
    public TextBox powderPositionTextBox { get; set; }
    public TextBox sweepPositionTextBox { get; set; }

    public TextBox incrBuildPositionTextBox { get; set; }
    public TextBox incrPowderPositionTextBox { get; set; }
    public TextBox incrSweepPositionTextBox { get; set; }

    public TextBox buildAbsMoveTextBox { get; set; }
    public TextBox powderAbsMoveTextBox { get; set; }
    public TextBox sweepAbsMoveTextBox { get; set; }

    #endregion

    /// <summary>
    /// Initializes the dictionary mapping motor names to their corresponding StepperMotor objects.
    /// This map facilitates the retrieval of motor objects based on their names.
    /// </summary>
    private Dictionary<string, StepperMotor?>? _motorTextMap;


    public MotorPageService(ActuationManager am, 
                            Button selectBuildButton, Button selectPowderButton, Button selectSweepButton, 
                            TextBox buildPosTextBox, TextBox powderPosTextBox, TextBox sweepPosTextBox,
                            TextBox incrBuildTextBox, TextBox incrPowderTextBox, TextBox incrSweepTextBox,
                            TextBox buildAbsMoveTextBox, TextBox powderAbsMoveTextBox, TextBox sweepAbsMoveTextBox)
    {
        // Set up event handers to communicate with motor controller ports
        ConfigurePortEventHandlers();

        // Initialize motor set up for test page
        InitMotors(am);

        // Initialize motor map to simplify coordinated calls below
        // Make sure this happens AFTER motor setup
        InitializeMotorMap();

        // Set up selection buttons
        selectBuildMotorButton = selectBuildButton;
        selectPowderMotorButton = selectPowderButton;
        selectSweepMotorButton = selectSweepButton;

        // Set up position text boxes
        buildPositionTextBox = buildPosTextBox;
        powderPositionTextBox = powderPosTextBox;
        sweepPositionTextBox = sweepPosTextBox;

        // Set up increment text boxes
        incrBuildPositionTextBox = incrBuildTextBox;
        incrPowderPositionTextBox = incrPowderTextBox;
        incrSweepPositionTextBox = incrSweepTextBox;

        // Setup abs position text box
        this.buildAbsMoveTextBox = buildAbsMoveTextBox;
        this.powderAbsMoveTextBox = powderAbsMoveTextBox;
        this.sweepAbsMoveTextBox = sweepAbsMoveTextBox;
    }

    public MotorPageService(ActuationManager am,
                            Button selectBuildButton, Button selectPowderButton, Button selectSweepButton,
                            Button selectBuildInPrintButton, Button selectPowderInPrintButton, Button selectSweepInPrintButton,
                            TextBox buildPosTextBox, TextBox powderPosTextBox, TextBox sweepPosTextBox,
                            TextBox incrBuildTextBox, TextBox incrPowderTextBox, TextBox incrSweepTextBox)
    {
        // Set up event handers to communicate with motor controller ports
        ConfigurePortEventHandlers();

        // Initialize motor set up for test page
        InitMotors(am);

        // Initialize motor map to simplify coordinated calls below
        // Make sure this happens AFTER motor setup
        InitializeMotorMap();

        // Set up selection buttons
        selectBuildMotorButton = selectBuildButton;
        selectPowderMotorButton = selectPowderButton;
        selectSweepMotorButton = selectSweepButton;

        // Set up inSitu buttons
        selectBuildMotorInPrintButton = selectBuildInPrintButton;
        selectPowderMotorInPrintButton = selectPowderInPrintButton;
        selectSweepMotorInPrintButton = selectSweepInPrintButton;

        // Set up position text boxes
        buildPositionTextBox = buildPosTextBox;
        powderPositionTextBox = powderPosTextBox;
        sweepPositionTextBox = sweepPosTextBox;

        // Set up increment text boxes
        incrBuildPositionTextBox = incrBuildTextBox;
        incrPowderPositionTextBox = incrPowderTextBox;
        incrSweepPositionTextBox = incrSweepTextBox;
    }

    #region Initial Setup

    private void ConfigurePortEventHandlers()
    {
        var msg = "Requesting port access for motor service.";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.DEBUG);
        MagnetoSerialConsole.LogAvailablePorts();

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

    private async void InitMotors(ActuationManager am)
    {
        // Set up each motor individually using the passed-in parameters
        HandleMotorInit("powder", am.GetPowderMotor(), out powderMotor);
        HandleMotorInit("build", am.GetBuildMotor(), out buildMotor);
        HandleMotorInit("sweep", am.GetSweepMotor(), out sweepMotor);

        // Since there's no _missionControl, you'll need to figure out how to get the BuildManager
        // if that's still necessary in this context.
        _actuationManager = am;

        // Optionally, get the positions of the motors after setting them up
        // GetMotorPositions(); // TODO: Fix this if needed
    }

    private void HandleMotorInit(string motorName, StepperMotor motor, out StepperMotor motorField)
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

    private void InitializeMotorMap()
    {
        _motorTextMap = new Dictionary<string, StepperMotor?>
            {
                { "build", buildMotor },
                { "powder", powderMotor },
                { "sweep", sweepMotor }
            };
    }

    #endregion

    #region Getters

    public ActuationManager GetActuationManager()
    {
        return _actuationManager;
    }

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
    /// Helper to get motor name, controller type, and motor axis given a motor
    /// </summary>
    /// <param name="motor"></param>
    /// <returns>Tuple containing motor name, controller type, and motor axis</returns>
    public (string motorName, ControllerType controllerType, int motorAxis) GetMotorDetailsHelper(StepperMotor motor)
    {
        return (motor.GetMotorName(), GetControllerTypeHelper(motor.GetMotorName()), motor.GetAxis());
    }

    #endregion

    #region Motor Movement Methods

    public async Task<int> MoveMotorAbs(StepperMotor motor, TextBox textBox)
    {
        if (textBox == null || !double.TryParse(textBox.Text, out var value))
        {
            var msg = $"invalid input in {motor.GetMotorName} text box: {textBox.Text}";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
            return 0;
        }
        else
        {
            var dist = double.Parse(textBox.Text);
            await _actuationManager.AddCommand(GetControllerTypeHelper(motor.GetMotorName()), motor.GetAxis(), CommandType.AbsoluteMove, dist);
            return 1;
        }
    }

    public async Task<int> MoveMotorRel(StepperMotor motor, TextBox textBox, bool moveUp)
    {
        if (textBox == null || !double.TryParse(textBox.Text, out var value))
        {
            var msg = $"invalid input in {motor.GetMotorName} text box: {textBox.Text}";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
            return 0;
        }
        else
        {
            // Convert distance to an absolute number to avoid confusing user
            var dist = Math.Abs(double.Parse(textBox.Text));

            // Update the text box with corrected distance
            textBox.Text = dist.ToString();

            // Check the direction we're moving
            dist = moveUp ? dist : -dist;

            // Move motor
            // NOTE: when called, you must await the return to get the integer value
            // Otherwise returns some weird string
            await _actuationManager.AddCommand(GetControllerTypeHelper(motor.GetMotorName()), motor.GetAxis(), CommandType.RelativeMove, dist);

            return 1;
        }
    }

    public async Task<int> MoveToNextLayer(double defaultLayerHeight)
    {
        // move sweep motor home
        await HomeMotor(sweepMotor);

        // move powder motor down by layer height
        await _actuationManager.AddCommand(GetControllerTypeHelper(powderMotor.GetMotorName()), powderMotor.GetAxis(), CommandType.RelativeMove, defaultLayerHeight);

        // move build motor down by layer height
        await _actuationManager.AddCommand(GetControllerTypeHelper(buildMotor.GetMotorName()), buildMotor.GetAxis(), CommandType.RelativeMove, -defaultLayerHeight);

        // distribute powder
        SweepLeft();

        return 1;
    }

    public async Task<int> StopMotor(StepperMotor motor, TextBox textBox)
    {
        if (textBox == null || !double.TryParse(textBox.Text, out var value))
        {
            var msg = $"invalid input in {motor.GetMotorName} text box: {textBox.Text}";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
            return 0;
        }
        else
        {
            // stop motor
            // NOTE: when called, you must await the return to get the integer value
            // Otherwise returns some weird string
            await sweepMotor.StopMotor(); // do not go through actuator; 

            return 1;
        }
    }

    public void SweepLeft()
    {
        _actuationManager.AddCommand(GetControllerTypeHelper(sweepMotor.GetMotorName()), sweepMotor.GetAxis(), CommandType.AbsoluteMove, (sweepMotor.GetMaxPos() - 2)); // NOTE: Subtracting 2 from max position for tolerance...probs not needed in long run
    }

    public async Task<int> HomeMotor(StepperMotor motor)
    {
        await _actuationManager.AddCommand(GetControllerTypeHelper(motor.GetMotorName()), motor.GetAxis(), CommandType.AbsoluteMove, motor.GetHomePos());
        UpdateMotorPositionTextBox(motor);
        return 1;
    }

    #endregion

    #region Movement Handlers

    public async void HandleGetPosition(StepperMotor motor, TextBox textBox)
    {
        var msg = "Get position button clicked...";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);

        if (motor != null)
        {
            if (motor != null)
            {
                //var motorDetails = GetMotorDetailsHelper(motor);
                var pos = await motor.GetPosAsync(); // TODO: figure out why AddCommand returns 0...
                if (textBox != null) // Full error checking in UITextHelper
                {
                    UpdateUITextHelper.UpdateUIText(textBox, pos.ToString());
                }
                SelectMotorHelper(motor);
            }
        }
        else
        {
            MagnetoLogger.Log($"{motor.GetPortName()} is null, cannot get position.", LogFactoryLogLevel.LogLevel.ERROR);
        }
    }

    public void HandleAbsMove(StepperMotor motor, TextBox textBox, XamlRoot xamlRoot)
    {
        var msg = $"{motor.GetMotorName()} abs move button clicked.";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.SUCCESS);
        if (motor != null)
        {
            var moveIsAbs = true;
            var moveUp = true; // Does not matter what we put here; unused in absolute move
            MoveMotorAndUpdateUI(motor, textBox, moveIsAbs, moveUp, xamlRoot);
        }
        else
        {
            msg = "Cannot move build motor. Motor is null.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
        }
    }

    public void HandleRelMove(StepperMotor motor, TextBox textBox, bool moveUp, XamlRoot xamlRoot)
    {
        var moveIsAbs = false;
        if (motor != null)
        {
            MoveMotorAndUpdateUI(motor, textBox, moveIsAbs, moveUp, xamlRoot);
        }
    }

    public void HandleRelMoveInSitu(StepperMotor motor, TextBox textBox, bool moveUp, XamlRoot xamlRoot)
    {
        var moveIsAbs = false;
        var inSitu = true;

        if (motor != null)
        {
            MoveMotorAndUpdateUI(motor, textBox, moveIsAbs, moveUp, inSitu, xamlRoot);
        }
    }

    public void HandleHomeMotor(StepperMotor motor, TextBox positionTextBox)
    {
        MagnetoLogger.Log("Homing Motor.", LogFactoryLogLevel.LogLevel.VERBOSE);

        if (motor != null)
        {
            _ = HomeMotor(motor);
            SelectMotorHelper(motor);
        }
        else
        {
            MagnetoLogger.Log($"Cannot home {motor.GetMotorName()} motor: motor value is null.", LogFactoryLogLevel.LogLevel.ERROR);
        }
    }

    #endregion

    #region Select Motor Helper Methods

    /// <summary>
    /// Selects the given StepperMotor as the current test motor, updates the UI to reflect this selection,
    /// and toggles the selection status. Clears the position text box and updates the background color of motor selection buttons.
    /// </summary>
    /// <param name="motor">The StepperMotor to be selected as the current test motor.</param>
    /// <param name="positionTextBox">The TextBox associated with the motor, to be cleared upon selection.</param>
    /// <param name="thisMotorSelected">A reference to a boolean flag indicating the selection status of this motor.</param>
    public void SelectMotorUIHelper(StepperMotor motor, ref bool thisMotorSelected)
    {
        // Update button backgrounds and selection flags
        selectBuildMotorButton.Background = new SolidColorBrush(buildMotor == motor ? Colors.Green : Colors.DimGray);
        _buildMotorSelected = buildMotor == motor;

        selectPowderMotorButton.Background = new SolidColorBrush(powderMotor == motor ? Colors.Green : Colors.DimGray);
        _powderMotorSelected = powderMotor == motor;

        selectSweepMotorButton.Background = new SolidColorBrush(sweepMotor == motor ? Colors.Green : Colors.DimGray);
        _sweepMotorSelected = sweepMotor == motor;

        // Update the selection flag for this motor
        thisMotorSelected = !thisMotorSelected;
    }

    public void SelectMotorUIInPrintHelper(StepperMotor motor, ref bool thisMotorSelected)
    {
        // Update button backgrounds and selection flags
        selectBuildMotorInPrintButton.Background = new SolidColorBrush(buildMotor == motor ? Colors.Green : Colors.DimGray);
        _buildMotorSelected = buildMotor == motor;

        selectPowderMotorInPrintButton.Background = new SolidColorBrush(powderMotor == motor ? Colors.Green : Colors.DimGray);
        _powderMotorSelected = powderMotor == motor;

        selectSweepMotorInPrintButton.Background = new SolidColorBrush(sweepMotor == motor ? Colors.Green : Colors.DimGray);
        _sweepMotorSelected = sweepMotor == motor;

        // Update the selection flag for this motor
        thisMotorSelected = !thisMotorSelected;
    }

    /// <summary>
    /// Wrapper for motor build motor selection code
    /// </summary>
    public void SelectBuildMotor()
    {
        if (buildMotor != null)
        {
            SelectMotorUIHelper(buildMotor, ref _buildMotorSelected);
        }
        else
        {
            var msg = "Build Motor is null.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
        }
    }

    /// <summary>
    /// Wrapper for motor powder motor selection code
    /// </summary>
    public void SelectPowderMotor()
    {
        if (powderMotor != null)
        {
            SelectMotorUIHelper(powderMotor, ref _powderMotorSelected);
        }
        else
        {
            var msg = "Powder Motor is null.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
        }
    }

    /// <summary>
    /// Wrapper for motor sweep motor selection code
    /// </summary>
    public void SelectSweepMotor()
    {
        if (sweepMotor != null)
        {
            SelectMotorUIHelper(sweepMotor, ref _sweepMotorSelected);
        }
        else
        {
            var msg = "Sweep Motor is null.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
        }
    }

    public void SelectBuildMotorInPrint()
    {
        if (buildMotor != null)
        {
            SelectMotorUIInPrintHelper(buildMotor, ref _buildMotorSelected);
        }
        else
        {
            var msg = "Build Motor is null.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
        }
    }

    /// <summary>
    /// Wrapper for motor powder motor selection code
    /// </summary>
    public void SelectPowderMotorInPrint()
    {
        if (powderMotor != null)
        {
            SelectMotorUIInPrintHelper(powderMotor, ref _powderMotorSelected);
        }
        else
        {
            var msg = "Powder Motor is null.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
        }
    }

    /// <summary>
    /// Wrapper for motor sweep motor selection code
    /// </summary>
    public void SelectSweepMotorInPrint()
    {
        if (sweepMotor != null)
        {
            SelectMotorUIInPrintHelper(sweepMotor, ref _sweepMotorSelected);
        }
        else
        {
            var msg = "Sweep Motor is null.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
        }
    }

    public void SelectMotorHelper(StepperMotor motor)
    {
        switch (motor.GetMotorName())
        {
            case "build":
                SelectBuildMotor();
                break;
            case "powder":
                SelectPowderMotor();
                break;
            case "sweep":
                SelectSweepMotor();
                break;
            default:
                break;
        }
    }

    public void SelectMotorInPrintHelper(StepperMotor motor)
    {
        switch (motor.GetMotorName())
        {
            case "build":
                SelectBuildMotorInPrint();
                break;
            case "powder":
                SelectPowderMotorInPrint();
                break;
            case "sweep":
                SelectSweepMotorInPrint();
                break;
            default:
                break;
        }
    }

    #endregion


    #region Move and Update UI Method
    public TextBox? GetMotorPositonTextBox(StepperMotor motor)
    {
        return motor.GetMotorName() switch
        {
            "build" => buildPositionTextBox,
            "powder" => powderPositionTextBox,
            "sweep" => sweepPositionTextBox,
            _ => null
        };
    }

    /// <summary>
    /// Updates the text box associated with a given motor name with the motor's current position.
    /// </summary>
    /// <param name="motorName">Name of the motor whose position needs to be updated in the UI.</param>
    /// <param name="motor">The motor object whose position is to be retrieved and displayed.</param>
    public async void UpdateMotorPositionTextBox(StepperMotor motor)
    {
        MagnetoLogger.Log("Updating motor position text box.", LogFactoryLogLevel.LogLevel.SUCCESS);
        // Call position add command first so we can update motor position in UI
        // TODO: WARNING -- this may cause issues when you decouple
        try
        {
            // Call AddCommand with CommandType.PositionQuery to get the motor's position
            var position = await _actuationManager.AddCommand(GetControllerTypeHelper(motor.GetMotorName()), motor.GetAxis(), CommandType.PositionQuery, 0);

            MagnetoLogger.Log($"Position of motor on axis {buildMotor.GetAxis()} is {position}", LogFactoryLogLevel.LogLevel.SUCCESS);
        }
        catch (Exception ex)
        {
            MagnetoLogger.Log($"Failed to get motor position: {ex.Message}", LogFactoryLogLevel.LogLevel.ERROR);
        }
        var textBox = GetMotorPositonTextBox(motor);
        if (textBox != null)
            textBox.Text = motor.GetCurrentPos().ToString();
    }

    public async void MoveMotorAndUpdateUI(StepperMotor motor, TextBox textBox, bool moveIsAbs, bool increment, XamlRoot xamlRoot)
    {
        var res = 0;
        if (motor != null)
        {
            // Select build motor button
            SelectMotorHelper(motor);

            if (moveIsAbs)
            {
                res = await MoveMotorAbs(motor, textBox);
            }
            else
            {
                res = await MoveMotorRel(motor, textBox, increment);
            }

            // If operation is successful, update text box
            if (res == 1)
            {
                UpdateMotorPositionTextBox(motor);
            }
            else
            {
                _ = PopupInfo.ShowContentDialog(xamlRoot, "Error", "Failed to send command to motor.");
            }
        }
        else
        {
            _ = PopupInfo.ShowContentDialog(xamlRoot, "Error", "Failed to select motor. Motor is null.");
        }
    }

    public async void MoveMotorAndUpdateUI(StepperMotor motor, TextBox textBox, bool moveIsAbs, bool increment, bool inSitu, XamlRoot xamlRoot)
    {
        var res = 0;
        if (motor != null)
        {
            // Select build motor button
            if (inSitu)
            {
                SelectMotorInPrintHelper(motor);
            }
            else
            {
                SelectMotorHelper(motor);
            }

            if (moveIsAbs)
            {
                res = await MoveMotorAbs(motor, textBox);
            }
            else
            {
                res = await MoveMotorRel(motor, textBox, increment);
            }

            // If operation is successful, update text box
            if (res == 1)
            {
                UpdateMotorPositionTextBox(motor);
            }
            else
            {
                _ = PopupInfo.ShowContentDialog(xamlRoot, "Error", "Failed to send command to motor.");
            }
        }
        else
        {
            _ = PopupInfo.ShowContentDialog(xamlRoot, "Error", "Failed to select motor. Motor is null.");
        }
    }

    public async void StopMotorAndUpdateUI(StepperMotor motor, TextBox textBox, XamlRoot xamlRoot)
    {
        var res = 0;
        if (motor != null)
        {
            // Select build motor button
            SelectMotorHelper(motor);

            res = await StopMotor(motor, textBox);

            // If operation is successful, update text box
            if (res == 1)
            {
                UpdateMotorPositionTextBox(motor);
            }
            else
            {
                _ = PopupInfo.ShowContentDialog(xamlRoot, "Error", "Failed to send command to motor.");
            }
        }
        else
        {
            _ = PopupInfo.ShowContentDialog(xamlRoot, "Error", "Cannot select motor. Motor is null.");
        }
    }

    #endregion
}

