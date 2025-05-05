using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using Magneto.Desktop.WinUI.Core.Models.Print;
using Magneto.Desktop.WinUI.Core.Models.Artifact;
using Magneto.Desktop.WinUI.Core.Contracts.Services.States;

namespace Magneto.Desktop.WinUI.Core.Models.State.PrintStates;
public class IdleBuildState : IPrintState
{
    private ProgramsManager _BuildManagerSM;

    public IdleBuildState(ProgramsManager bm)
    {
        var msg = "Entered IdleBuildState...";
        MagnetoLogger.Log(msg, Contracts.Services.LogFactoryLogLevel.LogLevel.VERBOSE);
        _BuildManagerSM = bm;
    }

    public void Cancel()
    {
        MagnetoLogger.Log("IdleBuildManagerState::Cancel -- Cannot cancel haven't started!",
            Contracts.Services.LogFactoryLogLevel.LogLevel.WARN);
    }
    public void Pause()
    {
        MagnetoLogger.Log("IdleBuildManagerState::Pause -- Cannot pause haven't started!",
            Contracts.Services.LogFactoryLogLevel.LogLevel.WARN);
    }
    public void Start() => throw new NotImplementedException();

    public void Start(ArtifactModel am)
    {
        MagnetoLogger.Log("Starting...", Contracts.Services.LogFactoryLogLevel.LogLevel.VERBOSE);

        // Get poses for print (each "pose" has a slice (shape to trace) and a thickness)
        _BuildManagerSM.danceModel.GenerateDance(am.sliceStack, MagnetoConfig.GetDefaultPrintThickness());

        // TODO: Get max height from dance model

        // TODO: Move Motor1 to max height; motor 1 steps down for each iteration (building surface)

        // TODO: Home Motor2; motor 2 steps up for each iteration (powder extruder)

        // TODO: Add artificial wait for user to enter powder

        // TODO: Add WaitForPowderState

        // TODO: Add PowderLoadedState

        // TODO: Move transition to build state to execute in PowderLoadedState
        // Transition to print state
        //_BuildManagerSM.TransitionTo(new PrintingBuildState(_BuildManagerSM));
    }

    public Task Draw()
    {
        MagnetoLogger.Log("IdleBuildManagerState::Draw --Drawing should not be called from this state!",
            Contracts.Services.LogFactoryLogLevel.LogLevel.WARN);
        return Task.CompletedTask;
    }

    public void Resume()
    {
        MagnetoLogger.Log("IdleBuildManagerState::Resume -- Cannot resume what we haven't started!",
            Contracts.Services.LogFactoryLogLevel.LogLevel.WARN);
    }
    public void Done()
    {
        MagnetoLogger.Log("IdleBuildManagerState::Done -- Cannot finish haven't started!",
            Contracts.Services.LogFactoryLogLevel.LogLevel.WARN);
    }

    public async Task Homing()
    {
        throw new NotImplementedException();

        // Return to idle state
        //_BuildManagerSM.TransitionTo(new IdleBuildState(_BuildManagerSM));
    }
}
