using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Magneto.Desktop.WinUI.Core.Contracts.Services;
using Magneto.Desktop.WinUI.Core.Models.Motors;
using Magneto.Desktop.WinUI.Core.Models.Print;
using static Magneto.Desktop.WinUI.Core.Models.Constants.MagnetoConstants;
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

    private readonly double SWEEP_CLEARANCE = 2; // mm

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

    #region Command Queue Helper
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

    private Controller GetControllerHelper(string motorName)
    {
        switch (motorName)
        {
            case "sweep":
                return Controller.SWEEP;
            default: return Controller.BUILD_AND_SUPPLY;
        }
    }
    #endregion

    public async Task ReadBuildMotorErrors() => await buildMotor.ReadErrors();
    public async Task ReadPowderMotorErrors() => await buildMotor.ReadErrors();
    public async Task ReadSweepMotorErrors() => await buildMotor.ReadErrors();
    public async Task ReadAndClearAllErrors()
    {
        await ReadBuildMotorErrors();
        await ReadPowderMotorErrors();
        await ReadSweepMotorErrors();
    }

    private string[] WriteAbsMoveProgram(StepperMotor motor, double target, bool moveUp) => motor.WriteAbsoluteMoveProgram(target, moveUp);
    public string[] WriteAbsoluteMoveProgramForBuildMotor(double target, bool moveUp) => WriteAbsMoveProgram(buildMotor, target, moveUp);
    public string[] WriteAbsoluteMoveProgramForPowderMotor(double target, bool moveUp) => WriteAbsMoveProgram(powderMotor, target, moveUp);
    public string[] WriteAbsoluteMoveProgramForSweepMotor(double target, bool moveUp) => WriteAbsMoveProgram(sweepMotor, target, moveUp);

    private string[] WriteRelativeMoveProgram(StepperMotor motor, double steps, bool moveUp) => motor.WriteMoveProgramHelper(steps, false, moveUp);
    public void SendProgram(string motorNameLower, string[] program)
    {
        switch (motorNameLower)
        {
            case "build":
                buildMotor.SendProgram(program);
                break;
            case "powder":
                buildMotor.SendProgram(program);
                break;
            case "sweep":
                buildMotor.SendProgram(program);
                break;
            default:
                MagnetoLogger.Log($"Unable to send program. Invalid motor name given: {motorNameLower}.", LogFactoryLogLevel.LogLevel.ERROR);
                break;
        }
    }

    public async Task StoreLastMoveAndSendProgram(string motorNameLower, ProgramNode programNode)
    {
        switch (motorNameLower)
        {
            case "build":
                buildMotor.SendProgram(programNode.program);
                break;
            case "powder":
                buildMotor.SendProgram(programNode.program);
                break;
            case "sweep":
                buildMotor.SendProgram(programNode.program);
                break;
            default:
                MagnetoLogger.Log($"Unable to send program. Invalid motor name given: {motorNameLower}.", LogFactoryLogLevel.LogLevel.ERROR);
                break;
        }
        await StoreLastRequestedMove(motorNameLower, programNode);
    }
    public string[] WriteRelativeMoveProgramForBuildMotor(double steps, bool moveUp) => WriteRelativeMoveProgram(buildMotor, steps, moveUp);
    public string[] WriteRelativeMoveProgramForPowderMotor(double steps, bool moveUp) => WriteRelativeMoveProgram(powderMotor, steps,moveUp);
    public string[] WriteRelativeMoveProgramForSweepMotor(double steps, bool moveUp) => WriteRelativeMoveProgram(sweepMotor, steps, moveUp);

    public async Task<bool> IsProgramRunningAsync(string motorNameLower)
    {
        switch (motorNameLower)
        {
            case "build":
                return await buildMotor.IsProgramRunningAsync();
            case "powder":
                return await powderMotor.IsProgramRunningAsync();
            case "sweep":
                return await sweepMotor.IsProgramRunningAsync();
            default:
                MagnetoLogger.Log($"Unable to check if program is running. Invalid motor name given: {motorNameLower}.", LogFactoryLogLevel.LogLevel.ERROR);
                return false;
        }
    }

    public void AddProgramFront(string[] program, Controller controller, int axis)
    {
        ProgramNode programNode = _commandQueueManager.CreateProgramNode(program, controller, axis);
        _commandQueueManager.AddProgramToFront(programNode);
    }

    public void AddBuildMotorProgramFront(string[] program)
    {
        AddProgramFront(program, Controller.BUILD_AND_SUPPLY, buildMotor.GetAxis());
    }
    public void AddPowderMotorProgramFront(string[] program)
    {
        AddProgramFront(program, Controller.BUILD_AND_SUPPLY, powderMotor.GetAxis());
    }
    public void AddSweepMotorProgramFront(string[] program)
    {
        AddProgramFront(program, Controller.SWEEP, sweepMotor.GetAxis());
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

    public void AddProgramLast(string[] program, Controller controller, int axis)
    {
        ProgramNode programNode = _commandQueueManager.CreateProgramNode(program, controller, axis);
        _commandQueueManager.AddProgramToBack(programNode);
    }
    public void AddBuildMotorProgramLast(string[] program)
    {
        AddProgramLast(program, Controller.BUILD_AND_SUPPLY, buildMotor.GetAxis());
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



    public void AddPowderMotorProgramLast(string[] program)
    {
        AddProgramLast(program, Controller.BUILD_AND_SUPPLY, powderMotor.GetAxis());
    }
    public void AddSweepMotorProgramLast(string[] program)
    {
        AddProgramLast(program, Controller.SWEEP, sweepMotor.GetAxis());
    }

    public int GetNumberOfPrograms()
    {
        return _commandQueueManager.programLinkedList.Count;
    }

    public ProgramNode? GetFirstProgramNode()
    {
        return _commandQueueManager.GetFirstProgramNode();
    }

    public ProgramNode? GetLastProgramNode()
    {
        return _commandQueueManager.GetLastProgramNode();
    }
    public void StopMotor(string motorNameLower)
    {
        switch (motorNameLower)
        {
            case "build":
                buildMotor.Stop();
                break;
            case "powder":
                powderMotor.Stop();
                break;
            case "sweep":
                sweepMotor.Stop();
                break;
            default:
                MagnetoLogger.Log($"Unable to check stop motor. Invalid motor name given: {motorNameLower}.", LogFactoryLogLevel.LogLevel.ERROR);
                return;
        }
    }

    public void StopAllMotors()
    {
        buildMotor.StopMotor();
        powderMotor.StopMotor();
        sweepMotor.StopMotor();
    }

    public bool IsProgramPaused() => _commandQueueManager.IsProgramPaused();
    public void PauseProgram()
    {
        _commandQueueManager.PauseProgram(); // updates boolean (should stop ProcessPrograms())
        // TODO: stop all motors on both controllers
        StopAllMotors();
    }

    public async Task ResumeProgramReading()
    {
        bool lastProgramFinished = false;
        // TODO: figure out if the last program finished
        // get the last program
        ProgramNode lastProgramNode = _commandQueueManager.GetLastProgramNodeRun();
        (var lastProgram, Controller controller, var axis) = _commandQueueManager.ExtractProgramNodeVariables(lastProgramNode);

        // get the move command from the last program

        // if it was relative, calculate the expected position // TODO: store target position on motor when move is requested?

        // did the motor reach the target?

        // TODO: request resume
        _commandQueueManager.ResetProgramList(lastProgramFinished);
        // execute process program
        await ProcessPrograms();
    }
    public async Task ExecuteLayerMove(double thickness, double amplifier, int numberOfLayers)
    {
        var clearance = SWEEP_CLEARANCE;
        var movePositive = true;

        // read and clear errors
        await ReadAndClearAllErrors();

        // move build and supply motors down so sweep motor can pass
        var lowerBuildClearance = WriteRelativeMoveProgramForBuildMotor(clearance, !movePositive);
        var lowerPowderClearance = WriteRelativeMoveProgramForPowderMotor(clearance, !movePositive);
        // home sweep motor
        var homeSweep = WriteAbsoluteMoveProgramForSweepMotor(sweepMotor.GetHomePos(), movePositive); // sweep moves home first
        // raise build and supply motors by clearance
        var raiseBuildClearance = WriteRelativeMoveProgramForBuildMotor(clearance, movePositive);
        var raisePowderClearance = WriteRelativeMoveProgramForPowderMotor(clearance, movePositive);
        // TODO: raise supply by (amplifier * thickness)
        // TODO: lower build by thickness
        var raiseSupplyLayer = WriteRelativeMoveProgramForPowderMotor((thickness * amplifier), movePositive);
        var lowerBuildLayer = WriteRelativeMoveProgramForBuildMotor(thickness, !movePositive);
        // spread powder
        var spreadPowder = WriteAbsoluteMoveProgramForSweepMotor(sweepMotor.GetMaxPos(), movePositive); // then to max position

        // Add commands to program list
        // lower clearance
        AddProgramLast(buildMotor.GetMotorName(), lowerBuildClearance);
        AddProgramLast(powderMotor.GetMotorName(), lowerPowderClearance);
        // home sweep
        AddProgramLast(sweepMotor.GetMotorName(), homeSweep);
        // raise clearance
        AddProgramLast(buildMotor.GetMotorName(), raiseBuildClearance);
        AddProgramLast(powderMotor.GetMotorName(), raisePowderClearance);
        // move motors for layer
        AddProgramLast(powderMotor.GetMotorName(), raiseSupplyLayer);
        AddProgramLast(buildMotor.GetMotorName(), lowerBuildLayer);
        // spread powder
        AddProgramLast(sweepMotor.GetMotorName(), spreadPowder);

        await ProcessPrograms();
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


    private async Task StoreLastRequestedMove(string motorNameLower, ProgramNode programNode)
    {
        double startingPosition;

        switch (motorNameLower)
        {
            case "build":
                startingPosition = await buildMotor.GetPositionAsync(2);
                break;
            case "powder":
                startingPosition = await powderMotor.GetPositionAsync(2);
                break;
            case "sweep":
                startingPosition = await sweepMotor.GetPositionAsync(2);
                break;
            default:
                MagnetoLogger.Log($"Invalid motor name: {motorNameLower}", LogFactoryLogLevel.LogLevel.ERROR);
                throw new ArgumentException($"Unknown motor: {motorNameLower}");
        }

        var target = CalculateTargetPosition(startingPosition, programNode);
        _commandQueueManager.SetLastMoveStartingPosition(startingPosition);
        _commandQueueManager.SetLastMoveTarget(target);
    }

    public (string[] program, Controller controller, int axis)? ExtractProgramNodeVariables(ProgramNode programNode) => _commandQueueManager.ExtractProgramNodeVariables(programNode);

    private async Task ProcessPrograms()
    {
        var buildMotorName = buildMotor.GetMotorName();
        var powderMotorName = powderMotor.GetMotorName();
        var sweepMotorName = sweepMotor.GetMotorName();

        // process queue
        while (GetNumberOfPrograms() > 0 && !IsProgramPaused())
        {
            var programNode = GetFirstProgramNode();
            if (programNode.HasValue)
            {
                var confirmedNode = programNode.Value;
                var (_, controller, axis) = ExtractProgramNodeVariables(confirmedNode).Value;
                if (controller == Controller.BUILD_AND_SUPPLY)
                {
                    if (axis == 1)
                    {
                        await StoreLastMoveAndSendProgram(buildMotorName, confirmedNode);
                        while (await IsProgramRunningAsync(buildMotorName))
                        {
                            await Task.Delay(100);
                        }
                    }
                    else // axis == 2
                    {
                        await StoreLastMoveAndSendProgram(powderMotorName, confirmedNode);
                        while (await IsProgramRunningAsync(powderMotorName))
                        {
                            await Task.Delay(100);
                        }
                    }
                }
                else // sweep controller
                {
                    await StoreLastMoveAndSendProgram(sweepMotorName, confirmedNode);
                    while (await IsProgramRunningAsync(sweepMotorName))
                    {
                        await Task.Delay(100);
                    }
                }
            }
        }
    }

    public int GetMotorAxis(string motorName)
    {
        switch (motorName)
        {
            case "build":
                return buildMotor.GetAxis();
            case "powder":
                return powderMotor.GetAxis();
            case "sweep":
                return sweepMotor.GetAxis();
            default:
                return 0;
        }
    }
    #region Getters
    public CommandQueueManager GetCommandQueueManager() => _commandQueueManager;

    public double GetMaxSweepPosition()
    {
        return sweepMotor.GetMaxPos();
    }

    public async Task<double> GetMotorPositionAsync(string motorNameLowerCase)
    {
        switch (motorNameLowerCase)
        {
            case "build":
                return await GetBuildMotorPositionAsync();
            case "powder":
                return await GetPowderMotorPositionAsync();
            case "sweep":
                return await GetSweepMotorPositionAsync();
            default:
                MagnetoLogger.Log($"Could not get {motorNameLowerCase} motor position. Invalid motor name given: {motorNameLowerCase}.", LogFactoryLogLevel.LogLevel.ERROR);
                return 0;
        }
    }
    public async Task<double> GetBuildMotorPositionAsync()
    {
        return await buildMotor.GetPositionAsync(2);
    }
    public async Task<double> GetPowderMotorPositionAsync()
    {
        return await powderMotor.GetPositionAsync(2);
    }
    public async Task<double> GetSweepMotorPositionAsync()
    {
        return await sweepMotor.GetPositionAsync(2);
    }
    public async Task<(int res, double position)> HandleGetPositionAsync(string motorNameLowerCase)
    {
        switch (motorNameLowerCase)
        {
            case "build":
                return (1, await GetBuildMotorPositionAsync());
            case "powder":
                return (1, await GetPowderMotorPositionAsync());
            case "sweep":
                return (1, await GetSweepMotorPositionAsync());
            default:
                var msg = $"Invalid motor name. Could not get {motorNameLowerCase} motor position.";
                MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
                return (0, 0.0);
        }
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
    public bool CheckMotorStopFlag(string motorNameLowerCase)
    {
        switch (motorNameLowerCase)
        {
            case "build":
                return buildMotor.STOP_MOVE_FLAG;
            case "powder":
                return powderMotor.STOP_MOVE_FLAG;
            case "sweep":
                return sweepMotor.STOP_MOVE_FLAG;
            default:
                MagnetoLogger.Log($"Could not check motor stop flag. Invalid motor name given: {motorNameLowerCase}.", LogFactoryLogLevel.LogLevel.ERROR);
                return true;
        }
    }
    #endregion

    #region Enablers
    public void EnableBuildMotor()
    {
        buildMotor.STOP_MOVE_FLAG = false;
    }
    public void EnablePowderMotor()
    {
        powderMotor.STOP_MOVE_FLAG = false;
    }
    public void EnableSweepMotor()
    {
        sweepMotor.STOP_MOVE_FLAG = false;
    }
    public void EnableMotors()
    {
        EnableBuildMotor();
        EnablePowderMotor();
        EnableSweepMotor();
    }
    #endregion

    #region Movement
    #region Absolute Move
    public async Task<int> MoveMotorAbs(string motorNameLowerCase, double target)
    {
        switch (motorNameLowerCase)
        {
            case "build":
                await MoveBuildMotorAbs(target);
                break;
            case "powder":
                await MovePowderMotorAbs(target);
                break;
            case "sweep":
                await MoveSweepMotorAbs(target);
                break;
            default:
                MagnetoLogger.Log($"Could not check motor stop flag. Invalid motor name given: {motorNameLowerCase}.", LogFactoryLogLevel.LogLevel.ERROR);
                return 0;
        }
        return 1;
    }
    private async Task<int> MoveMotorAbs(StepperMotor motor, double target)
    {
        await _commandQueueManager.AddCommand(GetControllerTypeHelper(motor.GetMotorName()), motor.GetAxis(), CommandType.AbsoluteMove, target);
        return 1;
    }
    public async Task<int> MoveBuildMotorAbs(double target) => await MoveMotorAbs(buildMotor, target);
    public async Task<int> MovePowderMotorAbs(double target) => await MoveMotorAbs(powderMotor, target);
    public async Task<int> MoveSweepMotorAbs(double target) => await MoveMotorAbs(sweepMotor, target);
    #endregion

    #region Relative Move
    public async Task<int> MoveMotorRel(string motorNameLowerCase, double distance)
    {
        switch (motorNameLowerCase)
        {
            case "build":
                await MoveBuildMotorRel(distance);
                break;
            case "powder":
                await MovePowderMotorRel(distance);
                break;
            case "sweep":
                await MoveSweepMotorRel(distance);
                break;
            default:
                MagnetoLogger.Log($"Could not check motor stop flag. Invalid motor name given: {motorNameLowerCase}.", LogFactoryLogLevel.LogLevel.ERROR);
                return 0;
        }
        return 1;
    }
    private async Task<int> MoveMotorRel(StepperMotor motor, double distance)
    {
        // NOTE: when called, you must await the return to get the integer value
        //       Otherwise returns some weird string
        MagnetoLogger.Log($"🚦called with distance {distance} on motor {motor.GetMotorName()}", LogFactoryLogLevel.LogLevel.WARN);
        MagnetoLogger.Log($"🔁distance {distance} to {motor.GetMotorName()} via {motor.GetAxis()}", LogFactoryLogLevel.LogLevel.WARN);
        await _commandQueueManager.AddCommand(GetControllerTypeHelper(motor.GetMotorName()), motor.GetAxis(), CommandType.RelativeMove, distance);
        return 1;
    }
    public async Task<int> MoveBuildMotorRel(double distance) => await MoveMotorRel(buildMotor, distance);
    public async Task<int> MovePowderMotorRel(double distance) => await MoveMotorRel(powderMotor, distance);
    public async Task<int> MoveSweepMotorRel(double distance) => await MoveMotorRel(sweepMotor, distance);
    #endregion

    #region Homing
    private async Task<int> HomeMotorHelper(StepperMotor motor)
    {
        await _commandQueueManager.AddCommand(GetControllerTypeHelper(motor.GetMotorName()), motor.GetAxis(), CommandType.AbsoluteMove, motor.GetHomePos());
        return 1;
    }
    public async Task<int> HomeMotor(string motorNameLowerCase)
    {
        switch (motorNameLowerCase)
        {
            case "build":
                await HomeMotorHelper(buildMotor);
                break;
            case "powder":
                await HomeMotorHelper(powderMotor);
                break;
            case "sweep":
                await HomeMotorHelper(sweepMotor);
                break;
            default:
                MagnetoLogger.Log($"Cannot wait until motor reaches position. Invalid motor name given: {motorNameLowerCase}.", LogFactoryLogLevel.LogLevel.ERROR);
                return 0;
        }
        return 1;
    }
    public async Task<int> HomeMotor(StepperMotor motor)
    {
        await _commandQueueManager.AddCommand(GetControllerTypeHelper(motor.GetMotorName()), motor.GetAxis(), CommandType.AbsoluteMove, motor.GetHomePos());
        return 1;
    }
    public async Task<int> HomeBuildMotor() => await HomeMotor(buildMotor);
    public async Task<int> HomePowderMotor() => await HomeMotor(powderMotor);
    public async Task<int> HomeSweepMotor() => await HomeMotor(sweepMotor);
    #endregion

    #region Stop and Clear Command Queue
    public async Task<int> StopMotorAndClearQueue(string motorNameLowerCase)
    {
        switch (motorNameLowerCase)
        {
            case "build":
                await _commandQueueManager.HandleStopRequest(buildMotor);
                break;
            case "powder":
                await _commandQueueManager.HandleStopRequest(powderMotor);
                break;
            case "sweep":
                await _commandQueueManager.HandleStopRequest(sweepMotor);
                break;
            default:
                MagnetoLogger.Log($"Invalid motor name given. Could not stop {motorNameLowerCase} motor.", LogFactoryLogLevel.LogLevel.ERROR);
                return 0;
                
        }
        return 1;
    }
    #endregion

    #region Wait Until Target Reached
    private async Task<int> WaitUntilAtTargetAsync(StepperMotor motor, double targetPos)
    {
        await motor.WaitUntilAtTargetAsync(targetPos);
        return 1;
    }
    public async Task<int> WaitUntilBuildReachesTargetAsync(double targetPos) => await WaitUntilAtTargetAsync(buildMotor, targetPos);
    public async Task<int> WaitUntilPowderReachesTargetAsync(double targetPos) => await WaitUntilAtTargetAsync(powderMotor, targetPos);
    public async Task<int> WaitUntilSweepReachesTargetAsync(double targetPos) => await WaitUntilAtTargetAsync(sweepMotor, targetPos);
    public async Task<int> WaitUntilMotorHomedAsync(string motorName)
    {
        switch (motorName)
        {
            case "build":
                await buildMotor.WaitUntilAtTargetAsync(buildMotor.GetHomePos());
                break;
            case "powder":
                await powderMotor.WaitUntilAtTargetAsync(powderMotor.GetHomePos());
                break;
            case "sweep":
                await sweepMotor.WaitUntilAtTargetAsync(sweepMotor.GetHomePos());
                break;
            default:
                MagnetoLogger.Log($"Cannot wait until motor reaches position. Invalid motor name given: {motorName}.", LogFactoryLogLevel.LogLevel.ERROR);
                return 0;
        }
        return 1;
    }
    #endregion
    #endregion
}
