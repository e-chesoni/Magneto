using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Magneto.Desktop.WinUI.Core.Contracts.Services.Controllers;

/// <summary>
/// Wrapper for synchronized motor control
/// </summary>
public interface IMotorController
{
    /// <summary>
    /// Adds motor to controller's list of motors
    /// </summary>
    /// <param name="axis"></param> Motors are uniquely attached to axis 1, 2, or 3
    /// <returns></returns> returns 0 on success, -1 on failure
    int AttachMotor(int axis);

    /// <summary>
    /// Removes motor from controller's list of motors
    /// </summary>
    /// <param name="axis"></param> Motors are uniquely attached to axis 1, 2, or 3
    /// <returns></returns> returns 0 on success, -1 on failure
    int DetachMotor(int axis);

    /// <summary>
    /// Perform sequenced motor movement
    /// Syntax to move both motors to absolute position: 0MVA5.5
    /// </summary>
    /// <returns></returns> returns 0 on success, -1 on failure
    int MoveMotors();

    /// <summary>
    /// EMERGENCY STOP: Stop all motors attached to controller
    /// </summary>
    /// <returns></returns> returns 0 on success, -1 on failure
    int StopMotors();

}
