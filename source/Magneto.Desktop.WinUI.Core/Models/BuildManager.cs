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

namespace Magneto.Desktop.WinUI.Core.Models;

/// <summary>
/// Coordinates printing tasks across components
/// </summary>
public class BuildManager
{
    private MotorController _buildController;
    private MotorController _sweepController;
    private LaserController _laserController;

    public BuildManager(MotorController buildController, MotorController sweepController, LaserController laserController)
    {
        _buildController = buildController;
        _sweepController = sweepController;
        _laserController = laserController;
    }


}
