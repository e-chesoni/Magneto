using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Magneto.Desktop.WinUI.Core.Contracts.Services.StateMachineServices;
public interface IMotorControllerState
{
    void Move();

    void Wait();

    void Done();

    void Cancel();

}
