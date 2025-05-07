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
using Magneto.Desktop.WinUI.Core.Models.States.PrintStates;


namespace Magneto.Desktop.WinUI.Core.Models.State.PrintStates;
public class PrintingPrintState : IPrintState
{
    private PrintStateMachine _stateMachine;
    public PrintingPrintState(PrintStateMachine psm)
    {
        _stateMachine = psm;
    }

    public void Play() => throw new NotImplementedException();
    public void Pause() => throw new NotImplementedException();
    public void Redo() => throw new NotImplementedException();
    public void Cancel() => throw new NotImplementedException();
}
