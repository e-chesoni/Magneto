using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using Magneto.Desktop.WinUI.Core.Contracts.Services.StateMachineServices;
using Magneto.Desktop.WinUI.Core.Models.Image;
using Magneto.Desktop.WinUI.Core.Services;

namespace Magneto.Desktop.WinUI.Core.Models.State.BuildManagerStates;
public class IdleBuildManagerState : IBuildManagerState
{
    private bool CANCEL_FLAG_SET = false;

    private BuildManager BuildManagerSM;
    public void SetStateMachine(BuildManager bm)
    {
        BuildManagerSM = bm;
    }
    public void Cancel() => throw new NotImplementedException();
    public void Pause() => throw new NotImplementedException();
    public void Start() {}

    public void Start(ImageModel im)
    {
        MagnetoLogger.Log("Starting...", Contracts.Services.LogFactoryLogLevel.LogLevel.VERBOSE);

        BuildManagerSM.TransitionTo(new PrintingBuildManagerState(im));
    }

    public void Draw()
    {
        MagnetoLogger.Log("Drawing should not be called from this state!", Contracts.Services.LogFactoryLogLevel.LogLevel.WARN);
    }
}
