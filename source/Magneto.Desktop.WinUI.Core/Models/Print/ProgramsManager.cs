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
using static Magneto.Desktop.WinUI.Core.Models.Print.ProgramsManager;
using MongoDB.Driver;

namespace Magneto.Desktop.WinUI.Core.Models.Print;

/// <summary>
/// Coordinates printing tasks across components
/// </summary>
public class ProgramsManager : ISubsciber
{
    #region Private Variables

    private List<StepperMotor> _motorList = new List<StepperMotor>();

    private double _sweepDist { get; set; }

    private double _currentPrintHeight { get; set; }

    #endregion

    #region Public Variables

    /// NOTE: Currently, Magneto has two motor controllers; 
    /// One for the build motors (on the base of the housing)
    /// And one for powder distribution (sweep) motor
    /// 
    public List<MotorController> motorControllers { get; set; } = new List<MotorController>();

    /// <summary>
    /// Controller for build motors
    /// </summary>
    public MotorController buildController { get; set; }

    /// <summary>
    /// Controller for sweep motor
    /// </summary>
    public MotorController sweepController { get; set; }

    /// <summary>
    /// Controller for laser and scan head
    /// </summary>
    public LaserController laserController { get; set; }

    /// <summary>
    /// Artifact model is generated for each print
    /// </summary>
    public ArtifactModel artifactModel { get; set; }

    /// <summary>
    /// Dance model generates consumable process of artifact model for print
    /// (Prepares slices by thickness for printing state)
    /// </summary>
    public DanceModel danceModel { get; set; }


    #region State Variables

    /// <summary>
    /// Holder for current state (set in constructor and updated by state methods)
    /// </summary>
    private IPrintState _state = null;

    /// <summary>
    /// Flag used to interrupt builds
    /// </summary>
    public BuildFlag build_flag;

    #endregion

    /// <summary>
    /// Various build flags
    /// </summary>
    public enum BuildFlag : ushort
    {
        RESUME,
        PAUSE,
        CANCEL
    }

    private string _buildMotorPort
    {
        get; set;
    }
    private string _sweepMotorPort
    {
        get; set;
    }

    public LinkedList<ProgramNode> programLinkedList = new();

    private LastMove _lastMove;

    // All controller types are 5 letters long
    public enum ControllerType
    {
        BUILD, // Corresponds to build motors
        SWEEP, // Corresponds to sweep motor
        LASER // Corresponds to Waverunner
    }

    // NOTE: Stop commands should not be added to queue; should be called directly
    public enum CommandType
    {
        AbsoluteMove, // Corresponds to "MVA" for absolute movements
        RelativeMove, // Corresponds to "MVR" for relative movements
        PositionQuery, // Corresponds to "POS?" for querying current position
        // TODO: implement wait for end command
    }

    public struct ProgramNode
    {
        public string[] program;
        public Controller controller;
        public int axis;
    }

    public bool PAUSE_REQUESTED;

    public struct LastMove
    {
        public ProgramNode programNode;
        public double startingPosition;
        public double target;
    }

    #endregion

    #region Constructor

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="buildController"></param> Build Controller
    /// <param name="sweepController"></param> Sweep/Powder Distribution Controller
    /// <param name="laserController"></param> Laser Controller
    public ProgramsManager(MotorController bc, MotorController sc, LaserController lc)
    {
        buildController = bc;
        sweepController = sc;
        laserController = lc;

        //_buildMotorPort = bc.GetPortName();
        //_sweepMotorPort = sc.GetPortName();

        motorControllers.Add(buildController);
        motorControllers.Add(sweepController);

        foreach(var m in buildController.GetMinions()) { _motorList.Add(m); }
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
    public bool IsProgramPaused() => PAUSE_REQUESTED;
    public void PauseExecutionFlag()
    {
        PAUSE_REQUESTED = true;
    }
    public void ResumeExecutionFlag()
    {
        PAUSE_REQUESTED = true;
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
        programLinkedList.AddFirst(node);
    }
    public void AddProgramToBack(ProgramNode node)
    {
        programLinkedList.AddLast(node);
    }
    #endregion

    #region Program Node Getters
    public ProgramNode GetFirstProgramNode()
    {
        if (programLinkedList.Count == 0)
        {
            MagnetoLogger.Log("Cannot remove program from front of linked list; program linked list is empty.", LogFactoryLogLevel.LogLevel.ERROR);
            var empty = Array.Empty<string>();
            return new ProgramNode();
        }
        var programNode = programLinkedList.First.Value;
        //(program, controller, axis) = ExtractProgramNodeVariables(programNode);
        programLinkedList.RemoveFirst();
        MagnetoLogger.Log("Removing first program from linked list:", LogFactoryLogLevel.LogLevel.VERBOSE);
        foreach (var line in programNode.program)
        {
            MagnetoLogger.Log($"{line}\n", LogFactoryLogLevel.LogLevel.VERBOSE);
        }
        return programNode;
    }
    public ProgramNode GetLastProgramNode()
    {
        if (programLinkedList.Count == 0)
        {
            MagnetoLogger.Log("Cannot remove program from back of linked list; program linked list is empty.", LogFactoryLogLevel.LogLevel.ERROR);
            return new ProgramNode();
        }
        var programNode = programLinkedList.Last.Value;
        //(program, controller, axis) = ExtractProgramNodeVariables(programNode);
        programLinkedList.RemoveLast();
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
    public double GetCurrentPrintHeight()
    {
        return _currentPrintHeight; 
    }

    public List<StepperMotor> GetMotorList()
    {
        return _motorList;
    }

    public StepperMotor GetPowderMotor()
    {
        return buildController.GetPowderMotor();
    }

    public StepperMotor GetBuildMotor()
    {
        return buildController.GetBuildMotor();
    }

    public StepperMotor GetSweepMotor()
    {
        return sweepController.GetSweepMotor();
    }
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
