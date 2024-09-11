using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Magneto.Desktop.WinUI.Core.Contracts.Services;
using Magneto.Desktop.WinUI.Core.Models.BuildModels;
using Magneto.Desktop.WinUI.Core.Services;
using static Magneto.Desktop.WinUI.Core.Models.BuildModels.ActuationManager;

namespace Magneto.Desktop.WinUI.Core.Models.Motor;
public class MotorService
{
    private MissionControl? _missionControl;
    private ActuationManager? _actuationManager;
    private StepperMotor? _powderMotor;
    private StepperMotor? _buildMotor;
    private StepperMotor? _sweepMotor;
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

    public MotorService(ActuationManager am)
    {
        // Initialize motor set up for test page
        SetUpTestMotors(am);

        // Initialize motor map to simplify coordinated calls below
        // Make sure this happens AFTER motor setup
        InitializeMotorMap();
    }

    private async void SetUpTestMotors(ActuationManager am)
    {
        // Set up each motor individually using the passed-in parameters
        SetUpMotor("powder", am.GetPowderMotor(), out _powderMotor);
        SetUpMotor("build", am.GetBuildMotor(), out _buildMotor);
        SetUpMotor("sweep", am.GetSweepMotor(), out _sweepMotor);

        // Since there's no _missionControl, you'll need to figure out how to get the BuildManager
        // if that's still necessary in this context.
        _actuationManager = am;

        // Optionally, get the positions of the motors after setting them up
        // GetMotorPositions(); // TODO: Fix this if needed
    }

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


    /// <summary>
    /// Initializes the dictionary mapping motor names to their corresponding StepperMotor objects.
    /// This map facilitates the retrieval of motor objects based on their names.
    /// </summary>
    private Dictionary<string, StepperMotor?>? _motorTextMap;

    private void InitializeMotorMap()
    {
        _motorTextMap = new Dictionary<string, StepperMotor?>
            {
                { "build", _buildMotor },
                { "powder", _powderMotor },
                { "sweep", _sweepMotor }
            };
    }

    #region Position Helper Methods

    // Helper method to get the position of an individual motor
    private async Task<double?> GetPositionHelper(StepperMotor motor)
    {
        if (MagnetoSerialConsole.OpenSerialPort(motor.GetPortName()))
        {
            // Retrieve motor position asynchronously
            var pos = await motor.GetPosAsync();

            // Log the motor position
            MagnetoLogger.Log($"Motor {motor.GetMotorName()} position: {pos}");

            // Return the motor position
            return pos;
        }
        else
        {
            // Log the error if the port cannot be opened
            MagnetoLogger.Log("Failed to open port.", LogFactoryLogLevel.LogLevel.ERROR);

            // Return null to indicate failure
            return null;
        }
    }


    #endregion

    #region Getters

    public ActuationManager? GetActuationManager() => _actuationManager;

    public StepperMotor? GetBuildMotor() => _buildMotor;
    public StepperMotor? GetPowderMotor() => _powderMotor;
    public StepperMotor? GetSweepMotor() => _sweepMotor;
    public ActuationManager? GetBuildManager() => _actuationManager;


    private async Task GetMotorPositionAfterMove((ControllerType controllerType, int motorAxis) motorDetails)
    {
        try
        {
            var position = await _actuationManager.AddCommand(motorDetails.controllerType, motorDetails.motorAxis, CommandType.PositionQuery, 0);
            MagnetoLogger.Log($"Motor position: {position}");
        }
        catch (Exception ex)
        {
            MagnetoLogger.Log($"Failed to get motor position: {ex.Message}", LogFactoryLogLevel.LogLevel.ERROR);
        }
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
    /// Helper to get motor axis
    /// </summary>
    /// <param name="motorName">Name of the motor for which to return the axis</param>
    /// <returns>Motor axis if request is successful; -1 if request failed</returns>
    private int GetMotorAxisHelper(string motorName)
    {
        if (_actuationManager != null)
        {
            switch (motorName)
            {
                case "build":
                    return _actuationManager.GetBuildMotor().GetAxis();
                case "powder":
                    return _actuationManager.GetPowderMotor().GetAxis();
                case "sweep":
                    return _actuationManager.GetSweepMotor().GetAxis();
                default: return _actuationManager.GetPowderMotor().GetAxis();
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

    #region Movement

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
            //_ = PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", "No motor selected.");
            return (false, null);
        }

        if (!MagnetoSerialConsole.OpenSerialPort(motor.GetPortName()))
        {
            MagnetoLogger.Log("Failed to open port.", LogFactoryLogLevel.LogLevel.ERROR);
            //_ = PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", "Failed to open serial port.");
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

        if (_missionControl == null)
        {
            msg = "Failed to access mission control.";
            //await PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", $"Mission control offline.");
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
                _ = await _actuationManager.AddCommand(controllerType, motorAxis, commandType, value);

                try
                {
                    // Call AddCommand with CommandType.PositionQuery to get the motor's position
                    var position = await _actuationManager.AddCommand(controllerType, motorAxis, CommandType.PositionQuery, 0);

                    MagnetoLogger.Log($"Position of motor on axis {motorAxis} is {position}", LogFactoryLogLevel.LogLevel.SUCCESS);
                }
                catch (Exception ex)
                {
                    MagnetoLogger.Log($"Failed to get motor position: {ex.Message}", LogFactoryLogLevel.LogLevel.ERROR);
                }

                // TODO: Call on page
                // Update text box
                //UpdateMotorPositionTextBox(motor);

                // Update movement boolean
                _movingMotorToTarget = false;
            }
        }
    }

    /// <summary>
    /// Move motor method executes absolute and relative moves
    /// </summary>
    /// <param name="isAbsolute">Indicates whether move is absolute or relative</param>
    private async void MoveMotor(StepperMotor motor, double pos, bool isAbsolute)
    {
        // Exit if no motor is selected
        if (motor == null)
        {
            MagnetoLogger.Log("Invalid motor request or Current Test Motor is null.", LogFactoryLogLevel.LogLevel.ERROR);
            //await PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", $"No motor selected.");
            return;
        }

        await ExecuteMovementCommand(motor, isAbsolute, pos);
        // TODO: Return success or failure
        // TODO: Handle error checking on position type on page
    }

    /// <summary>
    /// Move motor by incremental value obtained from increment text box
    /// </summary>
    /// <param name="motor">Currently selected motor</param>
    /// <param name="increment">Indicates whether move is incremental (positive direction/up) or decremental (down) (true = increment)</param>
    private async void IncrementMotor(StepperMotor motor, double dist, bool increment)
    {
        // Execute move
        await ExecuteMovementCommand(motor, false, increment ? dist : -dist);
    }

    private async Task HomeMotorHelperAsync(StepperMotor motor)
    {
        var msg = "Using helper to home motors...";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);

        if (_actuationManager != null)
        {
            if (motor != null)
            {
                var motorDetails = GetMotorDetailsHelper(motor);

                _ = _actuationManager.AddCommand(motorDetails.controllerType, motorDetails.motorAxis, CommandType.AbsoluteMove, motor.GetHomePos());

                // Call try catch block to send command to get position to motor
                // (Required to update text box)
                try
                {
                    // Call AddCommand with CommandType.PositionQuery to get the motor's position
                    double position = await _actuationManager.AddCommand(motorDetails.controllerType, motorDetails.motorAxis, CommandType.PositionQuery, 0);

                    MagnetoLogger.Log($"Position of motor on axis {motorDetails.motorAxis} is {position}", LogFactoryLogLevel.LogLevel.SUCCESS);
                }
                catch (Exception ex)
                {
                    MagnetoLogger.Log($"Failed to get motor position: {ex.Message}", LogFactoryLogLevel.LogLevel.ERROR);
                }
                // TODO: make sure you call text box updates on page
                //UpdateMotorPositionTextBox(motor);
            }
            else
            {
                msg = "Current Test Motor is null.";
                MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
                // TODO: show popup message on page
                //await PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", "You must select a motor to home.");
            }
        }
        else
        {
            msg = "ActuationManager is null.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
            // TODO: show popup message on page
            //await PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", "Internal error. Try reloading the page.");
        }
    }

    #endregion


}

