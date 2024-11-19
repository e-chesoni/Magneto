using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Magneto.Desktop.WinUI.Core.Contracts.Services.StateMachineServices;
using Magneto.Desktop.WinUI.Core.Models.BuildModels;
using Magneto.Desktop.WinUI.Core.Models.Artifact;
using Magneto.Desktop.WinUI.Core.Services;

namespace Magneto.Desktop.WinUI.Core.Models.State.BuildManagerStates;

/// <summary>
/// Processing state; user should not be able to invoke any functionality from this state
/// </summary>
public class DoneBuildState : IBuildManagerState
{
    private ActuationManager _BuildManagerSM { get; set; }

    public DoneBuildState(ActuationManager _bm)
    {
        var msg = "Entered DoneBuildState...";
        MagnetoLogger.Log(msg, Contracts.Services.LogFactoryLogLevel.LogLevel.VERBOSE);
        _BuildManagerSM = _bm;
    }

    public void Cancel() => throw new NotImplementedException();
    public void Done() => throw new NotImplementedException();
    public Task Draw() => throw new NotImplementedException();
    public void Pause() => throw new NotImplementedException();
    public void Resume() => throw new NotImplementedException();
    public void Start(ArtifactModel im) => throw new NotImplementedException();
    public async Task Homing()
    {
        // Home motors
        var powder_axis = _BuildManagerSM.buildController.GetPowderMotor().GetAxis();
        var build_axis = _BuildManagerSM.buildController.GetBuildMotor().GetAxis();

        // TODO: May want to change to await instead of _ (need to test)
        _ = _BuildManagerSM.AddCommand(ActuationManager.ControllerType.BUILD, powder_axis, ActuationManager.CommandType.AbsoluteMove, 0);
        _ = _BuildManagerSM.AddCommand(ActuationManager.ControllerType.BUILD, build_axis, ActuationManager.CommandType.AbsoluteMove, 0);

        // Return to idle state
        _BuildManagerSM.TransitionTo(new IdleBuildState(_BuildManagerSM));
    }
}
