﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Magneto.Desktop.WinUI.Core.Models.Print;
using Magneto.Desktop.WinUI.Core.Models.Artifact;
using Magneto.Desktop.WinUI.Core.Contracts.Services.States;

namespace Magneto.Desktop.WinUI.Core.Models.State.PrintStates;

/// <summary>
/// Processing state; user should not be able to invoke any functionality from this state
/// </summary>
public class DoneBuildState : IPrintState
{
    private CommandQueueManager _BuildManagerSM { get; set; }

    public DoneBuildState(CommandQueueManager _bm)
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
        _ = _BuildManagerSM.AddCommand(CommandQueueManager.ControllerType.BUILD, powder_axis, CommandQueueManager.CommandType.AbsoluteMove, 0);
        _ = _BuildManagerSM.AddCommand(CommandQueueManager.ControllerType.BUILD, build_axis, CommandQueueManager.CommandType.AbsoluteMove, 0);

        // Return to idle state
        _BuildManagerSM.TransitionTo(new IdleBuildState(_BuildManagerSM));
    }
}
