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

    private double tolerance = 0.05;

    /// <summary>
    /// Motor ID (the number of the COM port followed by the axis the motor is attached to)
    /// </summary>
    private int _motorId
    {
        get; set;
    }

    private string _motorName
    {
        get; set; 
    }

    private string _motorPort
    {
        get; set;
    }

    /// <summary>
    ///  The axis that the motor is attached to
    /// </summary>
    private int _motorAxis
    {
        get; set;
    }

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
    /// Directions in which  the motor can move
    /// </summary>
    public enum MotorDirection : short
    {
        Down = -1,
        Up = 1
    }

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

    #region Getters and Setters
    public string GetMotorName()
    {
        return _motorName;
    }

    public int GetID()
    {
        return _motorId;
    }

    public string GetPortName()
    {
        return _motorPort; 
    }

    public int GetAxis()
    {
        return _motorAxis;
    }

    public double GetHomePos()
    {
        return _homePos;
    }

    public double GetMaxPos()
    {
        return _maxPos;
    }

    public double GetMinPos()
    {
        return _minPos;
    }

    public double GetVelocity()
    {
        return _motorVelocity;
    }

    public void SetMotorName(string name)
    {
        _motorName = name;
    }

    public void SetPortName(string portName)
    {
        _motorPort = portName;
    }

    public void SetAxis(int axis)
    {
        _motorAxis = axis;
    }

    public void SetHomePos(double pos)
    {
        _homePos = pos;
    }

    public void SetMaxPos(double pos)
    {
        _maxPos = pos;
    }

    public void SetMinPos(double pos)
    {
        _minPos = pos;
    }

    public void SetVelocity(double vel)
    {
        _motorVelocity = vel;
    }

    #endregion

    // TODO: Update movement commands to check for position and return true if position was reached (consider using a while loop)
    #region Movement Methods

    /// <summary>
    /// Move motor to position zero
    /// </summary>
    /// <returns></returns> Returns -1 if home command fails, 0 if home command is successful
    public async Task HomeMotor()
    {
        MagnetoLogger.Log("Homing motor...",
            LogFactoryLogLevel.LogLevel.VERBOSE);
        await MoveMotorAbsAsync(GetHomePos());
        _calculatedPos = GetHomePos();
    }

    /// <summary>
    /// Move motor one step
    /// </summary>
    /// <param name="dir"></param> Direction to move motor
    /// <returns></returns>
    public int StepMotor(MotorDirection dir)
    {
        throw new NotImplementedException();
    }

    /// NOTE: The syntax to move a motor in an absolute direction is:
    /// nMVAx
    /// Where n is the axis
    /// And x is the number of mm to move

    /// <summary>
    /// Move motor to an absolute position
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns> Returns completed task when finished
    public Task MoveMotorAbsAsync(double pos)
    {
        var initialPos = GetPos();
        var msg = $"Initial motor position: {initialPos}";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.WARN);

        // Invalid position
        if (pos < _minPos || pos > _maxPos)
        {
            MagnetoLogger.Log($"Invalid position: {pos}. for motor {_motorName} _minPos is {_minPos} and _maxPos is {_maxPos}. Aborting motor move operation.",
                LogFactoryLogLevel.LogLevel.ERROR);
            return Task.CompletedTask;
        }

        // Move motor
        var s = string.Format("{0}MVA{1}", _motorAxis, pos);
        if (MagnetoSerialConsole.OpenSerialPort(_motorPort))
        {
            MagnetoSerialConsole.SerialWrite(_motorPort, s);

            // Log message
            msg = string.Format("Moving motor on axis {0} to position {1}mm",
                _motorAxis, pos);
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);

            // Update calculated position
            _calculatedPos = pos;
            msg = $"New calculated position: {_calculatedPos}";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);

            // Wait until position is reached
            CheckPos(pos);
        }
        else
        {
            MagnetoLogger.Log("Port Closed.", LogFactoryLogLevel.LogLevel.ERROR);
        }
        return Task.CompletedTask;
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
    public Task MoveMotorRelAsync(double pos)
    {
        // Calculate desired position:
        // Get current position
        var initialPos = GetPos();
        var msg = $"Initial position of {_motorName} motor: {initialPos}";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.WARN);

        // Add pos to current position
        var desiredPos = initialPos + pos; // TODO: FIX ME--somehow gets zero as desired pos in print

        // If the current position + steps is greater than _maxPos, fail
        if (desiredPos < _minPos || desiredPos > _maxPos)
        {
            msg = $"Invalid position: {desiredPos}. for {_motorName} build _minPos is {_minPos} and _maxPos is {_maxPos}. Aborting motor move operation.";
            MagnetoLogger.Log(msg,
                LogFactoryLogLevel.LogLevel.ERROR);
            return Task.CompletedTask;
        }

        // Format relative move command
        var s = string.Format("{0}MVR{1}", _motorAxis, pos);

        // Check if serial port is open
        if (MagnetoSerialConsole.OpenSerialPort(_motorPort))
        {
            // Send move command to serial port associated with this motor
            MagnetoSerialConsole.SerialWrite(_motorPort, s);

            // Log friendly message
            msg = $"Moving {_motorName} motor on axis {_motorAxis} {pos}mm relative to current position";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);
            
            // Log command sent
            msg = $"Command Sent: {s}";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.SUCCESS);

            // Wait until position is reached
            CheckPos(desiredPos);
        }
        else
        {
            MagnetoLogger.Log("Port Closed.", LogFactoryLogLevel.LogLevel.ERROR);
        }
        return Task.CompletedTask;
    }

    public bool CheckPos(double desiredPos)
    {
        var msg = "";
        bool posReached = false;

        while (!posReached)
        {
            msg = $"ENTERED POSITION CHECKING LOOP";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.WARN);

            // Update the current position each time we check
            var currentPos = GetPos();

            msg = $"Current Position: {currentPos}, Desired Position: {desiredPos}";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);

            if (Math.Abs(currentPos - desiredPos) <= tolerance)
            {
                msg = "Desired position reached. Exiting the loop.";
                MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.SUCCESS);

                // Set position reached to true
                posReached = true;

                // Clear the last term read
                MagnetoSerialConsole.ClearTermRead();

                // Make sure we exit the loop
                break;
            }
            msg = $"Sleeping. Will check again in a ms...";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);
            Thread.Sleep(100); // Sleep for 1 second before checking again (for example)
        }

        return posReached;
    }


    /// <summary>
    /// EMERGENCY STOP: Stop motor
    /// </summary>
    /// <returns></returns> Returns -1 if stop command fails, 0 if move command is successful
    public Task StopMotor()
    {
        throw new NotImplementedException();
    }

    #endregion

    #region Status Methods

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
            // Writing #POS? should initialize data send;
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

    /// <summary>
    /// Update the motor status
    /// </summary>
    /// <param name="newStatus"></param> New status of motor
    /// <returns></returns> Returns the updated status of the motor
    private MotorStatus UpdateStatus(MotorStatus newStatus)
    {
        _status = newStatus;
        return GetStatus();
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
