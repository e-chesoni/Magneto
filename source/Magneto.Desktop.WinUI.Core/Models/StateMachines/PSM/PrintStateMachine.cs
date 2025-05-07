using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Magneto.Desktop.WinUI.Core.Contracts.Services.States;
using Magneto.Desktop.WinUI.Core.Models.Print;
using Magneto.Desktop.WinUI.Core.Models.State.PrintStates;

namespace Magneto.Desktop.WinUI.Core.Models.States.PrintStates;
public class PrintStateMachine
{
    private IPrintState _currentState;
    public ProgramsManager pg { get; set; }
    public PrintStateMachine(ProgramsManager programsManager)
    {
        _currentState = new IdlePrintState(this);
        pg = programsManager;
    }
    public ProgramsManager GetProgramsManager() // temporary method TODO: remove later
    {
        return pg;
    }
    public void Play(PrintStateMachine context) => _currentState.Play();
    public void Pause(PrintStateMachine context) => _currentState.Pause();
    public void Redo(PrintStateMachine context) => _currentState.Redo();
    public void Cancel(PrintStateMachine context) => _currentState.Cancel();

}
