using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Magneto.Desktop.WinUI.Core.Models.BuildModels;
using Magneto.Desktop.WinUI.Core.Models.Artifact;
using Magneto.Desktop.WinUI.Core.Services;
using Magneto.Desktop.WinUI.Core.Contracts.Services.States;

namespace Magneto.Desktop.WinUI.Core.Models.State.PrintStates;

/// <summary>
/// Pass-through state; you should not be able to call anything in this state
/// This class exist to process the cancel request
/// </summary>
public class CancelledBuildState : IPrintState
{
    private ActuationManager _BuildManagerSM { get; set; }

    public CancelledBuildState(ActuationManager bm)
    {
        var msg = "Entered CancelledBuildState...";
        MagnetoLogger.Log(msg, Contracts.Services.LogFactoryLogLevel.LogLevel.VERBOSE);

        _BuildManagerSM = bm;
        // TODO: Stop build
        // Blocked by implementation of cancel motor move tasks in ActuationManager

        // TODO: Process cancelled build
        // This is the British spelling. Get over it spell checker.
        MagnetoLogger.Log("Handling cancelled build...", Contracts.Services.LogFactoryLogLevel.LogLevel.VERBOSE);

        // Home motors
        _ = _BuildManagerSM.buildController.HomeMotors();

        _BuildManagerSM.TransitionTo(new IdleBuildState(_BuildManagerSM));
    }

    public void Cancel() => throw new NotImplementedException();

    public void Pause() => throw new NotImplementedException();

    public void Start(ArtifactModel im) => throw new NotImplementedException();

    public Task Draw() => throw new NotImplementedException();

    public void Resume() => throw new NotImplementedException();
    public void Done() => throw new NotImplementedException();
    public async Task Homing() => throw new NotImplementedException();
}
