using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Magneto.Desktop.WinUI.Core.Contracts.Services.Controllers;
using Magneto.Desktop.WinUI.Core.Models.Motor;

namespace Magneto.Desktop.WinUI.Core.Models.Controllers;
public class MotorController : IMotorController
{
    public List<StepperMotor> _motors;

    public int AttachMotor(int axis) => throw new NotImplementedException();

    public int DetachMotor(int axis) => throw new NotImplementedException();

    public int MoveMotors()
    {
        throw new NotImplementedException();
    }

    public int StopMotors()
    {
        throw new NotImplementedException();
    }
}
