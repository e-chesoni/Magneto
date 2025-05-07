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
    private PrintStateMachine _stateMachine;

    public PausedPrintState(PrintStateMachine psm)
    {
        _stateMachine = psm;
    }

    public void Play() => throw new NotImplementedException();
    public void Pause() => throw new NotImplementedException();
    public void Redo() => throw new NotImplementedException();
    public void Cancel() => throw new NotImplementedException();
    public void ChangeStateTo(IPrintState state) => _stateMachine.ChangeStateTo(state);
}
