using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Magneto.Desktop.WinUI.Core.Contracts.Services;
using Magneto.Desktop.WinUI.Core.Models.Motors;
using Magneto.Desktop.WinUI.Core.Models.Print;
using static Magneto.Desktop.WinUI.Core.Models.Print.CommandQueueManager;

namespace Magneto.Desktop.WinUI.Core.Services;
public class MotorService : IMotorService
{
    private readonly CommandQueueManager _commandQueueManager;
    private StepperMotor? buildMotor;
    private StepperMotor? powderMotor;
    private StepperMotor? sweepMotor;

    /// <summary>
    /// Initializes the dictionary mapping motor names to their corresponding StepperMotor objects.
    /// This map facilitates the retrieval of motor objects based on their names.
    /// </summary>
    private Dictionary<string, StepperMotor?>? _motorTextMap;
    public MotorService(CommandQueueManager cqm)
    {
        _commandQueueManager = cqm;
    }

    #region Set Up
    public void ConfigurePortEventHandlers()
    {
        var msg = "Requesting port access for motor service.";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.DEBUG);
        MagnetoSerialConsole.LogAvailablePorts();

        // Get motor configurations
        var buildMotorConfig = MagnetoConfig.GetMotorByName("build");
        var sweepMotorConfig = MagnetoConfig.GetMotorByName("sweep");

        // Get motor ports, ensuring that the motor configurations are not null
        var buildPort = buildMotorConfig?.COMPort;
        var sweepPort = sweepMotorConfig?.COMPort;

        // Register event handlers on page
        foreach (SerialPort port in MagnetoSerialConsole.GetAvailablePorts())
        {
            if (port.PortName.Equals(buildPort, StringComparison.OrdinalIgnoreCase))
            {
                MagnetoSerialConsole.AddEventHandler(port);
                msg = $"Requesting addition of event handler for port {port.PortName}";
                MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);
            }
            else if (port.PortName.Equals(sweepPort, StringComparison.OrdinalIgnoreCase))
            {
                MagnetoSerialConsole.AddEventHandler(port);
                msg = $"Requesting addition of event handler for port {port.PortName}";
                MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);
            }
        }
    }
    public void HandleMotorInit(string motorName, StepperMotor motor, out StepperMotor motorField)
    {
        if (motor != null)
        {
            motorField = motor;
            var msg = $"Found motor in config with name {motor.GetMotorName()}. Setting this to {motorName} motor in test.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);
        }
        else
        {
            motorField = null;
            MagnetoLogger.Log($"Unable to find {motorName} motor", LogFactoryLogLevel.LogLevel.ERROR);
        }
    }
    public void IntializeMotors()
    {
        // Set up each motor individually using the passed-in parameters
        HandleMotorInit("powder", _commandQueueManager.GetPowderMotor(), out powderMotor);
        HandleMotorInit("build", _commandQueueManager.GetBuildMotor(), out buildMotor);
        HandleMotorInit("sweep", _commandQueueManager.GetSweepMotor(), out sweepMotor);

        // get maxSweepPosition
        //maxSweepPosition = _actuationManager.GetSweepMotor().GetMaxPos() - 2; // NOTE: Subtracting 2 from max position for tolerance...probs not needed in long run

        // Optionally, get the positions of the motors after setting them up
        // GetMotorPositions(); // TODO: Fix this if needed
    }
    public void InitializeMotorMap()
    {
        _motorTextMap = new Dictionary<string, StepperMotor?>
            {
                { "build", buildMotor },
                { "powder", powderMotor },
                { "sweep", sweepMotor }
            };
    }
    public void HandleStartUp()
    {
        ConfigurePortEventHandlers();
        // Initialize motor set up for test page
        IntializeMotors();
        // Initialize motor map to simplify coordinated calls below
        // Make sure this happens AFTER motor setup
        InitializeMotorMap();
    }
    public void Initialize()
    {
        HandleStartUp(); // This now runs AFTER everything is ready
    }
    #endregion

    #region Helpers
    /// <summary>
    /// Helper to get controller type given motor name
    /// </summary>
    /// <param name="motorName">Name of the motor for which to return the controller type</param>
    /// <returns>Controller type</returns>
    private ControllerType GetControllerTypeHelper(string motorName)
    {
        switch (motorName)
        {
            case "sweep":
                return ControllerType.SWEEP;
            default: return ControllerType.BUILD;
        }
    }
    #endregion

    #region Getters
    public CommandQueueManager GetActuationManager()
    {
        return _commandQueueManager;
    }
    public StepperMotor GetBuildMotor()
    {
        return buildMotor;
    }
    public StepperMotor GetPowderMotor()
    {
        return powderMotor;
    }
    public StepperMotor GetSweepMotor()
    {
        return sweepMotor;
    }
    public double GetMaxSweepPosition()
    {
        return sweepMotor.GetMaxPos();
    }
    public async Task<double> GetMotorPositionAsync(StepperMotor motor)
    {
        return await motor.GetPosAsync();
    }
    #endregion

    #region Checks
    public bool MotorsRunning()
    {
        // if queue is not empty, motors are running
        if (_commandQueueManager.MotorsRunning())
        {
            return true;
        }
        else
        {
            return false;
        }
    }
    #endregion

    #region Movement
    public async Task<int> LayerMove(double layerThickness, double supplyAmplifier)
    {
        //var powderAmplifier = 2.5; // Quan requested we change this from 4-2.5 to conserve powder
        var lowerBuildForSweepDist = 2;
        // safeguard max sweep position
        var maxSweepPos = _commandQueueManager.GetSweepMotor().GetMaxPos() - 2;

        if (_commandQueueManager != null)
        {
            // move build motor down for sweep
            await _commandQueueManager.AddCommand(GetControllerTypeHelper(buildMotor.GetMotorName()), buildMotor.GetAxis(), CommandType.RelativeMove, -lowerBuildForSweepDist);

            // home sweep motor
            await _commandQueueManager.AddCommand(GetControllerTypeHelper(sweepMotor.GetMotorName()), sweepMotor.GetAxis(), CommandType.AbsoluteMove, sweepMotor.GetHomePos());

            // move build motor back up to last mark height
            await _commandQueueManager.AddCommand(GetControllerTypeHelper(buildMotor.GetMotorName()), buildMotor.GetAxis(), CommandType.RelativeMove, lowerBuildForSweepDist);

            // move powder motor up by powder amp layer height (Prof. Tertuliano recommends powder motor moves 2-3x distance of build motor)
            await _commandQueueManager.AddCommand(GetControllerTypeHelper(powderMotor.GetMotorName()), powderMotor.GetAxis(), CommandType.RelativeMove, (supplyAmplifier * layerThickness));

            // move build motor down by layer height
            await _commandQueueManager.AddCommand(GetControllerTypeHelper(buildMotor.GetMotorName()), buildMotor.GetAxis(), CommandType.RelativeMove, -layerThickness);

            // apply material to build plate
            await _commandQueueManager.AddCommand(GetControllerTypeHelper(sweepMotor.GetMotorName()), sweepMotor.GetAxis(), CommandType.AbsoluteMove, maxSweepPos);

            // TEMPORARY SOLUTION: repeat last command to pad queue so we can use motors running check properly
            await _commandQueueManager.AddCommand(GetControllerTypeHelper(sweepMotor.GetMotorName()), sweepMotor.GetAxis(), CommandType.AbsoluteMove, maxSweepPos); // TODO: change to wait for end command
        }
        else
        {
            var msg = $"Actuation manager is null.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
            return 0;
        }
        return 1;
    }
    public async Task<int> MoveMotorAbs(StepperMotor motor, double target)
    {
        await _commandQueueManager.AddCommand(GetControllerTypeHelper(motor.GetMotorName()), motor.GetAxis(), CommandType.AbsoluteMove, target);
        return 1;
    }
    public async Task<int> MoveMotorRel(StepperMotor motor, double distance)
    {
        // NOTE: when called, you must await the return to get the integer value
        //       Otherwise returns some weird string
        MagnetoLogger.Log($"🚦called with distance {distance} on motor {motor.GetMotorName()}", LogFactoryLogLevel.LogLevel.WARN);
        MagnetoLogger.Log($"🔁distance {distance} to {motor.GetMotorName()} via {motor.GetAxis()}", LogFactoryLogLevel.LogLevel.WARN);
        await _commandQueueManager.AddCommand(GetControllerTypeHelper(motor.GetMotorName()), motor.GetAxis(), CommandType.RelativeMove, distance);
        return 1;
    }
    public async Task<int> HomeMotor(StepperMotor motor)
    {
        await _commandQueueManager.AddCommand(GetControllerTypeHelper(motor.GetMotorName()), motor.GetAxis(), CommandType.AbsoluteMove, motor.GetHomePos());
        return 1;
    }
    public async Task<int> StopMotorAndClearQueue(StepperMotor motor)
    {
        await _commandQueueManager.HandleStopRequest(motor);
        return 1;
    }

    public async Task<int> WaitUntilAtTargetAsync(StepperMotor motor, double targetPos)
    {
        await motor.WaitUntilAtTargetAsync(targetPos);
        return 1;
    }
    #endregion
}
