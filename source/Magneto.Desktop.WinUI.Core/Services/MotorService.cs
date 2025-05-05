using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Magneto.Desktop.WinUI.Core.Contracts.Services;
using Magneto.Desktop.WinUI.Core.Models.Motors;
using Magneto.Desktop.WinUI.Core.Models.Print;
using ZstdSharp.Unsafe;
using static Magneto.Desktop.WinUI.Core.Models.Constants.MagnetoConstants;
using static Magneto.Desktop.WinUI.Core.Models.Print.ProgramsManager;

namespace Magneto.Desktop.WinUI.Core.Services;
public class MotorService : IMotorService
{
    private readonly ProgramsManager _commandQueueManager;
    private StepperMotor? buildMotor;
    private StepperMotor? powderMotor;
    private StepperMotor? sweepMotor;

    /// <summary>
    /// Initializes the dictionary mapping motor names to their corresponding StepperMotor objects.
    /// This map facilitates the retrieval of motor objects based on their names.
    /// </summary>
    private Dictionary<string, StepperMotor?>? _motorTextMap;

    private readonly double SWEEP_CLEARANCE = 2; // mm

    public MotorService(ProgramsManager cqm)
    {
        _commandQueueManager = cqm;
    }


    #region Start Up
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

    #region Getters
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

    #region Position Getters
    public async Task<double> GetBuildMotorPositionAsync() => await buildMotor.GetPositionAsync(2);
    public async Task<double> GetPowderMotorPositionAsync() => await powderMotor.GetPositionAsync(2);
    public async Task<double> GetSweepMotorPositionAsync() => await sweepMotor.GetPositionAsync(2);
    public async Task<(int res, double position)> GetMotorPositionAsync(string motorNameLowerCase)
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
    public double GetMaxSweepPosition() => sweepMotor.GetMaxPos();
    #endregion
    #region Program Getters
    public int GetNumberOfPrograms() => _commandQueueManager.programLinkedList.Count;
    public ProgramNode? GetFirstProgramNode() => _commandQueueManager.GetFirstProgramNode();
    public ProgramNode? GetLastProgramNode() => _commandQueueManager.GetLastProgramNode();
    #endregion
    #endregion

    #region Read and Clear Motor Errors
    public async Task ReadBuildMotorErrors() => await buildMotor.ReadErrors();
    public async Task ReadPowderMotorErrors() => await buildMotor.ReadErrors();
    public async Task ReadSweepMotorErrors() => await buildMotor.ReadErrors();
    public async Task ReadAndClearAllErrors()
    {
        await ReadBuildMotorErrors();
        await ReadPowderMotorErrors();
        await ReadSweepMotorErrors();
    }
    #endregion

    #region Write Program
    private string[] WriteAbsoluteMoveProgram(StepperMotor motor, double target) => motor.CreateMoveProgramHelper(target, true);
    public string[] WriteAbsoluteMoveProgramForBuildMotor(double target) => WriteAbsoluteMoveProgram(buildMotor, target);
    public string[] WriteAbsoluteMoveProgramForPowderMotor(double target) => WriteAbsoluteMoveProgram(powderMotor, target);
    public string[] WriteAbsoluteMoveProgramForSweepMotor(double target) => WriteAbsoluteMoveProgram(sweepMotor, target);

    private string[] WriteRelativeMoveProgram(StepperMotor motor, double steps, bool moveUp) => motor.CreateMoveProgramHelper(steps, false, moveUp);
    public string[] WriteRelativeMoveProgramForBuildMotor(double steps, bool moveUp) => WriteRelativeMoveProgram(buildMotor, steps, moveUp);
    public string[] WriteRelativeMoveProgramForPowderMotor(double steps, bool moveUp) => WriteRelativeMoveProgram(powderMotor, steps, moveUp);
    public string[] WriteRelativeMoveProgramForSweepMotor(double steps, bool moveUp) => WriteRelativeMoveProgram(sweepMotor, steps, moveUp);
    #endregion

    #region Send and Store Program
    public void SendProgram(string motorNameLower, string[] program)
    {
        switch (motorNameLower)
        {
            case "build":
                buildMotor.WriteProgram(program);
                break;
            case "powder":
                powderMotor.WriteProgram(program);
                break;
            case "sweep":
                sweepMotor.WriteProgram(program);
                break;
            default:
                MagnetoLogger.Log($"Unable to send program. Invalid motor name given: {motorNameLower}.", LogFactoryLogLevel.LogLevel.ERROR);
                break;
        }
    }
    private async Task StoreLastMoveAndSendProgram(string motorNameLower, ProgramNode programNode)
    {
        switch (motorNameLower)
        {
            case "build":
                buildMotor.WriteProgram(programNode.program);
                break;
            case "powder":
                powderMotor.WriteProgram(programNode.program);
                break;
            case "sweep":
                sweepMotor.WriteProgram(programNode.program);
                break;
            default:
                MagnetoLogger.Log($"Unable to send program. Invalid motor name given: {motorNameLower}.", LogFactoryLogLevel.LogLevel.ERROR);
                break;
        }
        await StoreLastRequestedMove(motorNameLower, programNode);
    }
    #endregion

    #region Is Program Running
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
    #endregion

    #region Add Program Front
    private void AddProgramFrontHelper(string[] program, Controller controller, int axis)
    {
        ProgramNode programNode = _commandQueueManager.CreateProgramNode(program, controller, axis);
        _commandQueueManager.AddProgramToFront(programNode);
    }
    private void AddBuildMotorProgramFront(string[] program)
    {
        AddProgramFrontHelper(program, Controller.BUILD_AND_SUPPLY, buildMotor.GetAxis());
    }
    private void AddPowderMotorProgramFront(string[] program)
    {
        AddProgramFrontHelper(program, Controller.BUILD_AND_SUPPLY, powderMotor.GetAxis());
    }
    private void AddSweepMotorProgramFront(string[] program)
    {
        AddProgramFrontHelper(program, Controller.SWEEP, sweepMotor.GetAxis());
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

    #region Add Program Last
    private void AddProgramLastHelper(string[] program, Controller controller, int axis)
    {
        ProgramNode programNode = _commandQueueManager.CreateProgramNode(program, controller, axis);
        _commandQueueManager.AddProgramToBack(programNode);
    }
    private void AddBuildMotorProgramLast(string[] program)
    {
        AddProgramLastHelper(program, Controller.BUILD_AND_SUPPLY, buildMotor.GetAxis());
    }
    private void AddPowderMotorProgramLast(string[] program)
    {
        AddProgramLastHelper(program, Controller.BUILD_AND_SUPPLY, powderMotor.GetAxis());
    }
    private void AddSweepMotorProgramLast(string[] program)
    {
        AddProgramLastHelper(program, Controller.SWEEP, sweepMotor.GetAxis());
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

    #region Pause and Resume Program
    public bool IsProgramPaused() => _commandQueueManager.IsProgramPaused();
    public void PauseProgram()
    {
        _commandQueueManager.PauseProgram(); // updates boolean (should stop ProcessPrograms())
        //StopAllMotorsClearProgramList();
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
    private double CalculateTargetPosition(LastMove lastMove)
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
    public async Task ResumeProgramReading()
    {
        StepperMotor motor;
        // Figure out if the last program finished:
        // get the last program node and extract its variables
        LastMove lastMove = _commandQueueManager.GetLastMove();
        ProgramNode lastProgramNode = lastMove.programNode;
        (_, Controller controller, var axis) = _commandQueueManager.ExtractProgramNodeVariables(lastProgramNode);
        // use controller and axis to determine which motor command was called on
        if (controller == Controller.BUILD_AND_SUPPLY)
        {
            if (axis == buildMotor.GetAxis())
            {
                motor = buildMotor;
            }
            else
            {
                motor = powderMotor;
            }
        }
        else if (controller == Controller.SWEEP)
        {
            motor = sweepMotor;
        }
        else
        {
            MagnetoLogger.Log("Cannot resume reading program. No motor found.", LogFactoryLogLevel.LogLevel.ERROR);
            return;
        }
        // get the current position
        var currentPostion = await motor.GetPositionAsync(2);
        // calculate the targeted position
        var target = CalculateTargetPosition(lastMove);
        // if target is less than current position, moveUp = false
        var moveUp = target < currentPostion ? false : true;
        // if motor did not reach target, put absolute move command to move motor to target at the front of the program list
        if (currentPostion != target)
        {
            var absoluteProgram = WriteAbsoluteMoveProgram(motor, target);
            AddProgramFront(motor.GetMotorName(), absoluteProgram);
        }

        // resume executing process program
        await ProcessPrograms();
    }
    #endregion

    #region Stop Motors
    // Stops should clear the program list
    public void StopMotorAndClearProgramList(string motorNameLower)
    {
        PauseProgram();
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
        // clear the program list
        _commandQueueManager.programLinkedList.Clear();
    }
    public void StopAllMotorsClearProgramList()
    {
        PauseProgram();
        buildMotor.Stop();
        powderMotor.Stop();
        sweepMotor.Stop();
        // clear the program list
        _commandQueueManager.programLinkedList.Clear();
    }
    public void EmergencyStop()
    {

    }
    #endregion

    #region Multi-Motor Move Methods
    public (string[] program, Controller controller, int axis)? ExtractProgramNodeVariables(ProgramNode programNode) => _commandQueueManager.ExtractProgramNodeVariables(programNode);
    public async Task ExecuteLayerMove(double thickness, double amplifier)
    {
        var clearance = SWEEP_CLEARANCE;
        var movePositive = true;

        // read and clear errors
        await ReadAndClearAllErrors();

        // move build and supply motors down so sweep motor can pass
        var lowerBuildClearance = WriteRelativeMoveProgramForBuildMotor(clearance, !movePositive);
        var lowerPowderClearance = WriteRelativeMoveProgramForPowderMotor(clearance, !movePositive);
        // home sweep motor
        var homeSweep = WriteAbsoluteMoveProgramForSweepMotor(sweepMotor.GetHomePos()); // sweep moves home first
        // raise build and supply motors by clearance
        var raiseBuildClearance = WriteRelativeMoveProgramForBuildMotor(clearance, movePositive);
        var raisePowderClearance = WriteRelativeMoveProgramForPowderMotor(clearance, movePositive);
        // TODO: raise supply by (amplifier * thickness)
        // TODO: lower build by thickness
        var raiseSupplyLayer = WriteRelativeMoveProgramForPowderMotor((thickness * amplifier), movePositive);
        var lowerBuildLayer = WriteRelativeMoveProgramForBuildMotor(thickness, !movePositive);
        // spread powder
        var spreadPowder = WriteAbsoluteMoveProgramForSweepMotor(sweepMotor.GetMaxPos()); // then to max position

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
    private async Task ProcessPrograms()
    {
        var buildMotorName = buildMotor.GetMotorName();
        var powderMotorName = powderMotor.GetMotorName();
        var sweepMotorName = sweepMotor.GetMotorName();

        // process queue
        while (GetNumberOfPrograms() > 0)
        {
            // 🛑 Recheck pause flag before starting next command
            if (IsProgramPaused())
            {
                MagnetoLogger.Log("⏸ Program paused. Halting execution.", LogFactoryLogLevel.LogLevel.WARN);
                return;
            }

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
                        //SendProgram(buildMotorName, program);
                        while (await IsProgramRunningAsync(buildMotorName))
                        {
                            await Task.Delay(100);
                        }
                    }
                    else // axis == 2
                    {
                        await StoreLastMoveAndSendProgram(powderMotorName, confirmedNode);
                        //SendProgram(powderMotorName, program);
                        while (await IsProgramRunningAsync(powderMotorName))
                        {
                            await Task.Delay(100);
                        }
                    }
                }
                else // sweep controller
                {
                    await StoreLastMoveAndSendProgram(sweepMotorName, confirmedNode);
                    //SendProgram(sweepMotorName, program);
                    while (await IsProgramRunningAsync(sweepMotorName))
                    {
                        await Task.Delay(100);
                    }
                }
            }
        }
    }
    #endregion

    #region Movement
    #region Absolute Move
    public async Task MoveBuildMotorAbsoluteProgram(double target)
    {
        MagnetoLogger.Log($"Received absolute move: {target}.", LogFactoryLogLevel.LogLevel.VERBOSE);
        var program = WriteAbsoluteMoveProgramForBuildMotor(target);
        AddProgramLast(buildMotor.GetMotorName(), program);
        await ProcessPrograms();
    }
    public async Task MovePowderMotorAbsoluteProgram(double target)
    {
        MagnetoLogger.Log($"Received relative distance: {target}.", LogFactoryLogLevel.LogLevel.VERBOSE);
        var program = WriteAbsoluteMoveProgramForPowderMotor(target);
        AddProgramLast(powderMotor.GetMotorName(), program);
        await ProcessPrograms();
    }
    public async Task MoveSweepMotorAbsoluteProgram(double target)
    {
        MagnetoLogger.Log($"Received relative distance: {target}.", LogFactoryLogLevel.LogLevel.VERBOSE);
        var program = WriteAbsoluteMoveProgramForSweepMotor(target);
        AddProgramLast(sweepMotor.GetMotorName(), program);
        await ProcessPrograms();
    }

    public async Task MoveMotorAbsoluteProgram(string motorNameLower, double target)
    {
        switch (motorNameLower)
        {
            case "build":
                await MoveBuildMotorAbsoluteProgram(target);
                break;
            case "powder":
                await MovePowderMotorAbsoluteProgram(target);
                break;
            case "sweep":
                await MoveSweepMotorAbsoluteProgram(target);
                break;
            default:
                MagnetoLogger.Log($"Could not check motor stop flag. Invalid motor name given: {motorNameLower}.", LogFactoryLogLevel.LogLevel.ERROR);
                return;
        }
        return;
    }
    #endregion

    #region Relative Move

    public async Task MoveBuildMotorRelativeProgram(double distance, bool moveUp)
    {
        MagnetoLogger.Log($"🚦Received relative 🔁distance: {distance}.", LogFactoryLogLevel.LogLevel.VERBOSE);
        var program = WriteRelativeMoveProgramForBuildMotor(distance, moveUp);
        AddProgramLast(buildMotor.GetMotorName(), program);
        await ProcessPrograms();
    }
    public async Task MovePowderMotorRelativeProgram(double distance, bool moveUp)
    {
        var program = WriteRelativeMoveProgramForPowderMotor(distance, moveUp);
        AddProgramLast(powderMotor.GetMotorName(), program);
        await ProcessPrograms();
    }
    public async Task MoveSweepMotorRelativeProgram(double distance, bool moveUp)
    {
        var program = WriteRelativeMoveProgramForSweepMotor(distance, moveUp);
        AddProgramLast(sweepMotor.GetMotorName(), program);
        await ProcessPrograms();
    }

    public async Task MoveMotorRelativeProgram(string motorNameLower, double distance, bool moveUp)
    {
        switch (motorNameLower)
        {
            case "build":
                await MoveBuildMotorRelativeProgram(distance, moveUp);
                break;
            case "powder":
                await MovePowderMotorRelativeProgram(distance, moveUp);
                break;
            case "sweep":
                await MoveSweepMotorRelativeProgram(distance, moveUp);
                break;
            default:
                MagnetoLogger.Log($"Could not check motor stop flag. Invalid motor name given: {motorNameLower}.", LogFactoryLogLevel.LogLevel.ERROR);
                return;
        }
        return;
    }
    #endregion

    #region Homing
    public async Task<int> HomeMotorProgram(string motorNameLowerCase)
    {
        string[] program;
        switch (motorNameLowerCase)
        {
            case "build":
                program = WriteAbsoluteMoveProgramForBuildMotor(buildMotor.GetHomePos());
                AddProgramLast(buildMotor.GetMotorName(), program);
                break;
            case "powder":
                program = WriteAbsoluteMoveProgramForPowderMotor(powderMotor.GetHomePos());
                AddProgramLast(powderMotor.GetMotorName(), program);
                break;
            case "sweep":
                program = WriteAbsoluteMoveProgramForSweepMotor(sweepMotor.GetHomePos());
                AddProgramLast(sweepMotor.GetMotorName(), program);
                break;
            default:
                MagnetoLogger.Log($"Cannot wait until motor reaches position. Invalid motor name given: {motorNameLowerCase}.", LogFactoryLogLevel.LogLevel.ERROR);
                return 0;
        }
        await ProcessPrograms();
        return 1;
    }

    public async Task HomeAllMotors()
    {
        var homeBuild = WriteAbsoluteMoveProgramForBuildMotor(buildMotor.GetHomePos());
        var homePowder = WriteAbsoluteMoveProgramForPowderMotor(powderMotor.GetHomePos());
        var homeSweep = WriteAbsoluteMoveProgramForSweepMotor(sweepMotor.GetHomePos());
        AddProgramLast(buildMotor.GetMotorName(), homeBuild);
        AddProgramLast(powderMotor.GetMotorName(), homePowder);
        AddProgramLast(sweepMotor.GetMotorName(), homeSweep);
        await ProcessPrograms();
    }
    #endregion

    /*
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
    */
    #endregion
}
