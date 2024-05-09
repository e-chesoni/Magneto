using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Magneto.Desktop.WinUI.Core.Contracts.Services;
using Magneto.Desktop.WinUI.Core.Contracts.Services.Controllers;
using Magneto.Desktop.WinUI.Core.Models.Motor;
using Magneto.Desktop.WinUI.Core.Services;
using static Magneto.Desktop.WinUI.Core.Models.Motor.StepperMotor;

namespace Magneto.Desktop.WinUI.Core.Models.Controllers;

/// <summary>
/// Class used to synchronized motors attached to the same controller
/// </summary>
public class MotorController : IMotorController
{
    #region Private Variables
    /// <summary>
    /// Motor on controller 1 axis 1
    /// </summary>
    private StepperMotor _powderMotor { get; set; }

    /// <summary>
    /// Motor on controller 1 axis 2
    /// </summary>
    private StepperMotor _buildMotor { get; set; }

    /// <summary>
    /// Motor on controller 2, axis 1
    /// </summary>
    private StepperMotor _sweepMotor { get; set; }

    /// <summary>
    /// List of motors attached to controller
    /// </summary>
    private List<StepperMotor> _motorList { get; set; } = new List<StepperMotor>();

    private string _mcPort { get; set; }

    private Queue<string> commandQueue = new Queue<string>();

    private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
    
    private bool isCommandProcessing = false;

    public enum CommandType
    {
        AbsoluteMove, // Corresponds to "MVA" for absolute movements
        RelativeMove, // Corresponds to "MVR" for relative movements
        PositionQuery // Corresponds to "POS?" for querying current position
    }


    #endregion


    #region Constructors

    /// <summary>
    /// Constructor that accepts one stepper motor
    /// </summary>
    /// <param name="motor"></param> Motor to set on axis 1
    public MotorController(StepperMotor stepperMotor)
    {
        _mcPort = stepperMotor.GetPortName();
        _sweepMotor = stepperMotor;
        _motorList.Add(stepperMotor);
    }

    /// <summary>
    /// Constructor that accepts two stepper motors
    /// </summary>
    /// <param name="motor1"></param>
    /// <param name="motor2"></param>
    public MotorController(StepperMotor powderMotor, StepperMotor buildMotor)
    {
        _mcPort = powderMotor.GetPortName();
        _powderMotor = powderMotor;
        _buildMotor = buildMotor;
        _motorList.Add(powderMotor);
        _motorList.Add(buildMotor);
    }

    #endregion

    #region Getters

    public string GetPortName()
    {
        return _mcPort;
    }

    public List<StepperMotor> GetMotorList()
    {
        return _motorList;
    }

    public StepperMotor GetPowderMotor()
    {
        if (_powderMotor == null)
        {
            var msg = "Powder motor is not set. Are you calling GetPowderMotor() from the right controller?";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
        }
        return _powderMotor;
    }

    public StepperMotor GetBuildMotor()
    {
        if (_buildMotor == null)
        {
            var msg = "Build motor is not set. Are you calling GetBuildMotor() from the right controller?";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
        }
        return _buildMotor;
    }

    public StepperMotor GetSweepMotor()
    {
        if (_sweepMotor == null)
        {
            var msg = "Sweep motor is not set. Are you calling GetSweepMotor() from the right controller?";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
        }
        return _sweepMotor;
    }

    #endregion

    #region Queue Management

    /// <summary>
    /// Adds a command to the motor command queue.
    /// </summary>
    /// <param name="axis">The axis of the motor to which the command will be sent.</param>
    /// <param name="cmdType">The type of movement or query command.</param>
    /// <param name="dist">The distance or position for movement commands; ignored for position queries.</param>
    public void AddCommand(int axis, CommandType cmdType, double dist)
    {
        var msg = "Adding Command to Queue. Locking commandQueue";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);

        lock (commandQueue)
        {
            string command = $"{axis}";

            switch (cmdType)
            {
                case CommandType.AbsoluteMove:
                    command += $"MVA{dist}";
                    break;
                case CommandType.RelativeMove:
                    command += $"MVR{dist}";
                    break;
                case CommandType.PositionQuery:
                    command += "POS?";
                    break;
            }

            commandQueue.Enqueue(command);
            if (!isCommandProcessing)
            {
                isCommandProcessing = true;
                Task.Run(() => ProcessCommands());
            }
        }
    }

    private async Task ProcessCommands()
    {
        var msg = "Processing controller command queue...";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);

        while (commandQueue.Count > 0)
        {
            string command;
            lock (commandQueue)
            {
                command = commandQueue.Dequeue();
            }

            int axis = int.Parse(command.Substring(0, 1));
            string motorCommand = command.Substring(1);

            // Search motor list for id match; return that motor
            StepperMotor motor = _motorList.FirstOrDefault(motor => motor.GetID() % 10 == axis);

            if (motor != null)
            {
                msg = $"Found motor on axis: {axis}. Adding command associated with this motor to the control queue.";
                MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.SUCCESS);

                if (motorCommand.Contains("POS"))
                {
                    msg = $"Processing POS command...";
                    MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);

                    // TOOD: Return motor position

                }
                
                // TODO: Get command type (POS, MVR, or MVA)
                else if (motorCommand.Contains("MVA"))
                {
                    msg = $"Processing MVA command...";
                    MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);

                    double pos = double.Parse(motorCommand.Substring(3));
                    msg = $"Pos to get to: {pos.ToString()}";
                    MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);
                    await motor.MoveMotorAbsAsync(pos);
                }

                else if (motorCommand.Contains("MVR"))
                {
                    msg = $"Processing MVR command: {motorCommand}";
                    MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);

                    double step = double.Parse(motorCommand.Substring(3));

                    msg = $"Steps to process: {step.ToString()}";
                    MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);

                    await motor.MoveMotorRelAsync(step);
                }
            }
            else
            {
                msg = $"No motor with Axis {axis} found.";
                MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
            }
        }

        msg = "Done processing queue. Unlocking commandQueue";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);

        isCommandProcessing = false;
    }

    // TODO: Implement cancel token to respond to button click (see MotorQueue POC -- will need to modify)
    public void CancelOperations()
    {
        cancellationTokenSource.Cancel();
        Console.WriteLine("Cancellation requested.");
    }

    #endregion

    #region Movement Methods

    /// <summary>
    /// Perform sequenced motor movement
    /// Syntax to move both motors to absolute position
    /// </summary>
    /// <returns></returns> returns 0 on success, -1 on failure
    public async Task MoveMotorsAbsAsync(double thickness)
    {
        MagnetoLogger.Log("Moving Motors (PLURAL)",
            LogFactoryLogLevel.LogLevel.VERBOSE);

        // TODO: Thread blocking is not a great idea...
        // Find a more elegant way to handle running one motor at a time in the future
        foreach (var motor in _motorList)
        {
            await motor.MoveMotorAbsAsync(thickness);
            Thread.Sleep(2000);
        }
    }

    /// <summary>
    /// Move one motor relative to an absolute position
    /// </summary>
    /// <param name="axis"></param> The axis of the motor to move
    /// <param name="step"></param> Distance to move motor
    /// <returns></returns>
    public async Task MoveMotorByAxisAsync(int axis, double step)
    {

        var msg = "Moving Motor Absolute";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);

        // Search the controller _motorlist for a motor with axis == axis given
        // % 10 gets the last digit of the ID here
        // And axis is the last digit of the motor id
        StepperMotor motor = _motorList.FirstOrDefault(motor => motor.GetID() % 10 == axis);

        if ( motor != null)
        {
            msg = $"Found motor on axis: {axis}. Stepping motor absolute...";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.SUCCESS);
            await motor.MoveMotorAbsAsync(step);
        }
        else
        {
            msg = $"No motor with Axis {axis} found.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
        }
    }

    public async Task MoveMotorAbsAsync(StepperMotor motor, double step)
    {

        var msg = "Moving Motor Absolute";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);

        if (motor != null)
        {
            msg = $"Found {motor.GetMotorName()} on. Stepping motor absolute...";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.SUCCESS);
            await motor.MoveMotorAbsAsync(step);
        }
        else
        {
            msg = $"Motor not found.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
        }
    }

    /// <summary>
    /// Perform sequenced motor movement
    /// Syntax to move both motors to relative position
    /// </summary>
    /// <returns></returns> returns 0 on success, -1 on failure
    public async Task MoveMotorsRelAsync(double thickness)
    {
        var msg = "Moving Motors (PLURAL)...";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);

        // TODO: Thread blocking is not a great idea...
        // Find a more elegant way to handle running one motor at a time in the future
        foreach (var motor in _motorList)
        {
            await motor.MoveMotorRelAsync(thickness);
            //Thread.Sleep(2000);
        }
    }

    /// <summary>
    /// Move one motor relative to its current position
    /// </summary>
    /// <param name="axis"></param> The axis of the motor to move
    /// <param name="step"></param> Distance to move motor
    /// <returns></returns>
    public async Task MoveMotorRelAsync(int axis, double step)
    {
        var msg = "Moving Motor Relative";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);

        StepperMotor motor = _motorList.FirstOrDefault(motor => motor.GetID() % 10 == axis);

        if (motor != null)
        {
            msg = $"Found motor on axis: {axis}. Stepping motor relative to current position...";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.SUCCESS);
            await motor.MoveMotorRelAsync(step);
        }
        else
        {
            msg = $"No motor with Axis {axis} found.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
        }
    }

    // USED IN PRINT SEQUENCE
    public async Task MoveMotorRelAsync(StepperMotor motor, double step)
    {
        var msg = "Moving Motor Relative";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);

        if (motor != null)
        {
            msg = $"Found {motor.GetMotorName()} motor. Stepping motor {step} steps relative to current position...";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.SUCCESS);
            await motor.MoveMotorRelAsync(step);
        }
        else
        {
            msg = $"Motor is null. Are you sure you're accessing the right controller?";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
        }
    }

    /// <summary>
    /// Home all attached motors
    /// (Return all attached motors to zero position)
    /// </summary>
    public async Task HomeMotors()
    {
        var msg = "MotorController::HomeMotors -- Homing motors one at a time...";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);

        // Home each motor one at a time
        foreach (var motor in _motorList)
        {
            await motor.HomeMotor();
            MagnetoLogger.Log($"Motor {motor.GetID()} homed successfully.", LogFactoryLogLevel.LogLevel.SUCCESS);
        }
    }

    /// <summary>
    /// EMERGENCY STOP: Stop all motors attached to controller
    /// </summary>
    /// <returns></returns> returns 0 on success, -1 on failure
    public async Task StopMotors()
    {
        // Stop all motors concurrently
        var stopTasks = _motorList.Select(motor => motor.StopMotor());

        // Wait for all motors to stop
        await Task.WhenAll(stopTasks);
    }

    #endregion

    #region Status Methods

    /// <summary>
    /// Get the status of a motor with the requested ID
    /// </summary>
    /// <param name="motorId"></param>
    public MotorStatus GetMotorStatus(int motorId)
    {
        var msg = "Getting motor status";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);

        StepperMotor motor = _motorList.FirstOrDefault(motor => motor.GetID() == motorId);

        if (motor != null)
        { 
            return motor.GetStatus();
        }
        else
        {
            msg = "Invalid motor axis.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);
            return MotorStatus.Error;
        }
    }

    #endregion
}
