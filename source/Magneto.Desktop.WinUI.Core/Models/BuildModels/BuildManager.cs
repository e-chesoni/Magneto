using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

        // Create a dance model
        danceModel = new DanceModel();

        // Start in the idle state
        TransitionTo(new IdleBuildManagerState(this));
    }

    #endregion

    #region Getters

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

        StepperMotor motor = motors.FirstOrDefault(motor => motor.GetMotorID() == motorId);
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
        imageModel.sliceStack = ImageHandler.SliceImage(imageModel);
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
