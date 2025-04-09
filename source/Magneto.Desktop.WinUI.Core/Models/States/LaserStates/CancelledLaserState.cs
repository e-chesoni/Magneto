using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Magneto.Desktop.WinUI.Core.Contracts.Services.States;

namespace Magneto.Desktop.WinUI.Core.Models.State.LaserStates;
public class CancelledLaserState : ILaserControllerState
{
    public void Cancel() => throw new NotImplementedException();
    public void Draw() => throw new NotImplementedException();
    public void Pause() => throw new NotImplementedException();
}
