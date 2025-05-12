using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Magneto.Desktop.WinUI.Core.Contracts.Services;
using Magneto.Desktop.WinUI.Core.Contracts.Services.States;
using Magneto.Desktop.WinUI.Core.Models.Motors;
using Magneto.Desktop.WinUI.Core.Models.Print;
using Magneto.Desktop.WinUI.Core.Services;
using static Magneto.Desktop.WinUI.Core.Models.Constants.MagnetoConstants;
using static Magneto.Desktop.WinUI.Core.Models.Print.RoutineStateMachine;

namespace Magneto.Desktop.WinUI.Core.Models.StateMachines.ProgramStateMachine;
public class PausedProgramState : IProgramState
{
    private readonly RoutineStateMachine _rsm;
    public PausedProgramState(RoutineStateMachine rsm)
    {
        MagnetoLogger.Log("Transitioned to RSM Paused State.", LogFactoryLogLevel.LogLevel.WARN);
        _rsm = rsm;
        MagnetoLogger.Log("Changing rsm state to paused.", LogFactoryLogLevel.LogLevel.WARN);
        _rsm.status = RoutineStateMachineStatus.Paused;
    }
    public async Task<bool> Process()
    {
        IProgramState newState = new ProcessingProgramState(_rsm);
        ChangeStateTo(newState);
        return await newState.Process();
    }
    public void Pause() => _rsm.status = RoutineStateMachineStatus.Paused;
    public async Task<bool> Resume()
    {
        bool programComplete;
        StepperMotor motor;
        // Figure out if the last program finished:
        // get the last program node and extract its variables
        LastMove lastMove = _rsm.GetLastMove();
        ProgramNode lastProgramNode = lastMove.programNode;
        (_, Controller controller, var axis) = _rsm.ExtractProgramNodeVariables(lastProgramNode);
        // use controller and axis to determine which motor command was called on
        if (controller == Controller.BUILD_AND_SUPPLY)
        {
            if (axis == _rsm.GetBuildMotor().GetAxis())
            {
                motor = _rsm.GetBuildMotor();
            }
            else
            {
                motor = _rsm.GetPowderMotor();
            }
        }
        else if (controller == Controller.SWEEP)
        {
            motor = _rsm.GetSweepMotor();
        }
        else
        {
            MagnetoLogger.Log("Cannot resume reading program. No motor found.", LogFactoryLogLevel.LogLevel.ERROR);
            programComplete = false;
            return programComplete;
        }
        // get the current position
        var currentPostion = await motor.GetPositionAsync(2);
        // calculate the targeted position
        var target = _rsm.CalculateTargetPosition(lastMove);
        // if target is less than current position, moveUp = false
        var moveUp = target < currentPostion ? false : true;
        // if motor did not reach target, put absolute move command to move motor to target at the front of the program list
        if (currentPostion != target)
        {
            MagnetoLogger.Log($"Resuming program: {motor.GetMotorName} did not reach its target: {target}. Current position: {currentPostion}.", LogFactoryLogLevel.LogLevel.WARN);
            var absoluteProgram = _rsm.WriteAbsoluteMoveProgram(motor, target);
            _rsm.AddProgramFront(motor.GetMotorName(), absoluteProgram);
        }
        // set the pause requested flag to false
        //EnableProgramProcessing();
        // resume executing process program
        return await Process(); // will transition to processing state and process program
    }
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
