using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Magneto.Desktop.WinUI.Core.Contracts.Services;
using Magneto.Desktop.WinUI.Core.Contracts.Services.Controllers;
using Magneto.Desktop.WinUI.Core.Contracts.Services.States;
using Magneto.Desktop.WinUI.Core.Models.Controllers;
using Magneto.Desktop.WinUI.Core.Models.Artifact;
using Magneto.Desktop.WinUI.Core.Models.Monitors;
using Magneto.Desktop.WinUI.Core.Models.Motors;
using Magneto.Desktop.WinUI.Core.Models.State.PrintStates;
using static Magneto.Desktop.WinUI.Core.Models.Motors.StepperMotor;
using Magneto.Desktop.WinUI.Core.Contracts.Services.States;
using Magneto.Desktop.WinUI.Core.Contracts;
using static Magneto.Desktop.WinUI.Core.Models.Constants.MagnetoConstants;
using static Magneto.Desktop.WinUI.Core.Models.Print.RoutineStateMachine;
using MongoDB.Driver;
using Magneto.Desktop.WinUI.Core.Models.StateMachines.ProgramStateMachine;
using Magneto.Desktop.WinUI.Core.Services;

namespace Magneto.Desktop.WinUI.Core.Models.Print;

/// <summary>
/// Coordinates printing tasks across components
/// </summary>
public class RoutineStateMachine : ISubsciber
{
    private IProgramState _currentState;
    public RoutineStateMachineStatus status; 
    private List<StepperMotor> _motorList = new List<StepperMotor>();
    public List<MotorController> motorControllers { get; set; } = new List<MotorController>();
    public MotorController buildSupplyController { get; set; }
    public MotorController sweepController { get; set; }
    public LaserController laserController { get; set; }

    public LinkedList<ProgramNode> programNodes = new();
    public LastMove lastMove;

    // All controller types are 5 letters long
    public struct ProgramNode
    {
        public string[] program;
        public Controller controller;
        public int axis;
    }
    public struct LastMove
    {
        public ProgramNode programNode;
        public double startingPosition;
        public double target;
    }
    public enum RoutineStateMachineStatus
    {
        Idle,
        Processing,
        Paused
    }

    // TODO: remove booleans; use states and status
    public bool PROGRAMS_PAUSED;
    public bool PROGRAMS_STOPPED;
    public bool CANCELLATION_REQUESTED;

    #region Constructor
    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="buildController"></param> Build Controller
    /// <param name="sweepController"></param> Sweep/Powder Distribution Controller
    /// <param name="laserController"></param> Laser Controller
    public RoutineStateMachine(MotorController bc, MotorController sc, LaserController lc)
    {
        buildSupplyController = bc;
        sweepController = sc;
        laserController = lc;

        motorControllers.Add(buildSupplyController);
        motorControllers.Add(sweepController);

        foreach(var m in buildSupplyController.GetMinions()) { _motorList.Add(m); }
        foreach (var n in sweepController.GetMinions()) { _motorList.Add(n); }

        CANCELLATION_REQUESTED = false;
        
        // Start in the idle state
        status = RoutineStateMachineStatus.Idle;
        ChangeStateTo(new IdleProgramState(this));
    }
    #endregion

    #region Getters
    public StepperMotor GetBuildMotor() => buildSupplyController.GetBuildMotor();
    public StepperMotor GetPowderMotor() => buildSupplyController.GetPowderMotor();
    public StepperMotor GetSweepMotor() => sweepController.GetSweepMotor();
    public double GetNumberOfPrograms() => programNodes.Count;
    public LastMove GetLastMove() => lastMove;
    public ProgramNode GetLastProgramNodeRun() => lastMove.programNode;
    public ProgramNode? GetFirstProgramNode()
    {
        if (programNodes.Count == 0)
        {
            MagnetoLogger.Log("Cannot remove program from front of linked list; program linked list is empty.", LogFactoryLogLevel.LogLevel.ERROR);
            return null;
        }
        var programNode = programNodes.First.Value;
        //(program, controller, axis) = ExtractProgramNodeVariables(programNode);
        programNodes.RemoveFirst();
        MagnetoLogger.Log("Removing first program from linked list:", LogFactoryLogLevel.LogLevel.VERBOSE);
        foreach (var line in programNode.program)
        {
            MagnetoLogger.Log($"{line}\n", LogFactoryLogLevel.LogLevel.VERBOSE);
        }
        return programNode;
    }
    public ProgramNode GetLastProgramNode()
    {
        if (programNodes.Count == 0)
        {
            MagnetoLogger.Log("Cannot remove program from back of linked list; program linked list is empty.", LogFactoryLogLevel.LogLevel.ERROR);
            return new ProgramNode();
        }
        var programNode = programNodes.Last.Value;
        //(program, controller, axis) = ExtractProgramNodeVariables(programNode);
        programNodes.RemoveLast();
        MagnetoLogger.Log("Removing last program from linked list:", LogFactoryLogLevel.LogLevel.VERBOSE);
        foreach (var line in programNode.program)
        {
            MagnetoLogger.Log($"{line}\n", LogFactoryLogLevel.LogLevel.VERBOSE);
        }
        return programNode;
    }
    #endregion

    #region Basic Programming
    public ProgramNode CreateProgramNode(string[] program, Controller controller, int axis)
    {
        return new ProgramNode
        {
            program = program,
            controller = controller,
            axis = axis
        };
    }
    public void AddProgramToFront(ProgramNode node) => programNodes.AddFirst(node);
    public void AddProgramToBack(ProgramNode node) => programNodes.AddLast(node);
    public void ClearProgramList() => programNodes.Clear();
    #region Add to Front Wrappers
    private void AddProgramFrontHelper(string[] program, Controller controller, int axis)
    {
        ProgramNode programNode = CreateProgramNode(program, controller, axis);
        AddProgramToFront(programNode);
    }
    private void AddBuildMotorProgramFront(string[] program)
    {
        AddProgramFrontHelper(program, Controller.BUILD_AND_SUPPLY, GetBuildMotor().GetAxis());
    }
    private void AddPowderMotorProgramFront(string[] program)
    {
        AddProgramFrontHelper(program, Controller.BUILD_AND_SUPPLY, GetPowderMotor().GetAxis());
    }
    private void AddSweepMotorProgramFront(string[] program)
    {
        AddProgramFrontHelper(program, Controller.SWEEP, GetSweepMotor().GetAxis());
    }
    public void AddProgramFront(string motorNameLower, string[] program)
    {
        switch (motorNameLower)
        {
            case "build":
                AddBuildMotorProgramFront(program);
                break;
            case "powder":
                AddPowderMotorProgramFront(program);
                break;
            case "sweep":
                AddSweepMotorProgramFront(program);
                break;
            default:
                return;
        }
    }
    #endregion

    #region Add Last Wrappers
    private void AddProgramLastHelper(string[] program, Controller controller, int axis)
    {
        ProgramNode programNode = CreateProgramNode(program, controller, axis);
        AddProgramToBack(programNode);
    }
    private void AddBuildMotorProgramLast(string[] program)
    {
        AddProgramLastHelper(program, Controller.BUILD_AND_SUPPLY, GetBuildMotor().GetAxis());
    }
    private void AddPowderMotorProgramLast(string[] program)
    {
        AddProgramLastHelper(program, Controller.BUILD_AND_SUPPLY, GetPowderMotor().GetAxis());
    }
    private void AddSweepMotorProgramLast(string[] program)
    {
        AddProgramLastHelper(program, Controller.SWEEP, GetSweepMotor().GetAxis());
    }
    public void AddProgramLast(string motorNameLower, string[] program)
    {
        switch (motorNameLower)
        {
            case "build":
                AddBuildMotorProgramLast(program);
                break;
            case "powder":
                AddPowderMotorProgramLast(program);
                break;
            case "sweep":
                AddSweepMotorProgramLast(program);
                break;
            default:
                return;
        }
    }
    #endregion
    
    #region Program Writers
    public string[] WriteAbsoluteMoveProgram(StepperMotor motor, double target) => motor.CreateMoveProgramHelper(target, true);
    public string[] WriteAbsoluteMoveProgramForBuildMotor(double target) => WriteAbsoluteMoveProgram(GetBuildMotor(), target);
    public string[] WriteAbsoluteMoveProgramForPowderMotor(double target) => WriteAbsoluteMoveProgram(GetPowderMotor(), target);
    public string[] WriteAbsoluteMoveProgramForSweepMotor(double target) => WriteAbsoluteMoveProgram(GetSweepMotor(), target);

    public string[] WriteRelativeMoveProgram(StepperMotor motor, double steps, bool moveUp) => motor.CreateMoveProgramHelper(steps, false, moveUp);
    public string[] WriteRelativeMoveProgramForBuildMotor(double steps, bool moveUp) => WriteRelativeMoveProgram(GetBuildMotor(), steps, moveUp);
    public string[] WriteRelativeMoveProgramForPowderMotor(double steps, bool moveUp) => WriteRelativeMoveProgram(GetPowderMotor(), steps, moveUp);
    public string[] WriteRelativeMoveProgramForSweepMotor(double steps, bool moveUp) => WriteRelativeMoveProgram(GetSweepMotor(), steps, moveUp);
    #endregion
    #endregion

    #region Processing Helpers
    public (double? value, bool isAbsolute) ParseMoveCommand(string[] program)
    {
        for (var i = program.Length - 1; i >= 0; i--)
        {
            var line = program[i];

            if (line.Contains("MVA") || line.Contains("MVR"))
            {
                var isAbsolute = line.Contains("MVA");
                var prefix = isAbsolute ? "MVA" : "MVR";
                var prefixIndex = line.IndexOf(prefix);

                var target = line.Substring(prefixIndex + 3); // after "MVA" or "MVR"
                if (double.TryParse(target, out var value))
                {
                    return (value, isAbsolute);
                }
                break;
            }
        }
        MagnetoLogger.Log($"No move command found.", LogFactoryLogLevel.LogLevel.ERROR);
        return (null, false);
    }
    public double CalculateTargetPosition(double startingPosition, ProgramNode programNode)
    {
        var (value, isAbsolute) = ParseMoveCommand(programNode.program);

        if (value == null)
        {
            throw new InvalidOperationException("Move command parsing failed: no value found.");
        }

        return isAbsolute ? value.Value : startingPosition + value.Value;
    }
    public double CalculateTargetPosition(LastMove lastMove)
    {
        var programNode = lastMove.programNode;
        var startingPosition = lastMove.startingPosition;
        var (value, isAbsolute) = ParseMoveCommand(programNode.program);

        if (value == null)
        {
            throw new InvalidOperationException("Move command parsing failed: no value found.");
        }

        return isAbsolute ? value.Value : startingPosition + value.Value;
    }
    public (string[] program, Controller controller, int axis) ExtractProgramNodeVariables(ProgramNode programNode)
    {
        var program = programNode.program;
        Controller controller = programNode.controller;
        var axis = programNode.axis;
        return (program, controller, axis);
    }
    #endregion

    #region Program State Handlers
    public async Task<bool> IsProgramRunningAsync(string motorNameLower)
    {
        switch (motorNameLower)
        {
            case "build":
                return await GetBuildMotor().IsProgramRunningAsync();
            case "powder":
                return await GetPowderMotor().IsProgramRunningAsync();
            case "sweep":
                return await GetSweepMotor().IsProgramRunningAsync();
            default:
                MagnetoLogger.Log($"Unable to check if program is running. Invalid motor name given: {motorNameLower}.", LogFactoryLogLevel.LogLevel.ERROR);
                return false;
        }
    }
    #endregion

    #region State Methods
    public async Task<bool> Process() => await _currentState.Process();
    public void Pause() => _currentState.Pause();
    public void Resume() => _currentState.Resume();
    public void Add() => _currentState.Add();
    public void Remove() => _currentState.Remove();
    public void Cancel()
    {
        _currentState.Cancel();
        ChangeStateTo(new IdleProgramState(this));
    }
    public void ChangeStateTo(IProgramState state) => _currentState = state;
    #endregion

    #region Subscriber Methods

    /// <summary>
    /// Receive an update from a publisher the build manager has subscribed to
    /// </summary>
    /// <param name="publisher"></param>
    /// <exception cref="NotImplementedException"></exception>
    public void ReceiveUpdate(IPublisher publisher) => throw new NotImplementedException();

    /// <summary>
    /// Handle an update from a publisher the build manager is subscribed to
    /// </summary>
    /// <param name="publisher"></param>
    /// <exception cref="NotImplementedException"></exception>
    public void HandleUpdate(IPublisher publisher) => throw new NotImplementedException();

    #endregion
}
