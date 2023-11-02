using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Magneto.Desktop.WinUI.Core.Contracts.Services;
using Magneto.Desktop.WinUI.Core.Models;


namespace Magneto.Desktop.WinUI.Core;

public class MagnetoMotor
{
    public string COMPort { get; set; }

    public int axis { get; set; }

    public double maxPos { get; set; }

    public double minPos { get; set; }

    public double homePos { get; set; }
}

public class MagnetoConfig : IMagnetoConfig
{
    // TODO: Add getter methods to return motor info
    // TODO: Add IEnum for com ports
    // TODO: Change "COMPort" on motor to be a comport from enum
    private List<MagnetoMotor> _allMotors;

    private IEnumerable<MagnetoMotor> AllMotors()
    {
        return new List<MagnetoMotor>()
        {
            new MagnetoMotor()
            {
                COMPort = "COM4",
                axis = 1,
                maxPos = 35,
                minPos = 0,
                homePos = 0,
            },
            new MagnetoMotor()
            {
                COMPort = "COM4",
                axis = 2,
                maxPos = 35,
                minPos = 0,
                homePos = 0,
            },
            new MagnetoMotor()
            {
                COMPort = "COM7",
                axis = 1,
                maxPos = 50,
                minPos = -50,
                homePos = 0,
            },
        };
    }
    public MagnetoMotor GetFirstMotor()
    {
        return AllMotors().FirstOrDefault();
    }

    async Task<IEnumerable<MagnetoMotor>> IMagnetoConfig.GetMotorDataAsync()
    {
        if (_allMotors == null)
        {
            _allMotors = new List<MagnetoMotor>(AllMotors());
        }

        await Task.CompletedTask;
        return _allMotors;
    }

    public MagnetoConfig()
    {

    }
}
