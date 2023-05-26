using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using Magneto.Desktop.WinUI.Core.Contracts.Services.StateMachineServices;
using Magneto.Desktop.WinUI.Core.Models.BuildModels;
using Magneto.Desktop.WinUI.Core.Models.Image;
using Magneto.Desktop.WinUI.Core.Services;

namespace Magneto.Desktop.WinUI.Core.Models.State.BuildManagerStates;
public class IdleBuildManagerState : IBuildManagerState
{
    private BuildManager _BuildManagerSM;

    public IdleBuildManagerState(BuildManager bm)
    {
        MagnetoLogger.Log("IdleBuildManagerState::IdleBuildManagerState", 
            Contracts.Services.LogFactoryLogLevel.LogLevel.VERBOSE);
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

    public void Start(ImageModel im)
    {
        MagnetoLogger.Log("Starting...", Contracts.Services.LogFactoryLogLevel.LogLevel.VERBOSE);

        // Get poses for print
        _BuildManagerSM.danceModel.GetPoseStack(im.sliceStack);

        // Transition to print state
        _BuildManagerSM.TransitionTo(new PrintingBuildManagerState(_BuildManagerSM));
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

    public void Homing()
    {
        // Home motors
        _BuildManagerSM.buildController.HomeMotors();

        // Return to idle state
        _BuildManagerSM.TransitionTo(new IdleBuildManagerState(_BuildManagerSM));
    }
}
