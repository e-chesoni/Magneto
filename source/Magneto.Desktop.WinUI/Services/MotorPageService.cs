using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Magneto.Desktop.WinUI.Core.Contracts.Services;
using Magneto.Desktop.WinUI.Core.Models.Print;
using Magneto.Desktop.WinUI.Core.Models.Motors;
using static Magneto.Desktop.WinUI.Core.Models.Print.ActuationManager;
using Microsoft.UI.Xaml.Controls;
using Magneto.Desktop.WinUI.Helpers;
using Magneto.Desktop.WinUI.Popups;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;
using Magneto.Desktop.WinUI.Core;
using CommunityToolkit.WinUI.UI.Controls.TextToolbarSymbols;
using Magneto.Desktop.WinUI.Models.UIControl;

namespace Magneto.Desktop.WinUI;
public class MotorPageService
{
    private ActuationManager? _actuationManager;

    public StepperMotor? buildMotor;
    public StepperMotor? powderMotor;
    public StepperMotor? sweepMotor;
    public double maxSweepPosition;

    public PrintUIControlGroupHelper printUiControlGroupHelper { get; set; }

    /// <summary>
    /// Initializes the dictionary mapping motor names to their corresponding StepperMotor objects.
    /// This map facilitates the retrieval of motor objects based on their names.
    /// </summary>
    private Dictionary<string, StepperMotor?>? _motorTextMap;

    /*
    public MotorPageService(ActuationManager am,
                            MotorUIControlGroup calibrateCtlGrp)
    {
        // Set up event handers to communicate with motor controller ports
        ConfigurePortEventHandlers();

        // Initialize motor set up for test page
        InitMotors(am);

        // Initialize motor map to simplify coordinated calls below
        // Make sure this happens AFTER motor setup
        InitializeMotorMap();

        printUiControlGroupHelper = new PrintUIControlGroupHelper(calibrateCtlGrp);
    }
    */
    public MotorPageService(ActuationManager am, PrintUIControlGroupHelper printCtlGrpHelper)
    {
        //_motorService.ConfigurePortEventHandlers();
        // Set up event handers to communicate with motor controller ports
        ConfigurePortEventHandlers();
        
        // Initialize motor set up for test page
        InitMotors(am);

        // Initialize motor map to simplify coordinated calls below
        // Make sure this happens AFTER motor setup
        InitializeMotorMap();

        printUiControlGroupHelper = new PrintUIControlGroupHelper(printCtlGrpHelper.calibrateMotorControlGroup, printCtlGrpHelper.printMotorControlGroup);
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
        _actuationManager = am;

        // Set up each motor individually using the passed-in parameters
        HandleMotorInit("powder", _actuationManager.GetPowderMotor(), out powderMotor);
        HandleMotorInit("build", _actuationManager.GetBuildMotor(), out buildMotor);
        HandleMotorInit("sweep", _actuationManager.GetSweepMotor(), out sweepMotor);

        // get maxSweepPosition
        maxSweepPosition = _actuationManager.GetSweepMotor().GetMaxPos() - 2; // NOTE: Subtracting 2 from max position for tolerance...probs not needed in long run

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

    public TextBox GetBuildPositionTextBox()
    {
        return printUiControlGroupHelper.calibrateMotorControlGroup.buildPositionTextBox;
    }

    public TextBox GetPowderPositionTextBox()
    {
        return printUiControlGroupHelper.calibrateMotorControlGroup.powderPositionTextBox;
    }

    public TextBox GetSweepPositionTextBox()
    {
        return printUiControlGroupHelper.calibrateMotorControlGroup.sweepPositionTextBox;
    }

    public TextBox GetBuildStepTextBox()
    {
        return printUiControlGroupHelper.calibrateMotorControlGroup.buildStepTextBox;
    }

    public TextBox GetPowderStepTextBox()
    {
        return printUiControlGroupHelper.calibrateMotorControlGroup.powderStepTextBox;
    }

    public TextBox GetSweepStepTextBox()
    {
        return printUiControlGroupHelper.calibrateMotorControlGroup.sweepStepTextBox;
    }

    public TextBox GetBuildAbsMoveTextBox()
    {
        return printUiControlGroupHelper.calibrateMotorControlGroup.buildAbsMoveTextBox;
    }

    public TextBox GetPowderAbsMoveTextBox()
    {
        return printUiControlGroupHelper.calibrateMotorControlGroup.powderAbsMoveTextBox;
    }

    public TextBox GetSweepAbsMoveTextBox()
    {
        return printUiControlGroupHelper.calibrateMotorControlGroup.sweepAbsMoveTextBox;
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
            if (_actuationManager != null)
            {
                // Move motor
                // NOTE: when called, you must await the return to get the integer value
                // Otherwise returns some weird string
                await _actuationManager.AddCommand(GetControllerTypeHelper(motor.GetMotorName()), motor.GetAxis(), CommandType.RelativeMove, dist);
            } else {
                var msg = $"Actuation manager is null.";
                MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
                return 0;
            }
            return 1;
        }
    }

    /// <summary>
    /// Homes sweep motor (moves right to min position 0), moves powder motor up 2x layer height, moves build motor down by layer height, then sweeps material onto build plate
    /// (moves sweep motor left to max sweep position)
    /// </summary>
    /// <param name="layerThickness"></param>
    /// <returns></returns>
    public async Task<int> LayerMove(double layerThickness)
    {
        var powderAmplifier = 2.5; // Quan requested we change this from 4-2.5 to conserve powder
        var lowerBuildForSweepDist = 2;

        if (_actuationManager != null)
        {
            // move build motor down for sweep
            await _actuationManager.AddCommand(GetControllerTypeHelper(buildMotor.GetMotorName()), buildMotor.GetAxis(), CommandType.RelativeMove, -lowerBuildForSweepDist);

            // home sweep motor
            await _actuationManager.AddCommand(GetControllerTypeHelper(sweepMotor.GetMotorName()), sweepMotor.GetAxis(), CommandType.AbsoluteMove, sweepMotor.GetHomePos());

            // move build motor back up to last mark height
            await _actuationManager.AddCommand(GetControllerTypeHelper(buildMotor.GetMotorName()), buildMotor.GetAxis(), CommandType.RelativeMove, lowerBuildForSweepDist);

            // move powder motor up by powder amp layer height (Prof. Tertuliano recommends powder motor moves 2-3x distance of build motor)
            await _actuationManager.AddCommand(GetControllerTypeHelper(powderMotor.GetMotorName()), powderMotor.GetAxis(), CommandType.RelativeMove, (powderAmplifier * layerThickness));

            // move build motor down by layer height
            await _actuationManager.AddCommand(GetControllerTypeHelper(buildMotor.GetMotorName()), buildMotor.GetAxis(), CommandType.RelativeMove, -layerThickness);

            // apply material to build plate
            await _actuationManager.AddCommand(GetControllerTypeHelper(sweepMotor.GetMotorName()), sweepMotor.GetAxis(), CommandType.AbsoluteMove, maxSweepPosition);

            // TEMPORARY SOLUTION: repeat last command to pad queue so we can use motors running check properly
            await _actuationManager.AddCommand(GetControllerTypeHelper(sweepMotor.GetMotorName()), sweepMotor.GetAxis(), CommandType.AbsoluteMove, maxSweepPosition); // TODO: change to wait for end command
        } else {
            var msg = $"Actuation manager is null.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
            return 0;
        }
        return 1;
    }

    public bool MotorsRunning()
    {
        // if queue is not empty, motors are running
        if (_actuationManager.MotorsRunning())
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    public async Task<int> StopSweepMotor(StepperMotor motor, TextBox textBox)
    {
        if (textBox == null || !double.TryParse(textBox.Text, out var value))
        {
            var msg = $"invalid input in {motor.GetMotorName} text box: {textBox.Text}";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
            return 0;
        }
        else
        {
            if (sweepMotor != null)
            {
                // stop motor
                // NOTE: when called, you must await the return to get the integer value
                // Otherwise returns some weird string
                await sweepMotor.StopMotor(); // do not go through actuator;
            }
            else
            {
                var msg = $"Sweep motor is null.";
                MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
                return 0;
            }
            return 1;
        }
    }

    /// <summary>
    /// Sweeps left to apply material to build plate
    /// </summary>
    public int SweepPowder()
    {
        if (_actuationManager != null)
        {
            _actuationManager.AddCommand(GetControllerTypeHelper(sweepMotor.GetMotorName()), sweepMotor.GetAxis(), CommandType.AbsoluteMove, (sweepMotor.GetMaxPos() - 2)); // NOTE: Subtracting 2 from max position for tolerance...probs not needed in long run
        } else
        {
            var msg = $"Actuation manager is null.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
            return 0;
        }
        return 1;
    }

    public async Task<int> HomeMotor(StepperMotor motor)
    {
        if (_actuationManager != null)
        {
            await _actuationManager.AddCommand(GetControllerTypeHelper(motor.GetMotorName()), motor.GetAxis(), CommandType.AbsoluteMove, motor.GetHomePos());
        }
        else
        {
            var msg = $"Actuation manager is null.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
            return 0;
        }
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
                printUiControlGroupHelper.SelectMotor(motor);
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
            msg = $"Cannot execute relative move on {motor.GetMotorName()} motor. Motor is null.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
        }
    }

    public void HandleRelMove(StepperMotor motor, TextBox textBox, bool moveUp, XamlRoot xamlRoot)
    {
        var msg = $"{motor.GetMotorName()} rel move button clicked.";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.SUCCESS);
        if (motor != null)
        {
            var moveIsAbs = false;
            MoveMotorAndUpdateUI(motor, textBox, moveIsAbs, moveUp, xamlRoot);
        }
        else
        {
            msg = $"Cannot execute relative move on {motor.GetMotorName()} motor. Motor is null.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
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

    public void HandleHomeMotorAndUpdateTextBox(StepperMotor motor, TextBox positionTextBox)
    {
        MagnetoLogger.Log("Homing Motor.", LogFactoryLogLevel.LogLevel.VERBOSE);

        if (motor != null)
        {
            _ = HomeMotor(motor);
            printUiControlGroupHelper.SelectMotor(motor);
        }
        else
        {
            MagnetoLogger.Log($"Cannot home {motor.GetMotorName()} motor: motor value is null.", LogFactoryLogLevel.LogLevel.ERROR);
        }
    }

    public void HandleHomeMotor(StepperMotor motor)
    {
        MagnetoLogger.Log("Homing Motor.", LogFactoryLogLevel.LogLevel.VERBOSE);

        if (motor != null)
        {
            _ = HomeMotor(motor);
            printUiControlGroupHelper.SelectMotor(motor);
        }
        else
        {
            MagnetoLogger.Log($"Cannot home {motor.GetMotorName()} motor: motor value is null.", LogFactoryLogLevel.LogLevel.ERROR);
        }
    }

    #endregion

    #region Move and Update UI Method
    public TextBox? GetMotorPositonTextBox(StepperMotor motor)
    {
        return motor.GetMotorName() switch
        {
            "build" => printUiControlGroupHelper.calibrateMotorControlGroup.buildPositionTextBox,
            "powder" => printUiControlGroupHelper.calibrateMotorControlGroup.powderPositionTextBox,
            "sweep" => printUiControlGroupHelper.calibrateMotorControlGroup.sweepPositionTextBox,
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
            printUiControlGroupHelper.SelectMotor(motor);

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
                printUiControlGroupHelper.SelectMotorInPrint(motor);
            }
            else
            {
                printUiControlGroupHelper.SelectMotor(motor);
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
            printUiControlGroupHelper.SelectMotor(motor);

            res = await StopSweepMotor(motor, textBox);

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

