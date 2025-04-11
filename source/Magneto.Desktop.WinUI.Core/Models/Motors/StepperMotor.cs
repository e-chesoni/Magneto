using Magneto.Desktop.WinUI.Core.Contracts.Services;
using Microsoft.VisualBasic;
using System.Text.RegularExpressions;
using System.Diagnostics;
using Newtonsoft.Json.Linq;
using Magneto.Desktop.WinUI.Core.Contracts;

namespace Magneto.Desktop.WinUI.Core.Models.Motors;

/// <summary>
/// Represents a stepper motor and encapsulates its functionality and control mechanisms.
/// This class provides methods for initializing the motor, setting its parameters, and controlling
/// its movement to precise positions. It also includes functionalities for monitoring the motor's
/// status, handling errors, and executing specific motion commands tailored to the requirements
/// of stepper motor applications. The class is designed to be used in systems where accurate
/// positioning and speed control of a motor are critical.
/// </summary>
public class StepperMotor : IStepperMotor
{
    #region Private Variables

    private readonly double _tolerance = 0.05;

    /// <summary>
    /// Motor ID (the number of the COM port followed by the axis the motor is attached to)
    /// </summary>
    private int _motorId { get; set; }

    /// <summary>
    /// Name of the motor (either build, powder or sweep) set in Magneto config
    /// Used to distribute tasks through out app
    /// </summary>
    private string _motorName { get; set; }

    /// <summary>
    /// Port motor is attached to
    /// </summary>
    private string _motorPort { get; set; }

    /// <summary>
    ///  The axis that the motor is attached to
    /// </summary>
    private int _motorAxis { get; set; }

    /// <summary>
    /// Calculated motor position
    /// </summary>
    double _calculatedPos;

    /// <summary>
    /// Motor status
    /// </summary>
    private MotorStatus _status;

    /// <summary>
    /// Home position of the motor
    /// </summary>
    private double _homePos { get; set; }

    /// <summary>
    /// Max reach of the motor set in Magneto config
    /// </summary>
    private double _maxPos { get; set; }

    /// <summary>
    /// Minimum position motor is allowed to reach set in Magneto config
    /// </summary>
    private double _minPos { get; set; }

    /// <summary>
    /// Motor velocity noted in Magneto config
    /// </summary>
    private double _motorVelocity { get; set; }

    /// <summary>
    /// Current motor position
    /// </summary>
    private double _currentPos { get; set; }

    /// <summary>
    /// Boolean used to determine whether motor is currently moving
    /// Used for position tracking
    /// </summary>
    private bool _motorMoving { get; set; }

    #endregion

    #region Public Variables

    /// <summary>
    /// Possible motor statuses
    /// </summary>
    public enum MotorStatus : short
    {
        Error = -2,
        Bad = -1,
        Good = 0
    }

    /// <summary>
    /// Enumerates various error codes for motor operations
    /// </summary>
    public enum MotorError : short
    {
        ReceiveBufferOverrun = 10,
        MotorDisabled = 11,
        NoEncoderDetected = 12,
        IndexNotFound = 13,
        HomeRequiresEncoder = 14,
        MoveLimitRequiresEncoder = 15,
        CommandIsReadOnly = 20,
        OneReadOperationPerLine = 21,
        TooManyCommandsOnLine = 22,
        LineCharacterLimitExceeded = 23,
        MissingAxisNumber = 24,
        MalformedCommand = 25,
        InvalidCommand = 26,
        GlobalReadOperationRequest = 27,
        InvalidParameterType = 28,
        InvalidCharacterParameter = 29,
        CommandCannotBeUsedInGlobalContext = 30,
        ParameterOutOfBounds = 31,
        IncorrectJogVelocityRequest = 32,
        NotInJogMode = 33,
        TraceAlreadyInProgress = 34,
        TraceDidNotComplete = 35,
        CommandCannotBeExecutedDuringMotion = 36,
        MoveOutsideSoftLimits = 37,
        ReadNotAvailableForThisCommand = 38,
        ProgramNumberOutOfRange = 39,
        ProgramSizeLimitExceeded = 40,
        ProgramFailedToRecord = 41,
        EndCommandMustBeOnItsOwnLine = 42,
        FailedToReadProgram = 43,
        CommandOnlyValidWithinProgram = 44,
        ProgramAlreadyExists = 45,
        ProgramDoesntExist = 46,
        ReadOperationsNotAllowedInsideProgram = 47,
        CommandOperationsNotAllowedWhileProgramInProgress = 48,
        LimitActivated = 50,
        EndOfTravelLimit = 51,
        HomeInProgress = 52,
        IOFunctionAlreadyInUse = 53,
        LimitsAreNotConfiguredProperly = 55,
        CommandNotAvailableInThisVersion = 80,
        AnalogEncoderNotAvailableInThisVersion = 81
    }

    public enum ExecStatus
    {
        Success = 0,
        Failure = -1,
    }

    public bool STOP_MOVE_FLAG;
    #endregion

    #region Constructor

    /// <summary>
    /// StepperMotor constructor
    /// </summary>
    /// <param name="motorName"></param> The axis that the motor is attached to
    public StepperMotor(string motorName, string portName, int axis, double maxPos, double minPos, double homePos, double vel)
    {
        // Validate parameters
        if (string.IsNullOrWhiteSpace(motorName) || string.IsNullOrWhiteSpace(portName))
        {
            throw new ArgumentException("Motor name and port name cannot be null or empty.");
        }

        // Initiate motor definitions
        _motorName = motorName;
        _motorPort = portName;
        _motorAxis = axis;
        _maxPos = maxPos;
        _minPos = minPos;
        _homePos = homePos;
        _motorVelocity = vel;

        // Initiate flags
        STOP_MOVE_FLAG = false;

        // Extract ID from portName using regular expression
        var idString = Regex.Match(portName, @"\d+").Value + axis;
        if (int.TryParse(idString, out var motorId))
        {
            _motorId = motorId;
            MagnetoLogger.Log($"Assigning ID: {_motorId} to motor", LogFactoryLogLevel.LogLevel.SUCCESS);
        }
        else
        {
            _motorId = 0; // Consider if a default value or an exception is more appropriate
            MagnetoLogger.Log("Conversion to integer for motor ID failed.", LogFactoryLogLevel.LogLevel.ERROR);
        }

        MagnetoLogger.Log($"Created Stepper Motor '{_motorName}' on port '{_motorPort}', axis {_motorAxis}, max position: {_maxPos}, min position: {_minPos}, home position: {_homePos}, velocity: {_motorVelocity}", LogFactoryLogLevel.LogLevel.VERBOSE);
    }

    #endregion

    #region Basic Getters and Setters

    /// <summary>
    /// Get the motor name
    /// </summary>
    /// <returns></returns>
    public string GetMotorName()
    {
        return _motorName;
    }

    /// <summary>
    /// Get the motor id
    /// </summary>
    /// <returns></returns>
    public int GetID()
    {
        return _motorId;
    }

    /// <summary>
    /// Get the port assigned to the motor
    /// </summary>
    /// <returns></returns>
    public string GetPortName()
    {
        return _motorPort; 
    }

    /// <summary>
    /// Get the motor axis (initially assigned during startup in Magneto Config)
    /// </summary>
    /// <returns></returns>
    public int GetAxis()
    {
        return _motorAxis;
    }

    /// <summary>
    /// Get the home position of the motor (initially defined in Magneto Config at startup)
    /// </summary>
    /// <returns></returns>
    public double GetHomePos()
    {
        return _homePos;
    }

    /// <summary>
    /// Get the max position of the motor (initially defined in Magneto Config at startup)
    /// </summary>
    /// <returns></returns>
    public double GetMaxPos()
    {
        return _maxPos;
    }

    /// <summary>
    /// Get the min position of the motor (initially defined in Magneto Config at startup)
    /// </summary>
    /// <returns></returns>
    public double GetMinPos()
    {
        return _minPos;
    }

    /// <summary>
    /// Get the max velocity of the motor (initially defined in Magneto Config at startup)
    /// </summary>
    /// <returns></returns>
    public double GetVelocity()
    {
        return _motorVelocity;
    }

    /// <summary>
    /// Get the current position of the motor
    /// </summary>
    /// <returns></returns>
    public double GetCurrentPos()
    {
        return _currentPos;
    }

    /// <summary>
    /// Get current status of motor
    /// </summary>
    /// <param name="newStatus"></param>
    /// <returns></returns> Returns the status of the motor
    public MotorStatus GetStatus()
    {
        return _status;
    }

    /// <summary>
    /// Set the motor name
    /// </summary>
    /// <param name="name"></param>
    public void SetMotorName(string name)
    {
        _motorName = name;
    }

    /// <summary>
    /// Set the motor com port
    /// </summary>
    /// <param name="portName"></param>
    public void SetPortName(string portName)
    {
        _motorPort = portName;
    }

    /// <summary>
    /// Set the motor axis
    /// </summary>
    /// <param name="axis"></param>
    public void SetAxis(int axis)
    {
        _motorAxis = axis;
    }

    /// <summary>
    /// Set the motor home position
    /// </summary>
    /// <param name="pos"></param>
    public void SetHomePos(double pos)
    {
        _homePos = pos;
    }

    /// <summary>
    /// Set the motor's max position
    /// </summary>
    /// <param name="pos"></param>
    public void SetMaxPos(double pos)
    {
        _maxPos = pos;
    }

    /// <summary>
    /// Set the motor's min position
    /// </summary>
    /// <param name="pos"></param>
    public void SetMinPos(double pos)
    {
        _minPos = pos;
    }

    /// <summary>
    /// Set the motor velocity
    /// </summary>
    /// <param name="vel"></param>
    public void SetVelocity(double vel)
    {
        _motorVelocity = vel;
    }

    #endregion

    #region Movement Methods

    /// NOTE: The syntax to move a motor in an absolute position is:
    /// nMVAx
    /// Where n is the axis
    /// And x is the number of mm to move

    /// <summary>
    /// Move motor to an absolute position
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns> Returns completed task when finished
    public async Task MoveMotorAbsAsync(double value) // here, value is the position we want to reach
    {
        // Get the motors initial position
        var initialPos = await GetPosAsync();

        // Get the desired position
        // This is unnecessary, but it makes it easier to compare to the relative move command (below)
        // TODO: Remove unnecessary code when project moves to production
        var desiredPos = value;

        // Log initial and desired position
        var msg = $"Initial position of {_motorName} motor: {initialPos}. Desired relative position: {desiredPos}";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.WARN);

        var moveCmd = "MVA";

        if (ValidateDesiredPosition(desiredPos) == ExecStatus.Success)
        {
            // Check if serial port assigned to motor is open
            if (MagnetoSerialConsole.OpenSerialPort(_motorPort))
            {
                // If port is open:
                // Create move command
                var motorCmd = $"{_motorAxis}{moveCmd}{value}";

                // Send move command to motor port
                MagnetoSerialConsole.SerialWrite(_motorPort, motorCmd);
                MagnetoLogger.Log($"Moving {_motorName} motor on axis {_motorAxis}. Command Sent: {motorCmd}", LogFactoryLogLevel.LogLevel.VERBOSE);

                await HandleCheckPosition(desiredPos);
            }
            else
            {
                // If port is closed, log error and exit method
                MagnetoLogger.Log("Port Closed.", LogFactoryLogLevel.LogLevel.ERROR);
                return;
            }
        }
        else
        {
            MagnetoLogger.Log("Invalid position requested", LogFactoryLogLevel.LogLevel.ERROR);
            return;
        }

        //HandleSerialCommunication(desiredPos, value, true);
    }

    private ExecStatus ValidateDesiredPosition(double desiredPos)
    {
        // Check if desired position is out of range
        if (desiredPos < _minPos || desiredPos > _maxPos)
        {
            // If it is, log error and exit method
            MagnetoLogger.Log($"Invalid position: {desiredPos} for {_motorName}. _minPos is {_minPos} and _maxPos is {_maxPos}. Aborting motor move operation.", LogFactoryLogLevel.LogLevel.ERROR);
            return ExecStatus.Failure;
        }
        else
        {
            return ExecStatus.Success;
        }
    }

    private async Task HandleCheckPosition(double desiredPos)
    {
        while (!STOP_MOVE_FLAG) // If motor stop requested, exit this loop that continues to check position until it is reached
        {
            // Asynchronously wait until the desired position is reached
            await CheckPosAsync(desiredPos);
        }

        // reset stop move flag
        STOP_MOVE_FLAG = false;

        return;
    }


    // TODO: Figure out why encapsulation of serial communication here causes inescapable loop...
    private async Task HandleSerialCommunication(double desiredPos, double value, bool isAbsolute)
    {
        var moveCmd = "";

        if (isAbsolute)
        {
            moveCmd = "MVA";
        }
        else
        {
            moveCmd = "MVR";
        }

        // Check if desired position is out of range
        if (ValidateDesiredPosition(desiredPos) == ExecStatus.Success)
        {
            // Check if serial port assigned to motor is open
            if (MagnetoSerialConsole.OpenSerialPort(_motorPort))
            {
                // If port is open:
                // Create move command
                var motorCmd = $"{_motorAxis}{moveCmd}{value}";

                // Send move command to motor port
                MagnetoSerialConsole.SerialWrite(_motorPort, motorCmd);
                MagnetoLogger.Log($"Moving {_motorName} motor on axis {_motorAxis}. Command Sent: {motorCmd}", LogFactoryLogLevel.LogLevel.VERBOSE);

                await HandleCheckPosition(desiredPos);
            }
            else
            {
                // If port is closed, log error and exit method
                MagnetoLogger.Log("Port Closed.", LogFactoryLogLevel.LogLevel.ERROR);
                return;
            }
        }
        else
        {
            // If it is, log error and exit method
            MagnetoLogger.Log($"Invalid position: {desiredPos} for {_motorName}. _minPos is {_minPos} and _maxPos is {_maxPos}. Aborting motor move operation.", LogFactoryLogLevel.LogLevel.ERROR);
            return;
        }
    }

    /// NOTE: The syntax to move a motor in relative to a position is:
    /// nMVRx
    /// Where n is the axis
    /// And x is the number of mm to move

    /// <summary>
    /// Move motor relative to current position
    /// </summary>
    /// <param name="value"></param>
    /// <returns></returns> Returns -1 if move command fails, 0 if move command is successful
    public async Task MoveMotorRelAsync(double value) // here, value is the steps to take from the current position
    {
        // Get the motors initial position
        var initialPos = await GetPosAsync();

        // Calculate desired position
        var desiredPos = initialPos + value;

        var moveCmd = "MVR";

        // Log initial and desired position
        var msg = $"Initial position of {_motorName} motor: {initialPos}. Desired relative position: {desiredPos}";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.WARN);

        // Check if desired position is out of range
        if (ValidateDesiredPosition(desiredPos) == ExecStatus.Success)
        {
            // Check if serial port assigned to motor is open
            if (MagnetoSerialConsole.OpenSerialPort(_motorPort))
            {
                // If port is open:
                // Create move command
                var motorCmd = $"{_motorAxis}{moveCmd}{value}";

                // Send move command to motor port
                MagnetoSerialConsole.SerialWrite(_motorPort, motorCmd);
                MagnetoLogger.Log($"Moving {_motorName} motor on axis {_motorAxis} {value}mm relative to current position. Command Sent: {motorCmd}", LogFactoryLogLevel.LogLevel.VERBOSE);

                await HandleCheckPosition(desiredPos);
            }
            else
            {
                // If port is closed, log error and exit method
                MagnetoLogger.Log("Port Closed.", LogFactoryLogLevel.LogLevel.ERROR);
                return;
            }
        }
        else
        {
            // If it is, log error and exit method
            MagnetoLogger.Log($"Invalid position: {desiredPos} for {_motorName}. _minPos is {_minPos} and _maxPos is {_maxPos}. Aborting motor move operation.", LogFactoryLogLevel.LogLevel.ERROR);
            return;
        }

        //HandleSerialCommunication(desiredPos, value, false);

    }

    /// <summary>
    /// EMERGENCY STOP: Stop motor
    /// </summary>
    /// <returns></returns> Returns -1 if stop command fails, 0 if move command is successful
    public async Task StopMotor()
    {
        // Check if serial port assigned to motor is open
        if (MagnetoSerialConsole.OpenSerialPort(_motorPort))
        {
            // If port is open:
            // Create move command
            var motorCmd = $"{_motorAxis}STP";

            // Send move command to motor port
            MagnetoSerialConsole.SerialWrite(_motorPort, motorCmd);
            MagnetoLogger.Log($"Stopping {_motorName} motor on axis {_motorAxis}. Command Sent: {motorCmd}", LogFactoryLogLevel.LogLevel.VERBOSE);
            STOP_MOVE_FLAG = true;
        }
        else
        {
            // If port is closed, log error and exit method
            MagnetoLogger.Log("Port Closed.", LogFactoryLogLevel.LogLevel.ERROR);
            return;
        }
    }

    /// <summary>
    /// Initiates the process of moving the motor to its home position as defined in the Magneto configuration.
    /// Logs the action and performs the movement asynchronously.
    /// </summary>
    /// <returns>
    /// A Task that represents the asynchronous operation. 
    /// The task result is -1 if the home command fails, and 0 if the home command is successful.
    /// </returns>
    public async Task<int> HomeMotor()
    {
        MagnetoLogger.Log("Homing motor...", LogFactoryLogLevel.LogLevel.VERBOSE);
        await MoveMotorAbsAsync(GetHomePos());
        _calculatedPos = GetHomePos();

        // TODO: You need to implement the logic to determine if the command has failed or succeeded
        // and return -1 or 0 accordingly. This is just a placeholder.
        return 0; // Or -1 if failed
    }

    public Task<int> WaitForStop()
    {
        // TODO: implement wait for stop
        // 2WST = make axis 2 wait for stop before executing next command
        return Task.FromResult(0);
    }

    #endregion

    #region Movement Helpers
    /// <summary>
    /// Get current motor position
    /// </summary>
    /// <returns></returns> Returns -1 if request for position fails, otherwise returns motor position
    public async Task<double> GetPosAsync()
    {
        MagnetoLogger.Log($"Getting {_motorName} motor position...", LogFactoryLogLevel.LogLevel.VERBOSE);

        // TODO: Test is this is still needed with newly implemented AddCommand method (uses locking)
        MagnetoSerialConsole.ClearTermRead();
        var positionRequest = $"{_motorAxis}POS?";

        if (!MagnetoSerialConsole.OpenSerialPort(_motorPort))
        {
            MagnetoLogger.Log("Port Closed.", LogFactoryLogLevel.LogLevel.ERROR);
            return -1.0;
        }

        MagnetoSerialConsole.SerialWrite(_motorPort, positionRequest);
        MagnetoLogger.Log($"Position request sent: {positionRequest}", LogFactoryLogLevel.LogLevel.SUCCESS);

        var timeout = TimeSpan.FromSeconds(5);
        var stopwatch = Stopwatch.StartNew();
        string posString;

        do
        {
            if (stopwatch.Elapsed > timeout)
            {
                MagnetoLogger.Log("Timeout reached while waiting for position data.", LogFactoryLogLevel.LogLevel.ERROR);
                return -1.0;
            }

            posString = MagnetoSerialConsole.GetTermRead();
            await Task.Delay(100); // Non-blocking delay
        }
        while (string.IsNullOrEmpty(posString) || !posString.StartsWith("#"));

        var posDouble = ExtractDoubleFromString(posString);
        MagnetoLogger.Log($"Position as double: {posDouble}", LogFactoryLogLevel.LogLevel.VERBOSE);

        return posDouble;
    }


    // TODO: Update methods to use asynchronous position check
    public async Task<bool> CheckPosAsync(double desiredPos)
    {
        MagnetoLogger.Log("Checking motor position.", LogFactoryLogLevel.LogLevel.VERBOSE);

        const int maxAttempts = 10000; // Can modify max wait limit; This waits for 10000*100 ms (task delay below) ~ 16.7 min
        var attempt = 0;

        while (attempt < maxAttempts)
        {
            _currentPos = await GetPosAsync();
            MagnetoLogger.Log($"Current Position: {_currentPos}, Desired Position: {desiredPos}", LogFactoryLogLevel.LogLevel.VERBOSE);

            if (Math.Abs(_currentPos - desiredPos) <= _tolerance || STOP_MOVE_FLAG)
            {
                MagnetoLogger.Log("Desired position reached.", LogFactoryLogLevel.LogLevel.SUCCESS);
                MagnetoSerialConsole.ClearTermRead();

                // Reset set stop flag to true if loop entered because position was reached
                STOP_MOVE_FLAG = true;
                
                return true;
            }

            await Task.Delay(100); // Adjust based on your scenario
            attempt++;
        }

        MagnetoLogger.Log("Failed to reach the desired position within the maximum number of attempts.", LogFactoryLogLevel.LogLevel.ERROR);
        return false;
    }

    // TODO: Put this method in a helper

    /// <summary>
    /// Extracts a double value from a given string, assuming a specific format.
    /// The method searches for a numeric value within the string, starting just after a '#'
    /// character and ending at a specified position after a period. It logs the process and 
    /// handles format incompatibilities.
    /// </summary>
    /// <param name="input"></param>
    /// <returns></returns>
    static double ExtractDoubleFromString(string input)
    {
        var msg = "Extracting double from position string...";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);

        // Check if the input string starts with '#'
        if (input.StartsWith("#"))
        {
            // Find the index of the period after '#'
            var dotIndex = input.IndexOf('.');

            // If a period is found, attempt to parse the substring
            if (dotIndex != -1)
            {
                // Extract the substring from '#' to the period and parse as double
                var numberString = input.Substring(1, dotIndex + 6);
                if (double.TryParse(numberString, out var result))
                {
                    return result;
                }
            }
        }
        else
        {
            msg = "Incompatible string format.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
            return -1.0;
        }

        // TODO: Throw exception if this fails
        return -1.0;
    }

    #endregion

    #region Error Methods

    /// <summary>
    /// Send error message about motor
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns> Returns error associated with implementation error coding
    public int SendError(string message)
    {
        throw new NotImplementedException();
    }

    #endregion
}
