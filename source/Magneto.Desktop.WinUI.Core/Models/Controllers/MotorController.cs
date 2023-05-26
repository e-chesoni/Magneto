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
    public async Task MoveMotorsAbs(double motor1Pos, double motor2Pos)
    {
        MagnetoLogger.Log("MotorController::MoveMotorsAbs -- Moving Motors (PLURAL)...",
            Contracts.Services.LogFactoryLogLevel.LogLevel.VERBOSE);
        await _motor1.MoveMotorAbs(motor1Pos);
        await _motor2.MoveMotorAbs(motor2Pos);
    }

    public async Task MoveMotorAbs(double motorPos)
    {
        MagnetoLogger.Log("MotorController::MoveMotorAbs -- Moving Motors...",
            Contracts.Services.LogFactoryLogLevel.LogLevel.VERBOSE);
        await _motor1.MoveMotorAbs(motorPos);
    }

    /// <summary>
    /// Perform sequenced motor movement
    /// Syntax to move both motors to relative position
    /// </summary>
    /// <returns></returns> returns 0 on success, -1 on failure
    public async Task MoveMotorsRel(double motor1Pos, double motor2Pos)
    {
        MagnetoLogger.Log("MotorController::MoveMotorsAbs -- Moving Motors (PLURAL)...",
            Contracts.Services.LogFactoryLogLevel.LogLevel.VERBOSE);

        // TODO: Thread blocking is not a great idea...
        // Find a more elegant way to handle running one motor at a time in the future
        await _motor1.MoveMotorRel(motor1Pos);
        Thread.Sleep(2000);
        await _motor2.MoveMotorRel(motor2Pos);
        Thread.Sleep(2000);
    }

    public async Task MoveMotorRel(int axis, double motorPos)
    {
        MagnetoLogger.Log("MotorController::MoveMotorAbs -- Moving Motors...",
            Contracts.Services.LogFactoryLogLevel.LogLevel.VERBOSE);

        switch (axis)
        {
            case 1:
                await _motor1.MoveMotorRel(motorPos);
                break; 
            case 2:
                await _motor2.MoveMotorRel(motorPos);
                break;
            default: 
                // TODO: Handle gracefully...
                throw new ArgumentOutOfRangeException();
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

    public async Task HomeMotor()
    {
        MagnetoLogger.Log("MotorController::HomeMotors -- Homing motor...",
            Contracts.Services.LogFactoryLogLevel.LogLevel.VERBOSE);

        await _motor1.HomeMotor();
    }

    /// <summary>
    /// EMERGENCY STOP: Stop all motors attached to controller
    /// </summary>
    /// <returns></returns> returns 0 on success, -1 on failure
    public async Task StopMotors()
    {
        await _motor1.StopMotor();
        await _motor2.StopMotor();
    }
}
