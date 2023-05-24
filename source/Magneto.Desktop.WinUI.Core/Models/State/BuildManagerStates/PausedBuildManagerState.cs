using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Magneto.Desktop.WinUI.Core.Contracts.Services.StateMachineServices;

namespace Magneto.Desktop.WinUI.Core.Models.State.BuildManagerStates;
public class PausedBuildManagerState : IBuildManagerState
{
    public void SetStateMachine(BuildManager bm) => throw new NotImplementedException();
    public void Cancel() => throw new NotImplementedException();
    public void Pause() => throw new NotImplementedException();
    public void Start() => throw new NotImplementedException();
    public void Draw() => throw new NotImplementedException();
}
