﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Magneto.Desktop.WinUI.Core.Contracts.Services.StateMachineServices;
using Magneto.Desktop.WinUI.Core.Models.BuildModels;
using Magneto.Desktop.WinUI.Core.Models.Image;
using Magneto.Desktop.WinUI.Core.Services;

namespace Magneto.Desktop.WinUI.Core.Models.State.BuildManagerStates;

/// <summary>
/// Processing state; user should not be able to invoke any functionality from this state
/// </summary>
public class DoneBuildManagerState : IBuildManagerState
{
    private BuildManager _BuildManagerSM { get; set; }

    public DoneBuildManagerState(BuildManager _bm)
    {
        MagnetoLogger.Log("DoneBuildManagerState::DoneBuildManagerState",
            Contracts.Services.LogFactoryLogLevel.LogLevel.VERBOSE);

        _BuildManagerSM = _bm;
    }

    public void Cancel() => throw new NotImplementedException();
    public void Done() => throw new NotImplementedException();
    public Task Draw() => throw new NotImplementedException();
    public void Pause() => throw new NotImplementedException();
    public void Resume() => throw new NotImplementedException();
    public void Start(ImageModel im) => throw new NotImplementedException();
    public async Task Homing()
    {
        // Home motors
        await _BuildManagerSM.buildController.HomeMotors();
        await _BuildManagerSM.sweepController.HomeMotors();

        // Return to idle state
        _BuildManagerSM.TransitionTo(new IdleBuildManagerState(_BuildManagerSM));
    }
}
