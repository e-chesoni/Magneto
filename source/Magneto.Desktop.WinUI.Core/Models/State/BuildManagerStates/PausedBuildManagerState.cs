using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Magneto.Desktop.WinUI.Core.Contracts.Services.StateMachineServices;
using Magneto.Desktop.WinUI.Core.Models.BuildModels;
using Magneto.Desktop.WinUI.Core.Models.Image;
using Magneto.Desktop.WinUI.Core.Services;

namespace Magneto.Desktop.WinUI.Core.Models.State.BuildManagerStates;
public class PausedBuildManagerState : IBuildManagerState
{
    private BuildManager _BuildManagerSM { get; set; }

    public ImageModel ImageModel { get; set; }

    public PausedBuildManagerState(BuildManager bm)
    {
        MagnetoLogger.Log("PausedBuildManagerState::PausedBuildManagerState", 
            Contracts.Services.LogFactoryLogLevel.LogLevel.VERBOSE);
        _BuildManagerSM = bm;
    }

    public void Cancel()
    {
        _BuildManagerSM.TransitionTo(new CancelledBuildManagerState(_BuildManagerSM));
    }

    public void Pause()
    {
        MagnetoLogger.Log("Already Paused; Stay here.", Contracts.Services.LogFactoryLogLevel.LogLevel.VERBOSE);
    }

    public void Start(ImageModel im)
    {
        MagnetoLogger.Log("Can't start paused print; try resuming!", Contracts.Services.LogFactoryLogLevel.LogLevel.WARN);
    }

    public Task Draw()
    {
        MagnetoLogger.Log("Print is paused; try resuming to draw!...", Contracts.Services.LogFactoryLogLevel.LogLevel.VERBOSE);
        return Task.CompletedTask;
    }

    public void Resume()
    {
        _BuildManagerSM.TransitionTo(new PrintingBuildManagerState(_BuildManagerSM));
    }

    public void Done() => throw new NotImplementedException();
}
