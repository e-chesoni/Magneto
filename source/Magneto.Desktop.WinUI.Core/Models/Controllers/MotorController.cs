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
    private StepperMotor _motor1 { get; set; }

    /// <summary>
    /// Motor on axis 2
    /// </summary>
    private StepperMotor _motor2 { get; set; }

    private string _mcPort
    {
        get; set;
    }

    #endregion

    #region Constructors

    /// <summary>
    /// Constructor that accepts one stepper motor
    /// </summary>
    /// <param name="motor1"></param> Motor to set on axis 1
    public MotorController(StepperMotor motor1)
    {
        _mcPort = motor1.motorPort;
        _motor1 = motor1;
    }

    /// <summary>
    /// Constructor that accepts two stepper motors
    /// </summary>
    /// <param name="motor1"></param>
    /// <param name="motor2"></param>
    public MotorController(StepperMotor motor1, StepperMotor motor2)
    {
        _mcPort = motor1.motorPort;
        _motor1 = motor1;
        _motor2 = motor2;
    }

    #endregion

    #region Getters

    public string GetPortName()
    {
        return _mcPort;
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
        if (_motor1 != null)
        {
            await _motor1.MoveMotorAbs(thickness);
            Thread.Sleep(2000);
        }
        else
        {
            MagnetoLogger.Log("Motor 1 is null.",
                LogFactoryLogLevel.LogLevel.ERROR);
        }

        if (_motor2 != null)
        {
            await _motor2.MoveMotorAbs(thickness);
            Thread.Sleep(2000);
        }
        else
        {
            MagnetoLogger.Log("Motor 2 is null.",
                LogFactoryLogLevel.LogLevel.ERROR);
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
        MagnetoLogger.Log("Moving Motor Absolute",
            LogFactoryLogLevel.LogLevel.VERBOSE);

        switch(axis)
        {
            case 1:
                if (_motor1 != null)
                {
                    await _motor1.MoveMotorAbs(step);
                }
                else
                {
                    MagnetoLogger.Log("Motor 1 is null.",
                        LogFactoryLogLevel.LogLevel.ERROR);
                }
                break;
            case 2:
                if (_motor2 != null)
                {
                    await _motor2.MoveMotorAbs(step);
                }
                else
                {
                    MagnetoLogger.Log("Motor 2 is null.",
                        LogFactoryLogLevel.LogLevel.ERROR);
                }
                break;
            default:
                MagnetoLogger.Log("Invalid motor axis received.",
                    LogFactoryLogLevel.LogLevel.ERROR);
                break;
        }
    }

    /// <summary>
    /// Perform sequenced motor movement
    /// Syntax to move both motors to relative position
    /// </summary>
    /// <returns></returns> returns 0 on success, -1 on failure
    public async Task MoveMotorsRel(double thickness)
    {
        MagnetoLogger.Log("MotorController::MoveMotorsAbs -- Moving Motors (PLURAL)...",
            Contracts.Services.LogFactoryLogLevel.LogLevel.VERBOSE);

        // TODO: Thread blocking is not a great idea...
        // Find a more elegant way to handle running one motor at a time in the future
        if(_motor1 != null)
        {
            await _motor1.MoveMotorRel(thickness);
            Thread.Sleep(2000);
        }

        if (_motor2 != null)
        {
            await _motor2.MoveMotorRel(thickness);
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
        MagnetoLogger.Log("Moving Motor Relative",
            LogFactoryLogLevel.LogLevel.VERBOSE);

        switch (axis)
        {
            case 1:
                if (_motor1 != null)
                {
                    await _motor1.MoveMotorRel(step);
                }
                else
                {
                    MagnetoLogger.Log("Motor 1 is null.",
                        LogFactoryLogLevel.LogLevel.ERROR);
                }
                break;
            case 2:
                if (_motor2 != null)
                {
                    await _motor2.MoveMotorRel(step);
                }
                else
                {
                    MagnetoLogger.Log("Motor 2 is null.",
                        LogFactoryLogLevel.LogLevel.ERROR);
                }
                break;
            default:
                MagnetoLogger.Log("Invalid motor axis received.",
                    LogFactoryLogLevel.LogLevel.ERROR);
                break;
        }
    }


    /// <summary>
    /// Home all attached motors
    /// (Return all attached motors to zero position)
    /// </summary>
    public async Task HomeMotors()
    {
        MagnetoLogger.Log("MotorController::HomeMotors -- Homing motors (PLURAL!)...",
            Contracts.Services.LogFactoryLogLevel.LogLevel.VERBOSE);

        await _motor1.HomeMotor();
        await _motor2.HomeMotor();
    }

    /// <summary>
    /// EMERGENCY STOP: Stop all motors attached to controller
    /// </summary>
    /// <returns></returns> returns 0 on success, -1 on failure
    public async Task StopMotors()
    {
        if (_motor1 != null)
        {
            await _motor1.StopMotor();
        }

        if (_motor2 != null)
        {
            await _motor2.StopMotor();
        }
    }

    #endregion

    #region Status Methods

    /// <summary>
    /// Get the status of a motor on the requested axis
    /// </summary>
    /// <param name="axis"></param>
    public MotorStatus GetMotorStatus(int axis)
    {
        switch(axis)
        {
            case 1:
                return _motor1.GetStatus();
            case 2:
                return _motor2.GetStatus();
            default:
                MagnetoLogger.Log("Invalid motor axis.",
                LogFactoryLogLevel.LogLevel.ERROR);
                return MotorStatus.Error;
        }
    }

    #endregion
}
