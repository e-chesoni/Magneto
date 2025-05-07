using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Magneto.Desktop.WinUI.Core.Contracts.Services.States;
using Magneto.Desktop.WinUI.Core.Models.Print;

namespace Magneto.Desktop.WinUI.Core.Models.StateMachines.ProgramStateMachine;
public class PausedProgramState : IProgramState
{
    private readonly RoutineStateMachine _rsm;
    public PausedProgramState(RoutineStateMachine rsm)
    {
        _rsm = rsm;
    }
    public void Process() => throw new NotImplementedException();
    public void Pause() => throw new NotImplementedException();
    public void Add() => throw new NotImplementedException();
    public void Remove() => throw new NotImplementedException();
    public void Cancel() => throw new NotImplementedException();
    public void ChangeStateTo(IProgramState state) => throw new NotImplementedException();
}
