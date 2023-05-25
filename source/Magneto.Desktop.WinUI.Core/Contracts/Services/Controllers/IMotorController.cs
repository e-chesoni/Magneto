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
    /// <summary>
    /// Perform sequenced motor movement
    /// Syntax to move both motors to absolute position: 0MVA5.5
    /// </summary>
    /// <returns></returns> returns 0 on success, -1 on failure
    void MoveMotors(double motor1Pos, double motor2Pos);

    /// <summary>
    /// Home all attached motors
    /// (Return all attached motors to zero position)
    /// </summary>
    void HomeMotors();

    /// <summary>
    /// EMERGENCY STOP: Stop all motors attached to controller
    /// </summary>
    /// <returns></returns> returns 0 on success, -1 on failure
    int StopMotors();

}
