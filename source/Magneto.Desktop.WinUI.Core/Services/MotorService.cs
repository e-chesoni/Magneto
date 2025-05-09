using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Magneto.Desktop.WinUI.Core.Contracts.Services;
using Magneto.Desktop.WinUI.Core.Models.Motors;
using Magneto.Desktop.WinUI.Core.Models.Print;
using Magneto.Desktop.WinUI.Core.Models.States.PrintStates;
using ZstdSharp.Unsafe;
using static Magneto.Desktop.WinUI.Core.Models.Constants.MagnetoConstants;
using static Magneto.Desktop.WinUI.Core.Models.Print.RoutineStateMachine;

namespace Magneto.Desktop.WinUI.Core.Services;
public class MotorService : IMotorService
{
    private readonly RoutineStateMachine _rsm;
    private StepperMotor? buildMotor;
    private StepperMotor? powderMotor;
    private StepperMotor? sweepMotor;

    /// <summary>
    /// Initializes the dictionary mapping motor names to their corresponding StepperMotor objects.
    /// This map facilitates the retrieval of motor objects based on their names.
    /// </summary>
    private Dictionary<string, StepperMotor?>? _motorTextMap;

    public MotorService(RoutineStateMachine rsm)
    {
        _rsm = rsm;
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
        HandleMotorInit("powder", _rsm.GetPowderMotor(), out powderMotor);
        HandleMotorInit("build", _rsm.GetBuildMotor(), out buildMotor);
        HandleMotorInit("sweep", _rsm.GetSweepMotor(), out sweepMotor);
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
    #endregion

    #region Getters
    public StepperMotor GetBuildMotor() => buildMotor;
    public StepperMotor GetPowderMotor() => powderMotor;
    public StepperMotor GetSweepMotor() => sweepMotor;
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

    public void AddProgramFront(string motorNameLower, string[] program) => _rsm.AddProgramFront(motorNameLower, program);

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

    // TODO: Update pause and resume methods to use _rsm states!
    #region Pause and Resume Program
    //public bool IsProgramPaused() => _rsm.IsProgramPaused(); // check rsm status
    public void EnableProgram() => _rsm.CANCELLATION_REQUESTED = false;
    public void PauseProgram() => _rsm.Pause(); // _rsm.Pause()
    //public Task ResumeProgramReading() => throw new NotImplementedException();
    public void ResumeProgram() => _rsm.Resume(); // set the pause requested flag to false
    #endregion

    #region Stop Motors
    // Stops should clear the program list
    public void CancelProgram() => _rsm.CANCELLATION_REQUESTED = true;
    //public bool IsProgramStopped() => _rsm.IsProgramStopped();
    public void StopMotorAndClearProgramList(string motorNameLower)
    {
        // clear the program list
        CancelProgram();
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
    public void StopAllMotorsClearProgramList()
    {
        //PauseProgram(); // TODO: test; may need this
        buildMotor.Stop();
        powderMotor.Stop();
        sweepMotor.Stop();
        // clear the program list
        CancelProgram();
    }
    public void EmergencyStop() => throw new NotImplementedException();
    #endregion

    #region Movement
    #region Absolute Move
    public async Task MoveBuildMotorAbsoluteProgram(double target)
    {
        MagnetoLogger.Log($"Received absolute move: {target}.", LogFactoryLogLevel.LogLevel.VERBOSE);
        var program = _rsm.WriteAbsoluteMoveProgramForBuildMotor(target);
        _rsm.AddProgramLast(buildMotor.GetMotorName(), program);
        await _rsm.Process();
    }
    public async Task MovePowderMotorAbsoluteProgram(double target)
    {
        MagnetoLogger.Log($"Received relative distance: {target}.", LogFactoryLogLevel.LogLevel.VERBOSE);
        var program = _rsm.WriteAbsoluteMoveProgramForPowderMotor(target);
        _rsm.AddProgramLast(powderMotor.GetMotorName(), program);
        await _rsm.Process();
    }
    public async Task MoveSweepMotorAbsoluteProgram(double target)
    {
        MagnetoLogger.Log($"Received relative distance: {target}.", LogFactoryLogLevel.LogLevel.VERBOSE);
        var program = _rsm.WriteAbsoluteMoveProgramForSweepMotor(target);
        _rsm.AddProgramLast(sweepMotor.GetMotorName(), program);
        await _rsm.Process();
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
        var program = _rsm.WriteRelativeMoveProgramForBuildMotor(distance, moveUp);
        _rsm.AddProgramLast(buildMotor.GetMotorName(), program);
        await _rsm.Process();
    }
    public async Task MovePowderMotorRelativeProgram(double distance, bool moveUp)
    {
        var program = _rsm.WriteRelativeMoveProgramForPowderMotor(distance, moveUp);
        _rsm.AddProgramLast(powderMotor.GetMotorName(), program);
        await _rsm.Process();
    }
    public async Task MoveSweepMotorRelativeProgram(double distance, bool moveUp)
    {
        var program = _rsm.WriteRelativeMoveProgramForSweepMotor(distance, moveUp);
        _rsm.AddProgramLast(sweepMotor.GetMotorName(), program);
        await _rsm.Process();
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
                program = _rsm.WriteAbsoluteMoveProgramForBuildMotor(buildMotor.GetHomePos());
                _rsm.AddProgramLast(buildMotor.GetMotorName(), program);
                break;
            case "powder":
                program = _rsm.WriteAbsoluteMoveProgramForPowderMotor(powderMotor.GetHomePos());
                _rsm.AddProgramLast(powderMotor.GetMotorName(), program);
                break;
            case "sweep":
                program = _rsm.WriteAbsoluteMoveProgramForSweepMotor(sweepMotor.GetHomePos());
                _rsm.AddProgramLast(sweepMotor.GetMotorName(), program);
                break;
            default:
                MagnetoLogger.Log($"Cannot wait until motor reaches position. Invalid motor name given: {motorNameLowerCase}.", LogFactoryLogLevel.LogLevel.ERROR);
                return 0;
        }
        await _rsm.Process();
        return 1;
    }

    public async Task HomeAllMotors()
    {
        var homeBuild = _rsm.WriteAbsoluteMoveProgramForBuildMotor(buildMotor.GetHomePos());
        var homePowder = _rsm.WriteAbsoluteMoveProgramForPowderMotor(powderMotor.GetHomePos());
        var homeSweep = _rsm.WriteAbsoluteMoveProgramForSweepMotor(sweepMotor.GetHomePos());
        _rsm.AddProgramLast(buildMotor.GetMotorName(), homeBuild);
        _rsm.AddProgramLast(powderMotor.GetMotorName(), homePowder);
        _rsm.AddProgramLast(sweepMotor.GetMotorName(), homeSweep);
        await _rsm.Process();
    }
    #endregion
    #endregion
}
