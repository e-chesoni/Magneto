using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Magneto.Desktop.WinUI.Core.Contracts.Services;
using Magneto.Desktop.WinUI.Core.Contracts.Services.Controllers;
using Magneto.Desktop.WinUI.Core.Contracts.Services.StateMachineServices;
using Magneto.Desktop.WinUI.Core.Models.Controllers;
using Magneto.Desktop.WinUI.Core.Models.Artifact;
using Magneto.Desktop.WinUI.Core.Models.Monitor;
using Magneto.Desktop.WinUI.Core.Models.Motor;
using Magneto.Desktop.WinUI.Core.Models.State.BuildManagerStates;
using Magneto.Desktop.WinUI.Core.Services;
using static Magneto.Desktop.WinUI.Core.Models.Motor.StepperMotor;

namespace Magneto.Desktop.WinUI.Core.Models.BuildModels;

/// <summary>
/// Coordinates printing tasks across components
/// </summary>
public class BuildManager : ISubsciber, IStateMachine
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
    private IBuildManagerState _state = null;

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

    public struct MotorKey
    {
        public ControllerType ControllerType;
        public int Axis;

        public MotorKey(ControllerType controllerType, int axis)
        {
            ControllerType = controllerType;
            Axis = axis;
        }
    }

    private Dictionary<MotorKey, TaskCompletionSource<double>> positionTasks = new Dictionary<MotorKey, TaskCompletionSource<double>>();
    private Queue<string> commandQueue = new Queue<string>();
    private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
    private bool isCommandProcessing = false;

    // All controller types are 5 letters long
    public enum ControllerType
    {
        BUILD, // Corresponds to build motors
        SWEEP, // Corresponds to sweep motor
        LASER // Corresponds to Waverunner
    }

    public enum CommandType
    {
        AbsoluteMove, // Corresponds to "MVA" for absolute movements
        RelativeMove, // Corresponds to "MVR" for relative movements
        PositionQuery // Corresponds to "POS?" for querying current position
    }

    #endregion


    #region Constructor

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="buildController"></param> Build Controller
    /// <param name="sweepController"></param> Sweep/Powder Distribution Controller
    /// <param name="laserController"></param> Laser Controller
    public BuildManager(MotorController bc, MotorController sc, LaserController lc)
    {
        MagnetoLogger.Log("",
            LogFactoryLogLevel.LogLevel.VERBOSE);

        buildController = bc;
        sweepController = sc;
        laserController = lc;

        _buildMotorPort = bc.GetPortName();
        _sweepMotorPort = sc.GetPortName();

        motorControllers.Add(buildController);
        motorControllers.Add(sweepController);

        foreach(var m in buildController.GetMotorList()) { _motorList.Add(m); }
        foreach (var n in sweepController.GetMotorList()) { _motorList.Add(n); }

        // TODO: Move to config file
        // Set default sweep distance
        SetSweepDist(MagnetoConfig.GetSweepDist());

        // Subscribe to the PositionReported event
        //PositionReported += HandlePositionReported;

        // Create a dance model
        danceModel = new DanceModel();

        // Start in the idle state
        TransitionTo(new IdleBuildState(this));
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

    public double GetSweepDist()
    {
        return _sweepDist;
    }

    public string GetBuildMotorPort()
    {
        return _buildMotorPort; 
    }

    public string GetSweepMotorPort()
    {
        return _sweepMotorPort;

    }

    /// <summary>
    /// Get the status of a given motor
    /// </summary>
    /// <param name="axis"></param> MotorAxis from enum
    /// <returns></returns>
    /// <exception cref="NotImplementedException"></exception>
    public MotorStatus GetMotorStatus(int motorId)
    {
        // Create temp list for motors
        List <StepperMotor> motors = new List<StepperMotor>();

        // Go through all the motor controllers attached to the build manager
        foreach (var c in motorControllers)
        {
            // Add each motor on controller to temporary motor list (created above)
            foreach (var m in c.GetMotorList()) { motors.Add(m); }
        }

        // Find the stepper motor on the motor list that matches the given motorId
        StepperMotor motor = motors.FirstOrDefault(motor => motor.GetID() == motorId);

        // TODO: Return 'ERROR' status if no motor is found

        // Return the status of found motor
        return motor.GetStatus();

    }

    /// <summary>
    /// Get the thickness of print layers on the artifact model
    /// </summary>
    /// <returns></returns>
    public double GetDefaultArtifactThickness()
    {
        return MagnetoConfig.GetDefaultPrintThickness();
    }

    #endregion


    #region Setters

    public void SetCurrentPrintHeight(double printHeight)
    {
        _currentPrintHeight = printHeight;
    }


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
        artifactModel.path_to_artifact = path;
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


    #region Queue Management

    public Task<double> AddCommand(ControllerType controllerType, int axis, CommandType cmdType, double dist)
    {
        var msg = "Adding Command to Queue. Locking commandQueue";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);

        TaskCompletionSource<double> tcs = null;
        MotorKey key = new MotorKey(controllerType, axis);

        if (cmdType == CommandType.PositionQuery)
        {
            tcs = new TaskCompletionSource<double>();
            positionTasks[key] = tcs;
        }

        lock (commandQueue)
        {
            string command = $"{controllerType}{axis}";

            switch (cmdType)
            {
                case CommandType.AbsoluteMove:
                    command += $"MVA{dist}";
                    break;
                case CommandType.RelativeMove:
                    command += $"MVR{dist}";
                    break;
                case CommandType.PositionQuery:
                    command += "POS?";
                    break;
            }

            commandQueue.Enqueue(command);
            if (!isCommandProcessing)
            {
                isCommandProcessing = true;
                Task.Run(() => ProcessCommands());
            }
        }

        // Ensures a task is always returns even if the task is null:
        // Return the Task from the TaskCompletionSource if it's not null; 
        // otherwise, return a completed Task<double> with a result of 0.0.
        return tcs?.Task ?? Task.FromResult(0.0);
    }

    private async Task ProcessCommands()
    {
        var msg = "Processing build manager command queue...";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);

        while (commandQueue.Count > 0)
        {
            string command;
            lock (commandQueue)
            {
                command = commandQueue.Dequeue();
            }

            ControllerType controllerType = (ControllerType)Enum.Parse(typeof(ControllerType), command.Substring(0,5));
            int axis = int.Parse(command.Substring(5,1));
            MotorKey key = new MotorKey(controllerType, axis);
            string motorCommand = command.Substring(6);

            // Based on the controller type, fetch the correct controller
            var controller = GetController(controllerType);

            // Process the command with the respective controller
            if (controller != null)
            {
                if (controller is IMotorController motorController)
                {
                    var motorList = motorController.GetMotorList();
                    
                    // Get the motor matching the extrapolated axis (above) from the controller
                    StepperMotor motor = controller.GetMotorList().FirstOrDefault(m => m.GetID() % 10 == axis);
                    if (motor != null)
                    {
                        if (motorCommand.Contains("POS"))
                        {
                            double position = motor.GetCurrentPos();
                            if (positionTasks.TryGetValue(key, out var tcs))
                            {
                                tcs.SetResult(position);
                                positionTasks.Remove(key);
                            }
                        }
                        else
                        {
                            msg = $"Found motor on axis: {axis}. Executing command: {motorCommand}.";
                            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.SUCCESS);
                            await ExecuteMotorCommand(motor, motorCommand);
                        }
                    }
                    else
                    {
                        msg = $"No motor with Axis {axis} found.";
                        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
                    }
                }
            }
            else
            {
                msg = $"Controller not found for type {controllerType}.";
                MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
            }
        }

        isCommandProcessing = false;
    }

    private IController GetController(ControllerType type)
    {
        return type switch
        {
            ControllerType.BUILD => buildController,
            ControllerType.SWEEP => sweepController,
            ControllerType.LASER => laserController,
            _ => throw new ArgumentOutOfRangeException(nameof(type), $"Unsupported controller type: {type}")
        };
    }

    private async Task ExecuteMotorCommand(StepperMotor motor, string motorCommand)
    {
        //double value = double.Parse(motorCommand.Substring(3));
        double value = double.Parse(motorCommand[3..]); // TODO: test use of range operator (prettier code)
        var msg = $"Executing command {value}.";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.SUCCESS);

        if (motorCommand.StartsWith("MVA"))
        {
            await motor.MoveMotorAbsAsync(value);
        }
        else if (motorCommand.StartsWith("MVR"))
        {
            await motor.MoveMotorRelAsync(value);
        }
        else
        {
            msg = "Failed to execute motor command. Command not recognized.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
        }
    }

    #endregion

    #region State Machine Methods

    /// <summary>
    /// Method to handle state transitions
    /// </summary>
    /// <param name="state"></param>
    public void TransitionTo(IBuildManagerState state)
    {
        _state = state;
    }

    /// <summary>
    /// Start a print
    /// </summary>
    /// <param name="am"></param> artifact model to print
    public void StartPrint(ArtifactModel am)
    {
        _state.Start(am);
    }

    /// <summary>
    /// pause a print
    /// </summary>
    public void Pause()
    {
        _state.Pause();
    }

    /// <summary>
    /// Resume a print
    /// </summary>
    public void Resume()
    {
        _state.Resume();
    }

    /// <summary>
    /// Cancel a print; artifact model will we destroyed
    /// </summary>
    public void Cancel()
    {
        _state.Cancel();
    }

    /// <summary>
    /// Home the motors
    /// </summary>
    public void HomeMotors()
    {
        _state.Homing();
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
