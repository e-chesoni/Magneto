using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Magneto.Desktop.WinUI.Core.Models.Print;
using Magneto.Desktop.WinUI.Core.Models.Artifact;
using Magneto.Desktop.WinUI.Core.Services;
using Magneto.Desktop.WinUI.Core.Contracts.Services.States;

namespace Magneto.Desktop.WinUI.Core.Models.State.PrintStates;
public class PausedBuildState : IPrintState
{
    private ActuationManager _BuildManagerSM { get; set; }

    public ArtifactModel ImageModel { get; set; }

    public PausedBuildState(ActuationManager bm)
    {
        var msg = "Entered PausedBuildState...";
        MagnetoLogger.Log(msg, Contracts.Services.LogFactoryLogLevel.LogLevel.VERBOSE);
        _BuildManagerSM = bm;
    }

    public void Cancel()
    {
        _BuildManagerSM.TransitionTo(new CancelledBuildState(_BuildManagerSM));
    }

    public void Pause()
    {
        MagnetoLogger.Log("Already Paused; Stay here.", Contracts.Services.LogFactoryLogLevel.LogLevel.VERBOSE);
    }

    public void Start(ArtifactModel im)
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
        _BuildManagerSM.TransitionTo(new PrintingBuildState(_BuildManagerSM));
    }

    public void Done() => throw new NotImplementedException();
    public async Task Homing() => throw new NotImplementedException();
}
