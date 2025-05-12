using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Magneto.Desktop.WinUI.Core.Models.Print;
using Magneto.Desktop.WinUI.Core.Models.Artifact;
using Magneto.Desktop.WinUI.Core.Contracts.Services.States;
using Magneto.Desktop.WinUI.Core.Models.States.PrintStates;
using Magneto.Desktop.WinUI.Core.Models.Print.Database;
using System.Collections.ObjectModel;
using Magneto.Desktop.WinUI.Core.Contracts.Services.Database.Seeders;
using Magneto.Desktop.WinUI.Core.Contracts.Services.Database;

namespace Magneto.Desktop.WinUI.Core.Models.State.PrintStates;
public class PausedPrintState : IPrintState
{
    private PrintStateMachine _psm;

    public PausedPrintState(PrintStateMachine psm)
    {
        _psm = psm;
        _psm.status = PrintStateMachine.PrintStateMachineStatus.Paused;
        _psm.rsm.status = RoutineStateMachine.RoutineStateMachineStatus.Paused;
    }
    public async Task<bool> InitializePlayAsync()
    {
        var newState = new PrintingPrintState(_psm);
        _psm.ChangeStateTo(newState);
        return await newState.InitializePlayAsync(); // run Play() logic immediately
    }

    public async Task<bool> InitializeResumeAsync()
    {
        var newState = new PrintingPrintState(_psm);
        _psm.ChangeStateTo(newState);
        return await newState.Resume(); // run Play() logic immediately
    }

    public async Task<bool> Play() => await InitializePlayAsync();
    public void Pause()
    {
        // do nothing; already in paused state

    }
    public async Task<bool> Resume() => await InitializeResumeAsync();
    public void Redo() => throw new NotImplementedException();
    public void Cancel() => throw new NotImplementedException();
    public void ChangeStateTo(IPrintState state) => _psm.ChangeStateTo(state);
}
