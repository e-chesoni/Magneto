using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using Magneto.Desktop.WinUI.Core.Models.Print;
using Magneto.Desktop.WinUI.Core.Models.Artifact;
using Magneto.Desktop.WinUI.Core.Contracts.Services.States;
using Magneto.Desktop.WinUI.Core.Models.States.PrintStates;

namespace Magneto.Desktop.WinUI.Core.Models.State.PrintStates;
public class IdlePrintState : IPrintState
{
    private PrintStateMachine _psm;
    public IdlePrintState(PrintStateMachine psm)
    {
        _psm = psm;
        _psm.status = PrintStateMachine.PrintStateMachineStatus.Idle;
    }
    public async Task<bool> InitializePlayAsync(int numberOfLayers = 1)
    {
        var newState = new PrintingPrintState(_psm);
        _psm.ChangeStateTo(newState);
        return await newState.InitializePlayAsync(); // run Play() logic immediately
    }
    public async Task<bool> Play(int numberOfLayers = 1)
    {
        return await InitializePlayAsync();
    }
    public void Pause() => ChangeStateTo(new PausedPrintState(_psm));
    public async Task<bool> Resume()
    {
        var newState = new PrintingPrintState(_psm);
        _psm.ChangeStateTo(newState);
        return await newState.Resume(); // run resume
    }
    public void Redo() => throw new NotImplementedException();
    public void Cancel() => throw new NotImplementedException();
    public void ChangeStateTo(IPrintState state) => _psm.ChangeStateTo(state);
}
