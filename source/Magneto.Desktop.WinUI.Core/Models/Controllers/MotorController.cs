using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Magneto.Desktop.WinUI.Core.Contracts.Services.Controllers;
using Magneto.Desktop.WinUI.Core.Models.Motor;

namespace Magneto.Desktop.WinUI.Core.Models.Controllers;

/// <summary>
/// Class used to synchronized motors attached to the same controller
/// </summary>
public class MotorController : IMotorController
{
    public List<StepperMotor> _motors;

    /// <summary>
    /// Adds motor to controller's list of motors
    /// </summary>
    /// <param name="axis"></param> Motors are uniquely attached to axis 1, 2, or 3
    /// <returns></returns> returns 0 on success, -1 on failure
    public int AttachMotor(int axis) => throw new NotImplementedException();

    /// <summary>
    /// Removes motor from controller's list of motors
    /// </summary>
    /// <param name="axis"></param> Motors are uniquely attached to axis 1, 2, or 3
    /// <returns></returns> returns 0 on success, -1 on failure
    public int DetachMotor(int axis) => throw new NotImplementedException();

    /// <summary>
    /// Perform sequenced motor movement
    /// Syntax to move both motors to absolute position: 0MVA5.5
    /// </summary>
    /// <returns></returns> returns 0 on success, -1 on failure
    public void MoveMotors(double motor1Pos, double motor2Pos)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// EMERGENCY STOP: Stop all motors attached to controller
    /// </summary>
    /// <returns></returns> returns 0 on success, -1 on failure
    public int StopMotors()
    {
        throw new NotImplementedException();
    }
}
