using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Magneto.Desktop.WinUI.Core.Contracts.Services;
using Magneto.Desktop.WinUI.Core.Models.Controllers;
using Magneto.Desktop.WinUI.Core.Models.Motors;

namespace Magneto.Desktop.WinUI.Core.Factories;
public static class MotorFactory
{
    public static MotorController CreateBuildController()
    {
        var powder = CreateMotor("powder");
        var build = CreateMotor("build");

        return new MotorController(powder, build);
    }

    public static MotorController CreateSweepController()
    {
        var sweep = CreateMotor("sweep");
        return new MotorController(sweep);
    }

    public static StepperMotor CreateMotor(string name)
    {
        var config = MagnetoConfig.GetMotorByName(name);
        if (config == null)
        {
            var msg = $"❌ Could not find config for {name} motor.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
            throw new Exception(msg);
        }

        return new StepperMotor(
            config.motorName, config.COMPort, config.axis,
            config.maxPos, config.minPos, config.homePos, config.velocity
        );
    }
}

