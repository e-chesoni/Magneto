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
using Magneto.Desktop.WinUI.Core.Models.Image;
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
    /// Image model is generated for each print
    /// </summary>
    public ImageModel imageModel { get; set; }

    /// <summary>
    /// Dance model generates consumable process of image model for print
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

    /// <summary>
    /// Motor axis to get the status of a specific motor the controller as knowledge of
    /// </summary>
    public enum MotorAxis : ushort
    {
        MOTOR1 = 1,
        MOTOR2 = 2,
        SWEEP = 1, // NOTE: Colloquially refereed to as powder motor or linear motor on occasion
    }

    private string _buildMotorPort
    {
        get; set;
    }
    private string _sweepMotorPort
    {
        get; set;
    }

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

        // TODO: Add motors to list
        foreach(var m in buildController.GetMotorList()) { _motorList.Add(m); }
        foreach (var n in sweepController.GetMotorList()) { _motorList.Add(n); }

        // TODO: Move to config file
        // Set default sweep distance
        SetSweepDist(MagnetoConfig.GetSweepDist());

        // Create a dance model
        danceModel = new DanceModel();

        // Start in the idle state
        TransitionTo(new IdleBuildManagerState(this));
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

        foreach (var c in motorControllers)
        {
            foreach (var m in c.GetMotorList()) { motors.Add(m); }
        }

        StepperMotor motor = motors.FirstOrDefault(motor => motor.GetID() == motorId);
        return motor.GetStatus();

    }

    /// <summary>
    /// Get the thickness of print layers on the image model
    /// </summary>
    /// <returns></returns>
    public double GetImageThickness()
    {
        return imageModel.thickness;
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
    /// Set the path to the image on the image model
    /// </summary>
    /// <param name="path"></param>
    public void SetImagePath(string path)
    {
        imageModel.path_to_image = path;
    }

    /// <summary>
    /// Set the thickness of print layers on the image model
    /// </summary>
    /// <param name="tickness"></param>
    public void SetImageThickness(double tickness)
    {
        imageModel.thickness = tickness;
    }

    /// <summary>
    /// Slice the image on the image model and store the stack of slices on the image model
    /// </summary>
    public void SliceImage()
    {
        // TODO: UPDATE in production. Currently uses default number of slices from Magneto Config
        imageModel.sliceStack = ImageHandler.SliceImage(imageModel);
    }

    #endregion

    #region Queue Management

    /// <summary>
    /// Adds a command to the motor command queue.
    /// </summary>
    /// <param name="axis">The axis of the motor to which the command will be sent.</param>
    /// <param name="cmdType">The type of movement or query command.</param>
    /// <param name="dist">The distance or position for movement commands; ignored for position queries.</param>
    public void AddCommand(ControllerType controllerType, int axis, CommandType cmdType, double dist)
    {
        var msg = "Adding Command to Queue. Locking commandQueue";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);


        lock (commandQueue)
        {
            var command = "";

            switch (controllerType)
            {
                case ControllerType.BUILD:
                    command += $"BUILD{axis}";
                    break;
                case ControllerType.SWEEP:
                    command += $"SWEEP{axis}";
                    break;
                case ControllerType.LASER:
                    command += $"LASER{axis}";
                    break;
            }

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

            // Example cmd: BUILD1MVR5
            commandQueue.Enqueue(command);
            if (!isCommandProcessing)
            {
                isCommandProcessing = true;
                Task.Run(() => ProcessCommands());
            }
        }
    }

    private async Task ProcessCommands()
    {
        var msg = "Processing controller command queue...";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);

        while (commandQueue.Count > 0)
        {
            string command;
            lock (commandQueue)
            {
                command = commandQueue.Dequeue();
            }

            // TODO: get the controller
            
            // Set default controllers
            MotorController controller = buildController;
            LaserController wv = laserController; // TOOD: Not setup yet

            // Get controller type from command
            string ctlType = command.Substring(0, 5);

            switch (ctlType)
            {
                case "BUILD":
                    controller = buildController;
                    break;
                case "SWEEP":
                    controller = sweepController;
                    break;
                case "LASER":
                    wv = laserController;
                    break;
            }

            // TODO: print axis to make sure this gets the axis
            int axis = int.Parse(command.Substring(5, 6)); // In command, axis appears after 5-letter controller type
            string motorCommand = command.Substring(6);

            // Search motor list for id match; return that motor
            //StepperMotor motor = _motorList.FirstOrDefault(motor => motor.GetID() % 10 == axis);

            // Search controller motor list
            StepperMotor motor = controller.GetMotorList().FirstOrDefault(motor => motor.GetID() % 10 == axis);

            if (motor != null)
            {
                msg = $"Found motor on axis: {axis}. Adding command associated with this motor to the control queue.";
                MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.SUCCESS);

                if (motorCommand.Contains("POS"))
                {
                    msg = $"Processing POS command...";
                    MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);

                    // TOOD: Return motor position

                }

                // TODO: Get command type (POS, MVR, or MVA)
                else if (motorCommand.Contains("MVA"))
                {
                    msg = $"Processing MVA command...";
                    MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);

                    double pos = double.Parse(motorCommand.Substring(3));
                    
                    msg = $"Pos to get to: {pos}";
                    MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);
                    
                    await controller.MoveMotorAbsAsync(motor, pos);
                    //await motor.MoveMotorAbsAsync(pos);
                }

                else if (motorCommand.Contains("MVR"))
                {
                    msg = $"Processing MVR command: {motorCommand}";
                    MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);

                    double step = double.Parse(motorCommand.Substring(3));

                    msg = $"Steps to process: {step}";
                    MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);
                    
                    await controller.MoveMotorRelAsync(motor, step);
                    //await motor.MoveMotorRelAsync(step);
                }
            }
            else
            {
                msg = $"No motor with Axis {axis} found.";
                MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
            }
        }

        msg = "Done processing queue. Unlocking commandQueue";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);

        isCommandProcessing = false;
    }

    // TODO: Implement cancel token to respond to button click (see MotorQueue POC -- will need to modify)
    public void CancelOperations()
    {
        cancellationTokenSource.Cancel();
        Console.WriteLine("Cancellation requested.");
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
    /// <param name="im"></param> Image model to print
    public void StartPrint(ImageModel im)
    {
        _state.Start(im);
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
    /// Cancel a print; image model will we destroyed
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
