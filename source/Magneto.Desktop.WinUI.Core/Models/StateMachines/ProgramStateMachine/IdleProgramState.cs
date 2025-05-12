using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Magneto.Desktop.WinUI.Core.Contracts.Services;
using Magneto.Desktop.WinUI.Core.Contracts.Services.States;
using Magneto.Desktop.WinUI.Core.Models.Print;

namespace Magneto.Desktop.WinUI.Core.Models.StateMachines.ProgramStateMachine;
public class IdleProgramState : IProgramState
{
    private readonly RoutineStateMachine _rsm;
    public IdleProgramState(RoutineStateMachine rsm)
    {
        MagnetoLogger.Log("Transitioned to RSM Idle State.", LogFactoryLogLevel.LogLevel.WARN);
        _rsm = rsm;
        _rsm.status = RoutineStateMachine.RoutineStateMachineStatus.Idle;
    }
    public async Task<bool> Process()
    {
        IProgramState newState = new ProcessingProgramState(_rsm);
        ChangeStateTo(newState);
        return await newState.Process();
    }
    public void Pause() => throw new NotImplementedException();
    public async Task<bool> Resume() => throw new NotImplementedException();
    public void Add() => throw new NotImplementedException();
    public void Remove() => throw new NotImplementedException();
    public void Cancel() => _rsm.programNodes.Clear();
    public void ChangeStateTo(IProgramState state) => _rsm.ChangeStateTo(state);
}
