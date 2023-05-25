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
    //ublic List<StepperMotor> _motors = new List<StepperMotor>();

    private StepperMotor _motor1 { get; set; }
    private StepperMotor _motor2 { get; set; }
    
    public MotorController(StepperMotor motor1, StepperMotor motor2)
    {
        _motor1 = motor1;
        _motor2 = motor2;
    }

    /// <summary>
    /// Perform sequenced motor movement
    /// Syntax to move both motors to absolute position: 0MVA5.5
    /// </summary>
    /// <returns></returns> returns 0 on success, -1 on failure
    public void MoveMotors(double motor1Pos, double motor2Pos)
    {
        MagnetoLogger.Log("MotorController::MoveMotors -- Moving Motors...",
            Contracts.Services.LogFactoryLogLevel.LogLevel.VERBOSE);
        _motor1.MoveMotorAbs(motor1Pos);
        _motor2.MoveMotorAbs(motor2Pos);
    }

    /// <summary>
    /// Home all attached motors
    /// (Return all attached motors to zero position)
    /// </summary>
    public void HomeMotors()
    {
        MagnetoLogger.Log("MotorController::HomeMotors -- Homing motors (PLURAL!)...",
            Contracts.Services.LogFactoryLogLevel.LogLevel.VERBOSE);

        _motor1.HomeMotor();
        _motor2.HomeMotor();
    }

    /// <summary>
    /// EMERGENCY STOP: Stop all motors attached to controller
    /// </summary>
    /// <returns></returns> returns 0 on success, -1 on failure
    public int StopMotors()
    {
        _motor1.StopMotor();
        _motor2.StopMotor();
        return 0;
    }
}
