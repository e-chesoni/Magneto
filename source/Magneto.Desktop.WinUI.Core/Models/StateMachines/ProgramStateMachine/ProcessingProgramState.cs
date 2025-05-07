using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Magneto.Desktop.WinUI.Core.Contracts.Services.States;
using Magneto.Desktop.WinUI.Core.Models.Print;

namespace Magneto.Desktop.WinUI.Core.Models.StateMachines.ProgramStateMachine;
public class ProcessingProgramState : IProgramState
{
    private readonly RoutineStateMachine _rsm;
    public ProcessingProgramState(RoutineStateMachine rsm)
    {
        _rsm = rsm;
        Process();
    }
    public void Process()
    {
        // TODO: process programs on rsm program list

    }
    public void Pause()
    {
        ChangeStateTo(new PausedProgramState(_rsm));
    }
    public void Add() => throw new NotImplementedException();
    public void Remove() => throw new NotImplementedException();
    public void Cancel()
    {
        // TODO: clear the program list

        ChangeStateTo(new IdleProgramState(_rsm));
    }
    public void ChangeStateTo(IProgramState state) => throw new NotImplementedException();
}
