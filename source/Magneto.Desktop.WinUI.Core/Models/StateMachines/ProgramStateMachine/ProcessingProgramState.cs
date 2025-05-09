using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using Magneto.Desktop.WinUI.Core.Contracts.Services;
using Magneto.Desktop.WinUI.Core.Contracts.Services.States;
using Magneto.Desktop.WinUI.Core.Models.Print;
using static Magneto.Desktop.WinUI.Core.Models.Constants.MagnetoConstants;
using static Magneto.Desktop.WinUI.Core.Models.Print.RoutineStateMachine;

namespace Magneto.Desktop.WinUI.Core.Models.StateMachines.ProgramStateMachine;
public class ProcessingProgramState : IProgramState
{
    private readonly RoutineStateMachine _rsm;
    public ProcessingProgramState(RoutineStateMachine rsm)
    {
        MagnetoLogger.Log("Transitioned to RSM Processing State.", LogFactoryLogLevel.LogLevel.WARN);
        _rsm = rsm;
    }
    public async Task Process()
    {
        var buildMotorName = _rsm.GetBuildMotor().GetMotorName();
        var powderMotorName = _rsm.GetPowderMotor().GetMotorName();
        var sweepMotorName = _rsm.GetSweepMotor().GetMotorName();

        while (_rsm.GetNumberOfPrograms() > 0)
        {
            // check for pause before starting next program
            if (_rsm.status == RoutineStateMachineStatus.Paused)
            {
                MagnetoLogger.Log("⏸ rsm state is paused. Halting execution.", LogFactoryLogLevel.LogLevel.WARN);
                Pause();
                return;
            }
            // check for cancellation request before starting next program
            if (_rsm.CANCELLATION_REQUESTED)
            {
                MagnetoLogger.Log("🛑 Cancellation requested. Exiting loop.", LogFactoryLogLevel.LogLevel.WARN);
                //StopProgram(); // Ensure STOP flag and program list are cleared
                Cancel();
                return;
            }
            // get the next program on the list
            var programNode = _rsm.GetFirstProgramNode();
            // if the program is null, return
            if (!programNode.HasValue)
            {
                MagnetoLogger.Log("⚠️ No valid program node found. Exiting.", LogFactoryLogLevel.LogLevel.WARN);
                return;
            }
            // else, extract the controller and axis associated with the program
            var confirmedNode = programNode.Value;
            var (_, controller, axis) = _rsm.ExtractProgramNodeVariables(confirmedNode);
            // get the motor name from the controller
            var motorName = controller switch
            {
                Controller.BUILD_AND_SUPPLY when axis == 1 => buildMotorName,
                Controller.BUILD_AND_SUPPLY when axis == 2 => powderMotorName,
                _ => sweepMotorName
            };
            // store request before running
            await StoreLastRequestAndRunProgram(motorName, confirmedNode);
            // Wait while the controller executes the program
            while (await _rsm.IsProgramRunningAsync(motorName))
            {
                if (_rsm.status == RoutineStateMachineStatus.Paused)
                {
                    MagnetoLogger.Log("⏸ rsm state is paused. Halting execution.", LogFactoryLogLevel.LogLevel.WARN);
                    Pause();  // handled by current state
                    return;
                }
                //if (IsProgramStopped())
                if (_rsm.CANCELLATION_REQUESTED)
                {
                    MagnetoLogger.Log($"🛑 Cancellation detected mid-execution on {motorName}.", LogFactoryLogLevel.LogLevel.WARN);

                    // Attempt to stop and flush the controller if possible
                    //StopProgram();
                    Cancel(); // handled by current state (all clear list)
                    return;
                }
                await Task.Delay(100); // Throttle polling
            }
        }
        MagnetoLogger.Log("⚠️ Exiting program processor.", LogFactoryLogLevel.LogLevel.WARN);
        MagnetoLogger.Log($"Programs {_rsm.programNodes.Count} on list:", LogFactoryLogLevel.LogLevel.VERBOSE);
        foreach (var node in _rsm.programNodes)
        {
            MagnetoLogger.Log($"{node.program}\n", LogFactoryLogLevel.LogLevel.VERBOSE);
        }
        ChangeStateTo(new IdleProgramState(_rsm));
    }
    #region Program Processing Methods
    #region Program Processing Helpers

    #endregion
    #region Last Move Methods
    public void SetLastMoveStartingPosition(double start) => _rsm.lastMove.startingPosition = start;
    public void SetLastMoveTarget(double target) => _rsm.lastMove.target = target;
    private async Task StoreLastRequestedMove(string motorNameLower, ProgramNode programNode)
    {
        double startingPosition;

        switch (motorNameLower)
        {
            case "build":
                startingPosition = await _rsm.GetBuildMotor().GetPositionAsync(2);
                break;
            case "powder":
                startingPosition = await _rsm.GetPowderMotor().GetPositionAsync(2);
                break;
            case "sweep":
                startingPosition = await _rsm.GetSweepMotor().GetPositionAsync(2);
                break;
            default:
                MagnetoLogger.Log($"Invalid motor name: {motorNameLower}", LogFactoryLogLevel.LogLevel.ERROR);
                throw new ArgumentException($"Unknown motor: {motorNameLower}");
        }

        var target = _rsm.CalculateTargetPosition(startingPosition, programNode);
        SetLastMoveStartingPosition(startingPosition);
        SetLastMoveTarget(target);
    }
    private async Task StoreLastRequestAndRunProgram(string motorNameLower, ProgramNode programNode)
    {
        switch (motorNameLower)
        {
            case "build":
                _rsm.GetBuildMotor().WriteProgram(programNode.program);
                break;
            case "powder":
                _rsm.GetPowderMotor().WriteProgram(programNode.program);
                break;
            case "sweep":
                _rsm.GetSweepMotor().WriteProgram(programNode.program);
                break;
            default:
                MagnetoLogger.Log($"Unable to send program. Invalid motor name given: {motorNameLower}.", LogFactoryLogLevel.LogLevel.ERROR);
                break;
        }
        await StoreLastRequestedMove(motorNameLower, programNode);
    }
    #endregion
    #endregion

    public void Pause()
    {
        ChangeStateTo(new PausedProgramState(_rsm));
    }
    public async Task Resume() => throw new NotImplementedException();
     public void Add() => throw new NotImplementedException();
    public void Remove() => throw new NotImplementedException();
    public void Cancel()
    {
        MagnetoLogger.Log($"Clearing program list.", LogFactoryLogLevel.LogLevel.ERROR);
        _rsm.programNodes.Clear();
        ChangeStateTo(new IdleProgramState(_rsm));
    }
    public void ChangeStateTo(IProgramState state) => _rsm.ChangeStateTo(state);
}
