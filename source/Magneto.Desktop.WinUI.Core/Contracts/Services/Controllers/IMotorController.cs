using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Magneto.Desktop.WinUI.Core.Models.Motors;

namespace Magneto.Desktop.WinUI.Core.Contracts.Services.Controllers;

/// <summary>
/// Interface for wrapper used to synchronized motors attached to the same controller
/// </summary>
public interface IMotorController : IController
{
    // TODO: You can probably find a clever way to simplify this using unpacking, kwargs, or something...

    public List<StepperMotor> GetMinions();

    /// <summary>
    /// Perform asynchronous motor movement
    /// </summary>
    /// <param name="thickness"></param> Layer thickness for print
    /// <returns></returns>
    Task MoveMotorsAbsAsync(double thickness);

    /// <summary>
    /// Move one motor to an absolute position
    /// </summary>
    /// <param name="axis"></param> The axis of the motor to move
    /// <param name="step"></param> Distance to move motor
    /// <returns></returns>
    Task MoveMotorByAxisAsync(int axis, double step);

    /// <summary>
    /// Move motor to absolute position
    /// </summary>
    /// <param name="thickness"></param>
    /// <returns></returns>
    Task MoveMotorAbsAsync(StepperMotor motor, double step);

    /// <summary>
    /// Move all motors attached to controller relative to their current position
    /// Syntax to move both motors to relative position
    /// </summary>
    /// <returns></returns> returns 0 on success, -1 on failure
    Task MoveMotorsRelAsync(double thickness);

    /// <summary>
    /// Move motor relative to its current position
    /// </summary>
    /// <param name="axis"></param> The axis of the motor to move
    /// <param name="step"></param> Distance to move motor
    /// <returns></returns>
    Task MoveMotorRelAsync(int axis, double step);

    /// <summary>
    /// Move motor relative to its current position 
    /// </summary>
    /// <param name="motor"></param>
    /// <param name="step"></param>
    /// <returns></returns>
    Task MoveMotorRelAsync(StepperMotor motor, double step);

    /// <summary>
    /// Home all attached motors
    /// (Return all attached motors to zero position)
    /// </summary>
    Task HomeMotors();

    /// <summary>
    /// EMERGENCY STOP: Stop all motors attached to controller
    /// </summary>
    /// <returns></returns> returns 0 on success, -1 on failure
    void StopMotors();

}
