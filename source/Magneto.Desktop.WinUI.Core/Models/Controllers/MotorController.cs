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
    /// Motor on axis 1
    /// </summary>
    //private StepperMotor _motor1 { get; set; }

    /// <summary>
    /// Motor on axis 2
    /// </summary>
    //private StepperMotor _motor2 { get; set; }

    /// <summary>
    /// List of motors attached to controller
    /// </summary>
    private List<StepperMotor> _motorList { get; set; } = new List<StepperMotor>();

    private string _mcPort
    {
        get; set;
    }

    #endregion


    #region Constructors

    /// <summary>
    /// Constructor that accepts one stepper motor
    /// </summary>
    /// <param name="motor"></param> Motor to set on axis 1
    public MotorController(StepperMotor motor)
    {
        _mcPort = motor.GetPortName();
        //_motor1 = motor1;
        _motorList.Add(motor);
    }

    /// <summary>
    /// Constructor that accepts two stepper motors
    /// </summary>
    /// <param name="motor1"></param>
    /// <param name="motor2"></param>
    public MotorController(StepperMotor motor1, StepperMotor motor2)
    {
        _mcPort = motor1.GetPortName();
        //_motor1 = motor1;
        //_motor2 = motor2;
        _motorList.Add(motor1);
        _motorList.Add(motor2);
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

    #endregion

    #region Movement Methods

    /// <summary>
    /// Perform sequenced motor movement
    /// Syntax to move both motors to absolute position
    /// </summary>
    /// <returns></returns> returns 0 on success, -1 on failure
    public async Task MoveMotorsAbs(double thickness)
    {
        MagnetoLogger.Log("Moving Motors (PLURAL)",
            LogFactoryLogLevel.LogLevel.VERBOSE);

        // TODO: Thread blocking is not a great idea...
        // Find a more elegant way to handle running one motor at a time in the future
        foreach (var motor in _motorList)
        {
            await motor.MoveMotorAbs(thickness);
            Thread.Sleep(2000);
        }
    }

    /// <summary>
    /// Move one motor relative to an absolute position
    /// </summary>
    /// <param name="axis"></param> The axis of the motor to move
    /// <param name="step"></param> Distance to move motor
    /// <returns></returns>
    public async Task MoveMotorAbs(int axis, double step)
    {

        var msg = "Moving Motor Absolute";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);

        StepperMotor motor = _motorList.FirstOrDefault(motor => motor.GetID() % 10 == axis);

        if ( motor != null)
        {
            msg = $"Found motor on axis: {axis}. Stepping motor absolute...";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.SUCCESS);
            await motor.MoveMotorAbs(step);
        }
        else
        {
            msg = $"No motor with Axis {axis} found.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
        }
    }

    /// <summary>
    /// Perform sequenced motor movement
    /// Syntax to move both motors to relative position
    /// </summary>
    /// <returns></returns> returns 0 on success, -1 on failure
    public async Task MoveMotorsRel(double thickness)
    {
        var msg = "Moving Motors (PLURAL)...";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);

        // TODO: Thread blocking is not a great idea...
        // Find a more elegant way to handle running one motor at a time in the future
        foreach (var motor in _motorList)
        {
            await motor.MoveMotorRel(thickness);
            Thread.Sleep(2000);
        }
    }

    /// <summary>
    /// Move one motor relative to its current position
    /// </summary>
    /// <param name="axis"></param> The axis of the motor to move
    /// <param name="step"></param> Distance to move motor
    /// <returns></returns>
    public async Task MoveMotorRel(int axis, double step)
    {
        var msg = "Moving Motor Relative";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);

        StepperMotor motor = _motorList.FirstOrDefault(motor => motor.GetID() % 10 == axis);

        if (motor != null)
        {
            msg = $"Found motor on axis: {axis}. Stepping motor relative to current position...";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.SUCCESS);
            await motor.MoveMotorRel(step);
        }
        else
        {
            msg = $"No motor with Axis {axis} found.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
        }
    }


    /// <summary>
    /// Home all attached motors
    /// (Return all attached motors to zero position)
    /// </summary>
    public async Task HomeMotors()
    {
        var msg = "MotorController::HomeMotors -- Homing motors (PLURAL!)...";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);

        // TODO: Thread blocking is not a great idea...
        // Find a more elegant way to handle running one motor at a time in the future

        /*
        foreach (var motor in _motorList)
        {
            await motor.HomeMotor();
        }
        */

        // Start homing all motors concurrently
        var homeTasks = _motorList.Select(motor => motor.HomeMotor());

        // Wait for all motors to complete homing
        await Task.WhenAll(homeTasks);
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
