using Magneto.Desktop.WinUI.Core.Contracts.Services;
using Magneto.Desktop.WinUI.Core.Contracts.Services.Motor;
using Magneto.Desktop.WinUI.Core.Services;
using System.Text.RegularExpressions;

namespace Magneto.Desktop.WinUI.Core.Models.Motor;
public class StepperMotor : IStepperMotor
{
    #region Private Variables

    /// <summary>
    /// Calculated motor position
    /// </summary>
    double _calculatedPos;

    /// <summary>
    /// Motor status
    /// </summary>
    private MotorStatus _status;

    private double _homePos { get; set; }

    #endregion

    #region Public Variables

    public string motorPort { get; set; }

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

    #region Public Variables

    int motorId
    {
        get; set;
    }

    /// <summary>
    ///  The axis that the motor is attached to
    /// </summary>
    public int motorAxis
    {
        get; set;
    }

    #endregion

    #region Constructor
    
    /// <summary>
    /// StepperMotor constructor
    /// </summary>
    /// <param name="motorName"></param> The axis that the motor is attached to
    public StepperMotor(string portName, int axis)
    {
        motorPort = portName;
        motorAxis = axis;
        _homePos = 0; // default; in sweep it should be offset (so motor isn't centered at home)

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
                motorId = resultNumber;
                var msg = $"Assigning ID: {resultNumber} to motor";
                MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.SUCCESS);
            }
            else
            {
                motorId = 0;
                var msg = "Conversion to integer for motor ID failed.";
                MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
            }
        }
    }

    #endregion

    #region Getters and Setters
    public int GetMotorID()
    {
        return motorId;
    }

    public string GetMotorPort()
    {
        return motorPort; 
    }

    #endregion

    #region Movement Methods

    /// <summary>
    /// Move motor to position zero
    /// </summary>
    /// <returns></returns> Returns -1 if home command fails, 0 if home command is successful
    public async Task HomeMotor()
    {
        MagnetoLogger.Log("Homing motor...",
            LogFactoryLogLevel.LogLevel.VERBOSE);
        await MoveMotorAbs(0);
        _calculatedPos = 0;
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
    public Task MoveMotorAbs(double pos)
    {
        // Invalid position
        if (pos < 0 || pos > 35)
        {
            MagnetoLogger.Log("Invalid position. Aborting motor move operation.",
                LogFactoryLogLevel.LogLevel.ERROR);
            return Task.CompletedTask;
        }

        // Move motor
        var s = string.Format("{0}MVA{1}", motorAxis, pos);
        if (MagnetoSerialConsole.OpenSerialPort(motorPort))
        {
            MagnetoSerialConsole.SerialWrite(motorPort, s);

            // Log message
            var msg = string.Format("Moving motor on axis {0} to position {1}mm",
                motorAxis, pos);
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);

            // Update calculated position
            _calculatedPos = pos;
            msg = $"New calculated position: {_calculatedPos}";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);
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
    public Task MoveMotorRel(double pos)
    {
        // get the current position
        // TODO: In the future use GetPos to get desiredPos
        //var currPos = GetPos();
        var desiredPos = _calculatedPos + pos;

        // if the current position + steps is greater than 35, fail
        if (desiredPos < 0 || desiredPos > 35)
        {
            MagnetoLogger.Log("Invalid position. Aborting motor move operation.",
                LogFactoryLogLevel.LogLevel.ERROR);
            return Task.CompletedTask;
        }

        var s = string.Format("{0}MVR{1}", motorAxis, pos);
        if (MagnetoSerialConsole.OpenSerialPort(motorPort))
        {
            MagnetoSerialConsole.SerialWrite(motorPort, s);

            // Log message
            var msg = string.Format("Moving motor on axis {0} {1}mm relative to current position",
                motorAxis, pos);
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);
            
            msg = $"Command Sent: {s}";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);

            // Update calculated position
            _calculatedPos = desiredPos;
            msg = $"New calculated position: {_calculatedPos}";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);
        }
        else
        {
            MagnetoLogger.Log("Port Closed.", LogFactoryLogLevel.LogLevel.ERROR);
        }

        return Task.CompletedTask;
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

    /// <summary>
    /// Get current motor position
    /// </summary>
    /// <returns></returns> Returns -1 if request for position fails, otherwise returns motor position
    public double GetPos()
    {
        var s = string.Format("{0}POS?", motorAxis);

        // TODO: Needs testing; if uncommented causes MoveMotorRel to fail
        bool tested = false;

        if (tested)
        {
            MagnetoSerialConsole.SerialWrite(motorPort, s);
            // TODO: Read serial console to get position value
            // TOOD: Figure out how to actually read serial console (safely)
            // see: Micronix note https://www.dropbox.com/scl/fo/2ls4fr6ffx0nswuno2n4x/h/System.IO.Ports%20Example%20Program%20and%20Guide?dl=0&preview=System.IO.Ports+C%23+Guide.pdf&subfolder_nav_tracking=1
            // see: Microsoft SerialPort.ReadLine Method https://learn.microsoft.com/en-us/dotnet/api/system.io.ports.serialport.readline?source=recommendations&view=dotnet-plat-ext-7.0

            // TODO: This probably won't work
            // see: https://stackoverflow.com/questions/13754694/what-is-the-correct-way-to-read-a-serial-port-using-net-framework
            // test: In WPF app or simple console log app
            // string s = MagnetoSerialConsole.SerialRead(sender, e);

            // Remember to check if the port is open!
        }

        return 0;
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
