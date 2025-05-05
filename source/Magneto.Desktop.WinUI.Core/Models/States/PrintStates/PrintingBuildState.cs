using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Magneto.Desktop.WinUI.Core.Models.Print;
using Magneto.Desktop.WinUI.Core.Models.Controllers;
using Magneto.Desktop.WinUI.Core.Models.Artifact;
using Magneto.Desktop.WinUI.Core.Models.Motors;
using Magneto.Desktop.WinUI.Core.Contracts.Services.States;


namespace Magneto.Desktop.WinUI.Core.Models.State.PrintStates;
public class PrintingBuildState : IPrintState
{
    private ProgramsManager _BuildManagerSM { get; set; }

    public PrintingBuildState(ProgramsManager bm)
    {
        var msg = "Entered PrintingBuildState...";
        MagnetoLogger.Log(msg, Contracts.Services.LogFactoryLogLevel.LogLevel.VERBOSE);

        // Set state build manger to build manager passed in on initialization
        _BuildManagerSM = bm;

        // Set the build flag to resume
        //_BuildManagerSM.build_flag = ProgramsManager.BuildFlag.RESUME;

        // Start drawing
        _ = Draw();
    }

    public void Cancel()
    {
        MagnetoLogger.Log("Cancel flag triggered!", Contracts.Services.LogFactoryLogLevel.LogLevel.WARN);
        //_BuildManagerSM.build_flag = ProgramsManager.BuildFlag.CANCEL;
        //_BuildManagerSM.TransitionTo(new CancelledBuildState(_BuildManagerSM));
    }

    public void Pause()
    {
        MagnetoLogger.Log("Pause flag triggered!", Contracts.Services.LogFactoryLogLevel.LogLevel.WARN);
        //_BuildManagerSM.build_flag = ProgramsManager.BuildFlag.PAUSE;
        //_BuildManagerSM.TransitionTo(new PausedBuildState(_BuildManagerSM));
    }

    public void Start(ArtifactModel im) => throw new NotImplementedException();

    public async Task Draw()
    {
        throw new NotImplementedException();
        //_BuildManagerSM.TransitionTo(new DoneBuildState(_BuildManagerSM));
    }

    public void Resume() => throw new NotImplementedException();
    
    public void Done() => throw new NotImplementedException();

    public async Task Homing()
    {
        throw new NotImplementedException();

        // Return to idle state
        //_BuildManagerSM.TransitionTo(new IdleBuildState(_BuildManagerSM));
    }
}
