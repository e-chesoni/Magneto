using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Magneto.Desktop.WinUI.Core.Contracts.Services.Controllers;
using Magneto.Desktop.WinUI.Core.Models.Controllers;
using Magneto.Desktop.WinUI.Core.Models.Image;
using Magneto.Desktop.WinUI.Core.Models.Monitor;
using Magneto.Desktop.WinUI.Core.Services;
using static Magneto.Desktop.WinUI.Core.Models.Motor.StepperMotor;

namespace Magneto.Desktop.WinUI.Core.Models;

/// <summary>
/// Coordinates printing tasks across components
/// </summary>
public class BuildManager
{
    #region Private Variables

    /// NOTE: Currently, Magneto has two motor controllers; 
    /// One for the build motors (on the base of the housing)
    /// And one for powder distribution (sweep) motor

    /// <summary>
    /// Controller for build motors
    /// </summary>
    private MotorController _buildController;

    /// <summary>
    /// Controller for sweep motor
    /// </summary>
    private MotorController _sweepController;

    /// <summary>
    /// Controller for laser and scan head
    /// </summary>
    private LaserController _laserController;

    #endregion

    #region Constructor

    /// <summary>
    /// 
    /// </summary>
    /// <param name="buildController"></param>
    /// <param name="sweepController"></param>
    /// <param name="laserController"></param>
    public BuildManager(MotorController buildController, MotorController sweepController, LaserController laserController)
    {
        _buildController = buildController;
        _sweepController = sweepController;
        _laserController = laserController;
    }

    #endregion

    public MotorStatus GetStatus()
    {
        throw new NotImplementedException();
    }

    public void Idle()
    {
        throw new NotImplementedException(); 
    }

    public void BuildShape()
    {
        throw new NotImplementedException(); 
    }

    public void Pause()
    {
        throw new NotImplementedException(); 
    }

    public void Cancel()
    {
        throw new NotImplementedException();
    }

}
