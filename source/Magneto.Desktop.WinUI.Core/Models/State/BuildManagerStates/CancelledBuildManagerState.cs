using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Magneto.Desktop.WinUI.Core.Contracts.Services.StateMachineServices;
using Magneto.Desktop.WinUI.Core.Models.BuildModels;
using Magneto.Desktop.WinUI.Core.Services;

namespace Magneto.Desktop.WinUI.Core.Models.State.BuildManagerStates;

/// <summary>
/// Pass-through state; you should not be able to call anything in this state
/// This class exist to process the cancel request
/// </summary>
public class CancelledBuildManagerState : IBuildManagerState
{
    private BuildManager BuildManagerSM { get; set; }

    public CancelledBuildManagerState(BuildManager bm)
    {
        MagnetoLogger.Log("CancelledBuildManagerState", Contracts.Services.LogFactoryLogLevel.LogLevel.VERBOSE);
        BuildManagerSM = bm;

        // TODO: Process cancelled build
        MagnetoLogger.Log("Handling cancelled build...", Contracts.Services.LogFactoryLogLevel.LogLevel.VERBOSE);

        BuildManagerSM.TransitionTo(new IdleBuildManagerState(BuildManagerSM));
    }

    public void Cancel() => throw new NotImplementedException();

    public void Pause() => throw new NotImplementedException();

    public void Start() => throw new NotImplementedException();

    public void Draw() => throw new NotImplementedException();

    public void Resume() => throw new NotImplementedException();
}
