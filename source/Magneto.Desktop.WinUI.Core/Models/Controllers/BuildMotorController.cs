using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Magneto.Desktop.WinUI.Core.Models.Motors;

namespace Magneto.Desktop.WinUI.Core.Models.Controllers;
public class BuildMotorController : MotorController
{
    public BuildMotorController(StepperMotor powder, StepperMotor build)
        : base(powder, build) { }
}