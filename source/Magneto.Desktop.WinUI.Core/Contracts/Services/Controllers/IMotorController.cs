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
    /// Perform sequenced motor movement
    /// Syntax to move both motors to absolute position
    /// </summary>
    /// <returns></returns> returns 0 on success, -1 on failure
    Task MoveMotorsAbs(double motor1Pos, double motor2Pos);

    Task MoveMotorAbs(double motorPos);

    /// <summary>
    /// Perform sequenced motor movement
    /// Syntax to move both motors to relative position
    /// </summary>
    /// <returns></returns> returns 0 on success, -1 on failure
    Task MoveMotorsRel(double motor1Pos, double motor2Pos);

    Task MoveMotorRel(double motorPos);

    /// <summary>
    /// Home all attached motors
    /// (Return all attached motors to zero position)
    /// </summary>
    Task HomeMotors();

    Task HomeMotor();

    /// <summary>
    /// EMERGENCY STOP: Stop all motors attached to controller
    /// </summary>
    /// <returns></returns> returns 0 on success, -1 on failure
    Task StopMotors();

}
