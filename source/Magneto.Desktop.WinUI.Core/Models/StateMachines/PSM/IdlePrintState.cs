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
    private PrintStateMachine _stateMachine;
    public IdlePrintState(PrintStateMachine psm)
    {
        _stateMachine = psm;
    }

    public void Play() => throw new NotImplementedException();
    public void Pause() => throw new NotImplementedException();
    public void Redo() => throw new NotImplementedException();
    public void Cancel() => throw new NotImplementedException();

}
