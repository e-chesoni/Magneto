using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Magneto.Desktop.WinUI.Core.Contracts.Services.State;

namespace Magneto.Desktop.WinUI.Core.Models.State.MotorControlerStates;
public class CancelledMotorControllerState : IMotorControllerState
{
    public void Cancel() => throw new NotImplementedException();
    public void Done() => throw new NotImplementedException();
    public void Move() => throw new NotImplementedException();
    public void Wait() => throw new NotImplementedException();
}
