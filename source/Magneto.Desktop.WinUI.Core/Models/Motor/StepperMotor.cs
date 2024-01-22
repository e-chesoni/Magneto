using Magneto.Desktop.WinUI.Core.Contracts.Services;
using Magneto.Desktop.WinUI.Core.Contracts.Services.Motor;
using Magneto.Desktop.WinUI.Core.Services;
using Microsoft.VisualBasic;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace Magneto.Desktop.WinUI.Core.Models.Motor;
public class StepperMotor : IStepperMotor
{
    #region Private Variables

    private readonly double _tolerance = 0.05;

    /// <summary>
    /// Motor ID (the number of the COM port followed by the axis the motor is attached to)
    /// </summary>
    private int _motorId { get; set; }

    private string _motorName { get; set; }

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

    private double _homePos { get; set; }

    private double _maxPos { get; set; }

    private double _minPos { get; set; }

    private double _motorVelocity { get; set; }

    private double _currentPos { get; set; }

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
    /// Directions in which the motor can move
    /// </summary>
    /*
    public enum MotorDirection : short
    {
        Down = -1,
        Up = 1
    }
    */

    // TODO: Define error codes for stepper motor
    /// <summary>
    /// Motor error codes
    /// </summary>
    public enum MotorError : short
    {
    
    }

    #endregion


    #region Constructor
    
    /// <summary>
    /// StepperMotor constructor
    /// </summary>
    /// <param name="motorName"></param> The axis that the motor is attached to
    public StepperMotor(string motorName, string portName, int axis, double maxPos, double minPos, double homePos, double vel)
    {
        // TODO: settings to config file
        _motorName = motorName;
        _motorPort = portName;
        _motorAxis = axis;
        _maxPos = maxPos;
        _minPos = minPos;
        _homePos = homePos;
        _motorVelocity = vel;

        // Create ID for motor
        // Use regular expression to match the number part
        Match match = Regex.Match(portName, @"\d+");

        if (match.Success)
        {
            // Extract the number from the match
            var numberString = match.Value;

            // Create the new string by concatenating the original number with "1"
            var idString = numberString + axis;

            if (int.TryParse(idString, out var resultNumber))
            {
                _motorId = resultNumber;
                var msg = $"Assigning ID: {resultNumber} to motor";
                MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.SUCCESS);
            }
            else
            {
                _motorId = 0;
                var msg = "Conversion to integer for motor ID failed.";
                MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
            }
        }

        var message = $"Created Stepper Motor attached to {_motorPort} on axis {_motorAxis} with max position of {_maxPos}, min position of {_minPos}, and home position of {_homePos}";
        MagnetoLogger.Log(message, LogFactoryLogLevel.LogLevel.VERBOSE);
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
    /// <param name="pos"></param>
    /// <returns></returns> Returns completed task when finished
    public async Task MoveMotorAbsAsync(double pos)
    {
        var initialPos = GetPos();
        MagnetoLogger.Log($"Initial motor position: {initialPos}", LogFactoryLogLevel.LogLevel.WARN);

        if (pos < _minPos || pos > _maxPos)
        {
            MagnetoLogger.Log($"Invalid position: {pos}. For motor {_motorName}, _minPos is {_minPos} and _maxPos is {_maxPos}. Aborting motor move operation.", LogFactoryLogLevel.LogLevel.ERROR);
            return;
        }

        if (MagnetoSerialConsole.OpenSerialPort(_motorPort))
        {
            await PerformMotorMoveAsync(pos);
        }
        else
        {
            MagnetoLogger.Log("Port Closed.", LogFactoryLogLevel.LogLevel.ERROR);
        }
    }

    private async Task PerformMotorMoveAsync(double pos)
    {
        var moveCmd = $"{_motorAxis}MVA{pos}";
        MagnetoSerialConsole.SerialWrite(_motorPort, moveCmd);

        _motorMoving = true;
        MagnetoLogger.Log($"Moving motor on axis {_motorAxis} to position {pos}mm", LogFactoryLogLevel.LogLevel.VERBOSE);

        _calculatedPos = pos;
        MagnetoLogger.Log($"New calculated position: {_calculatedPos}", LogFactoryLogLevel.LogLevel.VERBOSE);

        await CheckPosAsync(pos);

        _motorMoving = false;
    }

    /// NOTE: The syntax to move a motor in relative to a position is:
    /// nMVRx
    /// Where n is the axis
    /// And x is the number of mm to move

    /// <summary>
    /// Move motor relative to current position
    /// </summary>
    /// <param name="steps"></param>
    /// <returns></returns> Returns -1 if move command fails, 0 if move command is successful
    public async Task MoveMotorRelAsync(double pos)
    {
        var initialPos = GetPos();
        var desiredPos = initialPos + pos;
        var msg = $"Initial position of {_motorName} motor: {initialPos}. Desired relative position: {desiredPos}";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.WARN);

        if (desiredPos < _minPos || desiredPos > _maxPos)
        {
            MagnetoLogger.Log($"Invalid position: {desiredPos} for {_motorName}. _minPos is {_minPos} and _maxPos is {_maxPos}. Aborting motor move operation.", LogFactoryLogLevel.LogLevel.ERROR);
            return;
        }

        if (MagnetoSerialConsole.OpenSerialPort(_motorPort))
        {
            var moveCommand = $"{_motorAxis}MVR{pos}";
            MagnetoSerialConsole.SerialWrite(_motorPort, moveCommand);
            MagnetoLogger.Log($"Moving {_motorName} motor on axis {_motorAxis} {pos}mm relative to current position. Command Sent: {moveCommand}", LogFactoryLogLevel.LogLevel.VERBOSE);

            // Asynchronously wait until the desired position is reached
            await CheckPosAsync(desiredPos);
        }
        else
        {
            MagnetoLogger.Log("Port Closed.", LogFactoryLogLevel.LogLevel.ERROR);
        }
    }

    /// <summary>
    /// EMERGENCY STOP: Stop motor
    /// </summary>
    /// <returns></returns> Returns -1 if stop command fails, 0 if move command is successful
    public Task StopMotor()
    {
        throw new NotImplementedException();
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

    #endregion

    #region Helpers
    /// <summary>
    /// Get current motor position
    /// </summary>
    /// <returns></returns> Returns -1 if request for position fails, otherwise returns motor position
    public double GetPos()
    {
        // Method entry notification for log
        var msg = $"Getting {_motorName} motor position...";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);

        // Clear old term
        msg = $"Clearing previous term...";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);
        MagnetoSerialConsole.ClearTermRead();

        // Format position request
        var s = string.Format("{0}POS?", _motorAxis);

        // Make sure port is open
        if (MagnetoSerialConsole.OpenSerialPort(_motorPort))
        {
            // Writing #POS? initializes data send;
            // Data received event is registered in MagnetoSerialConsole
            // Position should be pick up there
            MagnetoSerialConsole.SerialWrite(_motorPort, s);
            msg = $"Position request sent: {s}";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.SUCCESS);
        }
        else
        {
            msg = "Port Closed.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
            return -1.0;
        }

        string posString = null;
        var timeout = TimeSpan.FromSeconds(5); // Set timeout duration
        var stopwatch = Stopwatch.StartNew();

        // Loop until a valid position string is received
        while (string.IsNullOrEmpty(posString) || !posString.StartsWith("#"))
        {
            if (stopwatch.Elapsed > timeout)
            {
                msg = "Timeout reached while waiting for position data.";
                MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
                return -1.0;
            }

            posString = MagnetoSerialConsole.GetTermRead();
            msg = $"TermRead: {posString}";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);

            Thread.Sleep(100); // Use delay to avoid busy-waiting
        }

        var posDoub = ExtractDoubleFromString(posString);
        msg = $"Position as double: {posDoub}";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);

        return posDoub;
    }

    // TODO: Update methods to use asynchronous position check
    public async Task<bool> CheckPosAsync(double desiredPos)
    {
        MagnetoLogger.Log("Checking motor position.", LogFactoryLogLevel.LogLevel.VERBOSE);

        const int maxAttempts = 10000; // Can modify limit; currently will try to reach position for about 10sec
        var attempt = 0;

        while (attempt < maxAttempts)
        {
            _currentPos = GetPos();
            MagnetoLogger.Log($"Current Position: {_currentPos}, Desired Position: {desiredPos}", LogFactoryLogLevel.LogLevel.VERBOSE);

            if (Math.Abs(_currentPos - desiredPos) <= _tolerance)
            {
                MagnetoLogger.Log("Desired position reached.", LogFactoryLogLevel.LogLevel.SUCCESS);
                MagnetoSerialConsole.ClearTermRead();
                return true;
            }

            await Task.Delay(100); // Adjust based on your scenario
            attempt++;
        }

        MagnetoLogger.Log("Failed to reach the desired position within the maximum number of attempts.", LogFactoryLogLevel.LogLevel.ERROR);
        return false;
    }


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
