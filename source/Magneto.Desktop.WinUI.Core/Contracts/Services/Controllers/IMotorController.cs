using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Magneto.Desktop.WinUI.Core.Models.Motor;

namespace Magneto.Desktop.WinUI.Core.Contracts.Services.Controllers;

/// <summary>
/// Interface for wrapper used to synchronized motors attached to the same controller
/// </summary>
public interface IMotorController
{
    // TODO: You can probably find a clever way to simplify this using unpacking, kwargs, or something...

    /// <summary>
    /// Perform asynchronous motor movement
    /// </summary>
    /// <param name="thickness"></param> Layer thickness for print
    /// <returns></returns>
    Task MoveMotorsAbsAsync(double thickness);

    /// <summary>
    /// Move one motor relative to an absolute position
    /// </summary>
    /// <param name="axis"></param> The axis of the motor to move
    /// <param name="step"></param> Distance to move motor
    /// <returns></returns>
    Task MoveMotorAbsAsync(int axis, double step);

    /// <summary>
    /// Move motor synchronously during prints
    /// </summary>
    /// <param name="thickness"></param>
    /// <returns></returns>
    Task MoveMotorAbs(StepperMotor motor, double step);

    /// <summary>
    /// Perform sequenced motor movement
    /// Syntax to move both motors to relative position
    /// </summary>
    /// <returns></returns> returns 0 on success, -1 on failure
    Task MoveMotorsRel(double thickness);

    /// <summary>
    /// Move one motor relative to its current position
    /// </summary>
    /// <param name="axis"></param> The axis of the motor to move
    /// <param name="step"></param> Distance to move motor
    /// <returns></returns>
    Task MoveMotorRel(int axis, double step);

    /// <summary>
    /// Home all attached motors
    /// (Return all attached motors to zero position)
    /// </summary>
    Task HomeMotors();

    /// <summary>
    /// EMERGENCY STOP: Stop all motors attached to controller
    /// </summary>
    /// <returns></returns> returns 0 on success, -1 on failure
    Task StopMotors();

}
