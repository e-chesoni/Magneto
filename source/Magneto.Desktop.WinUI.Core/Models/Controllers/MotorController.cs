using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Magneto.Desktop.WinUI.Core.Contracts.Services.Controllers;
using Magneto.Desktop.WinUI.Core.Models.Motor;
using Magneto.Desktop.WinUI.Core.Services;

namespace Magneto.Desktop.WinUI.Core.Models.Controllers;

/// <summary>
/// Class used to synchronized motors attached to the same controller
/// </summary>
public class MotorController : IMotorController
{
    private StepperMotor _motor1 { get; set; }
    private StepperMotor _motor2 { get; set; }

    public MotorController(StepperMotor motor1)
    {
        _motor1 = motor1;
    }

    public MotorController(StepperMotor motor1, StepperMotor motor2)
    {
        _motor1 = motor1;
        _motor2 = motor2;
    }

    /// <summary>
    /// Perform sequenced motor movement
    /// Syntax to move both motors to absolute position
    /// </summary>
    /// <returns></returns> returns 0 on success, -1 on failure
    public async Task MoveMotorsAbs(double thickness)
    {
        MagnetoLogger.Log("MotorController::MoveMotorsAbs -- Moving Motors (PLURAL)...",
            Contracts.Services.LogFactoryLogLevel.LogLevel.VERBOSE);

        // TODO: Thread blocking is not a great idea...
        // Find a more elegant way to handle running one motor at a time in the future
        if (_motor1 != null)
        {
            await _motor1.MoveMotorAbs(thickness);
            Thread.Sleep(2000);
        }

        if (_motor2 != null)
        {
            await _motor2.MoveMotorAbs(thickness);
            Thread.Sleep(2000);
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
}
