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
    #region Private Variables
    private List<StepperMotor> _motorList = new List<StepperMotor>();
    private double _sweepDist { get; set; }
    private IProgramState _currentState;
    #endregion

    #region Public Variables
    public List<MotorController> motorControllers { get; set; } = new List<MotorController>();
    public MotorController buildSupplyController { get; set; }
    public MotorController sweepController { get; set; }
    public LaserController laserController { get; set; }
    public ArtifactModel artifactModel { get; set; }
    public DanceModel danceModel { get; set; }

    public LinkedList<ProgramNode> programNodes = new();

    private LastMove _lastMove;

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
    public bool PROGRAMS_PAUSED;
    public bool PROGRAMS_STOPPED;
    #endregion

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

        //_buildMotorPort = bc.GetPortName();
        //_sweepMotorPort = sc.GetPortName();

        motorControllers.Add(buildSupplyController);
        motorControllers.Add(sweepController);

        foreach(var m in buildSupplyController.GetMinions()) { _motorList.Add(m); }
        foreach (var n in sweepController.GetMinions()) { _motorList.Add(n); }

        // TODO: Move to config file
        // Set default sweep distance
        SetSweepDist(MagnetoConfig.GetSweepDist());

        // Create a dance model
        danceModel = new DanceModel();

        // Start in the idle state
        //TransitionTo(new IdleBuildState(this));
    }

    #endregion

    #region Last Move Methods
    public LastMove GetLastMove() => _lastMove;
    public ProgramNode GetLastProgramNodeRun() => _lastMove.programNode;
    public void SetLastMoveStartingPosition(double start) => _lastMove.startingPosition = start;
    public void SetLastMoveTarget(double target) => _lastMove.target = target;
    #endregion

    #region Program State Handlers
    public bool IsProgramPaused() => PROGRAMS_PAUSED;
    public bool IsProgramStopped() => PROGRAMS_STOPPED;
    public void PauseExecutionFlag()
    {
        PROGRAMS_PAUSED = true;
    }
    public void ResumeExecutionFlag()
    {
        PROGRAMS_PAUSED = false;
        PROGRAMS_STOPPED = false;
    }
    public void StopExecutionFlag()
    {
        PROGRAMS_PAUSED = true;
        PROGRAMS_STOPPED = true;
        programNodes.Clear();
    }
    #endregion

    #region Create Program Node
    public ProgramNode CreateProgramNode(string[] program, Controller controller, int axis)
    {
        return new ProgramNode
        {
            program = program,
            controller = controller,
            axis = axis
        };
    }
    #endregion

    #region Program Adders
    public void AddProgramToFront(ProgramNode node)
    {
        programNodes.AddFirst(node);
    }
    public void AddProgramToBack(ProgramNode node)
    {
        programNodes.AddLast(node);
    }
    #endregion

    #region Program Node Getters
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

    #region Extract Program Variables
    public (string[] program, Controller controller, int axis) ExtractProgramNodeVariables(ProgramNode programNode)
    {
        var program = programNode.program;
        Controller controller = programNode.controller;
        var axis = programNode.axis;
        return (program, controller, axis);
    }
    #endregion

    #region Getters
    public StepperMotor GetBuildMotor()
    {
        return buildSupplyController.GetBuildMotor();
    }

    public StepperMotor GetPowderMotor()
    {
        return buildSupplyController.GetPowderMotor();
    }
    public StepperMotor GetSweepMotor()
    {
        return sweepController.GetSweepMotor();
    }

    public double GetNumberOfPrograms() => programNodes.Count;
    #endregion

    #region Setters

    public void SetSweepDist(double dist)
    {
        _sweepDist = dist;
    }

    /// <summary>
    /// Set the path to the artifact on the artifact model
    /// </summary>
    /// <param name="path"></param>
    public void SetArtifactPath(string path)
    {
        artifactModel.pathToArtifact = path;
    }

    /// <summary>
    /// Set the thickness of print layers on the artifact model
    /// </summary>
    /// <param name="thickness"></param>
    public void SetArtifactThickness(double thickness)
    {
        artifactModel.defaultThickness = thickness;
    }

    /// <summary>
    /// Slice the artifact on the artifact model and store the stack of slices on the artifact model
    /// </summary>
    public void SliceArtifact()
    {
        // TODO: UPDATE in production. Currently uses default number of slices from Magneto Config
        artifactModel.sliceStack = ArtifactHandler.SliceArtifact(artifactModel);
    }

    #endregion
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
    private double CalculateTargetPosition(double startingPosition, ProgramNode programNode)
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
    private async Task StoreLastRequestedMove(string motorNameLower, ProgramNode programNode)
    {
        double startingPosition;

        switch (motorNameLower)
        {
            case "build":
                startingPosition = await GetBuildMotor().GetPositionAsync(2);
                break;
            case "powder":
                startingPosition = await GetPowderMotor().GetPositionAsync(2);
                break;
            case "sweep":
                startingPosition = await GetSweepMotor().GetPositionAsync(2);
                break;
            default:
                MagnetoLogger.Log($"Invalid motor name: {motorNameLower}", LogFactoryLogLevel.LogLevel.ERROR);
                throw new ArgumentException($"Unknown motor: {motorNameLower}");
        }

        var target = CalculateTargetPosition(startingPosition, programNode);
        SetLastMoveStartingPosition(startingPosition);
        SetLastMoveTarget(target);
    }
    private async Task SelectMotorForStoredMove(string motorNameLower, ProgramNode programNode)
    {
        switch (motorNameLower)
        {
            case "build":
                GetBuildMotor().WriteProgram(programNode.program);
                break;
            case "powder":
                GetPowderMotor().WriteProgram(programNode.program);
                break;
            case "sweep":
                GetSweepMotor().WriteProgram(programNode.program);
                break;
            default:
                MagnetoLogger.Log($"Unable to send program. Invalid motor name given: {motorNameLower}.", LogFactoryLogLevel.LogLevel.ERROR);
                break;
        }
        await StoreLastRequestedMove(motorNameLower, programNode);
    }

    public async Task ProcessPrograms()
    {
        var buildMotorName = buildSupplyController.GetBuildMotor().GetMotorName();
        var powderMotorName = buildSupplyController.GetPowderMotor().GetMotorName();
        var sweepMotorName = sweepController.GetSweepMotor().GetMotorName();

        while (GetNumberOfPrograms() > 0)
        {
            // Check pause/stop before starting next program
            if (IsProgramPaused())
            {
                MagnetoLogger.Log("⏸ Program paused. Halting execution.", LogFactoryLogLevel.LogLevel.WARN);
                return;
            }

            if (IsProgramStopped())
            {
                MagnetoLogger.Log("🛑 Program stop requested. Exiting loop.", LogFactoryLogLevel.LogLevel.WARN);
                //StopProgram(); // Ensure STOP flag and program list are cleared
                _currentState.Cancel();
                return;
            }

            var programNode = GetFirstProgramNode();
            if (!programNode.HasValue)
            {
                MagnetoLogger.Log("⚠️ No valid program node found. Exiting.", LogFactoryLogLevel.LogLevel.WARN);
                return;
            }

            var confirmedNode = programNode.Value;
            var (_, controller, axis) = ExtractProgramNodeVariables(confirmedNode);

            var motorName = controller switch
            {
                Controller.BUILD_AND_SUPPLY when axis == 1 => buildMotorName,
                Controller.BUILD_AND_SUPPLY when axis == 2 => powderMotorName,
                _ => sweepMotorName
            };

            await SelectMotorForStoredMove(motorName, confirmedNode);

            // Wait while the controller executes the program
            while (await IsProgramRunningAsync(motorName))
            {
                if (IsProgramStopped())
                {
                    MagnetoLogger.Log($"🛑 Program stop detected mid-execution on {motorName}.", LogFactoryLogLevel.LogLevel.WARN);

                    // Attempt to stop and flush the controller if possible
                    //StopProgram();
                    _currentState.Cancel();
                    return;
                }

                await Task.Delay(100); // Throttle polling
            }
        }
        MagnetoLogger.Log("⚠️ Exiting program processor.", LogFactoryLogLevel.LogLevel.WARN);
        MagnetoLogger.Log($"Programs {programNodes.Count} on list:", LogFactoryLogLevel.LogLevel.VERBOSE);
        foreach (var node in programNodes)
        {
            MagnetoLogger.Log($"{node.program}\n", LogFactoryLogLevel.LogLevel.VERBOSE);
        }
    }

    #region State Methods
    public void Process()
    {
        ChangeStateTo(new ProcessingProgramState(this));
    }
    public void Pause() => throw new NotImplementedException();
    public void Add() => throw new NotImplementedException();
    public void Remove() => throw new NotImplementedException();
    public void Cancel() => throw new NotImplementedException();
    public void ChangeStateTo(IProgramState state) => throw new NotImplementedException();
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
