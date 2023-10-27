using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using Magneto.Desktop.WinUI.Core.Contracts.Services;
//using Microsoft.Extensions.Logging.Console.Internal;

namespace Magneto.Desktop.WinUI.Core.Services;

public static class MagnetoSerialConsole
{
    #region Private Variables

    /// <summary>
    /// Serial port
    /// </summary>
    private static SerialPort _serialPort = new SerialPort();


    private static List<SerialPort> _serialPorts = new List<SerialPort>();

    #endregion

    #region Default Port Setting Variables

    /// <summary>
    /// Success boolean for methods
    /// </summary>
    private static bool _success;

    /// <summary>
    /// Default port name for port setup
    /// </summary>
    private static readonly string _defaultPortName = "COM4";

    /// <summary>
    /// Default baud rate for port setup
    /// </summary>
    private static readonly int _defaultBaudRate = 38400;

    /// <summary>
    /// Default parity for port setup
    /// </summary>
    private static readonly string _defaultParity = "None";

    /// <summary>
    /// Default data bits for port setup
    /// </summary>
    private static readonly int _defaultDataBits = 8;

    /// <summary>
    /// Default stop bits for port setup
    /// </summary>
    private static readonly string _defaultStopBits = "One";

    /// <summary>
    /// Default handshake for port setup
    /// </summary>
    private static readonly string _defaultHandshake = "None";

    #endregion

    #region Initialization Methods
    /// <summary>
    /// Initialize a new port and manually set port characteristics
    /// </summary>
    /// <param name="port"></param> Desired port to use (ex. COM3)
    /// <param name="baud"></param> Desired port baud rate
    /// <param name="parity"></param> Desired port parity
    /// <param name="dataBits"></param> Desired port data bits
    /// <param name="stopBits"></param> Desired port stop bits
    /// <param name="handshake"></param> Desired port handshake
    public static void InitializePort(string portName, int baud, string parity, int dataBits, string stopBits, string handshake)
    {
        var msg = $"Initializing a new serial port: {portName}";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.WARN);

        SerialPort serialPort = new SerialPort();

        // Allow the user to set the appropriate properties.
        serialPort.PortName = SetPortName(portName);
        serialPort.BaudRate = SetBaudRate(baud);
        serialPort.Parity = SetParity(parity);
        serialPort.DataBits = SetDataBits(dataBits);
        serialPort.StopBits = SetStopBits(stopBits);
        serialPort.Handshake = SetHandshake(handshake);

        _serialPorts.Add(serialPort);
    }

    #endregion

    #region Port Setup Methods

    /// <summary>
    /// Print list of available ports
    /// </summary>
    public static void GetAvailablePorts()
    {
        var msg = "";
        MagnetoLogger.Log("Available Ports:", LogFactoryLogLevel.LogLevel.DEBUG);
        foreach (var s in SerialPort.GetPortNames())
        {
            msg = $"   {s}";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);
        }
    }

    public static void GetInitializedPorts()
    {
        var msg = "";
        MagnetoLogger.Log("Initialized Ports:", LogFactoryLogLevel.LogLevel.DEBUG);
        foreach (SerialPort s in _serialPorts)
        {
            msg = $"   {s.PortName}";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);
        }
    }

    /// <summary>
    /// Set which port to use (ex. COM3)
    /// </summary>
    /// <param name="portName"></param> Port to use
    /// <returns></returns>
    private static string SetPortName(string portName)
    {
        if (portName == "" || !(portName.ToLower()).StartsWith("com"))
        {
            portName = _defaultPortName;
        }
        
        var msg = $"Setting port name to {portName}";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.DEBUG);
        
        return portName;
    }

    /// <summary>
    /// Set the port baud rate
    /// </summary>
    /// <param name="baudRate"></param> Baud rate
    /// <returns></returns>
    private static int SetBaudRate(int baudRate)
    {
        if (baudRate == 0)
        {
            baudRate = _defaultBaudRate;
        }
        
        var msg = $"Setting baud rate to {baudRate}";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.DEBUG);
        
        return baudRate;
    }

    /// <summary>
    /// Set the port parity
    /// </summary>
    /// <param name="parity"></param> Parity
    /// <returns></returns>
    private static Parity SetParity(string parity)
    {
        if (parity == "")
        {
            parity = _defaultParity.ToString();
        }

        var msg = $"Setting parity to {parity}";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.DEBUG);

        return (Parity)Enum.Parse(typeof(Parity), parity, true);
    }

    /// <summary>
    /// Set the port data bits
    /// </summary>
    /// <param name="dataBits"></param> Data bits
    /// <returns></returns>
    private static int SetDataBits(int dataBits)
    {
        var msg = $"Setting DataBits to {dataBits}";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.DEBUG);
        
        return dataBits;
    }

    /// <summary>
    /// Set the port stop bits
    /// </summary>
    /// <param name="stopBits"></param> Stop bits
    /// <returns></returns>
    private static StopBits SetStopBits(string stopBits)
    {
        var msg = $"Setting stopBits to {stopBits}";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.DEBUG);

        return (StopBits)Enum.Parse(typeof(StopBits), stopBits, true);
    }

    /// <summary>
    /// Set the port handshake
    /// </summary>
    /// <param name="handshake"></param> Handshake
    /// <returns></returns>
    private static Handshake SetHandshake(string handshake)
    {
        var msg = $"Setting handshake to {handshake}";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.DEBUG);

        return (Handshake)Enum.Parse(typeof(Handshake), handshake, true);
    }

    /// <summary>
    /// Manually set port characteristics
    /// </summary>
    /// <param name="port"></param> Desired port to use (ex. COM3)
    /// <param name="baud"></param> Desired port baud rate
    /// <param name="parity"></param> Desired port parity
    /// <param name="dataBits"></param> Desired port data bits
    /// <param name="stopBits"></param> Desired port stop bits
    /// <param name="handshake"></param> Desired port handshake
    public static void SetSerialPort(string port, int baud, string parity, int dataBits, string stopBits, string handshake)
    {
        var msg = "Initializing user defined serial port...";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.WARN);

        // Allow the user to set the appropriate properties.
        _serialPort.PortName = SetPortName(port);
        _serialPort.BaudRate = SetBaudRate(baud);
        _serialPort.Parity = SetParity(parity);
        _serialPort.DataBits = SetDataBits(dataBits);
        _serialPort.StopBits = SetStopBits(stopBits);
        _serialPort.Handshake = SetHandshake(handshake);
    }

    /// <summary>
    /// Use preset/default settings to set up port
    /// </summary>
    public static void SetDefaultSerialPort()
    {
        var msg = "Initializing default serial port...";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.DEBUG);
        
        _serialPort.PortName = SetPortName(_defaultPortName);
        _serialPort.BaudRate = SetBaudRate(_defaultBaudRate);
        _serialPort.Parity = SetParity(_defaultParity);
        _serialPort.DataBits = SetDataBits(_defaultDataBits);
        _serialPort.StopBits = SetStopBits(_defaultStopBits);
        _serialPort.Handshake = SetHandshake(_defaultHandshake);
    }

    #endregion

    #region Open and Close Methods

    /// <summary>
    /// Open the serial port
    /// </summary>
    /// <returns></returns>
    public static bool OpenSerialPort(string portName)
    {
        var msg = $"Opening serial port {portName}";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.DEBUG);

        // Find the port in ports list
        SerialPort foundPort = _serialPorts.Find(port => port.PortName == portName);

        if (foundPort != null)
        {
            msg = $"Found port {foundPort.PortName}";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);

            // Try opening found port
            try { foundPort.Open(); }
            catch (InvalidOperationException)
            {
                msg = "The port is already open.";
                MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.SUCCESS);
            }
            catch (Exception e)
            {
                MagnetoLogger.Log(e.ToString(), LogFactoryLogLevel.LogLevel.ERROR);
                _success = false;
            }

            if (foundPort.IsOpen) { _success = true; }
        }
        else // If not in list, return failed
        {
            msg = $"Could not find port {portName}";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);
            _success = false;
        }

        return _success;
    }

    /// <summary>
    /// Close the serial port
    /// </summary>
    /// <returns></returns>
    public static bool CloseSerialPort(string portName)
    {
        var msg = $"Closing serial port {portName}";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.DEBUG);

        // Find the port in ports list
        SerialPort foundPort = _serialPorts.Find(port => port.PortName == portName);
        
        if (foundPort != null)
        {
            // Try closing the serial port
            try { _serialPort.Close(); }
            catch
            {
                msg = "Cannot close serial port!";
                MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
                _success = false;
            }

            if (!_serialPort.IsOpen) { _success = true; }
        }
        else // If not in list, return failed
        {
            msg = $"Could not find port {portName}";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);
            _success = false;
        }

        return _success;
    }

    public static bool CloseAllSerialPorts()
    {
        var msg = "Closing all serial ports";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.DEBUG);

        foreach (SerialPort port in _serialPorts)
        {
            // Try closing the serial port
            try { port.Close(); }
            catch
            {
                msg = $"Could not close serial port {port.PortName}";
                MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
                _success = false;
            }
        }

        if (!_serialPort.IsOpen) { _success = true; }

        return _success;
    }

    #endregion

    #region Read and Write Methods

    /// <summary>
    /// Write a message to output
    /// </summary>
    /// <param name="msg"></param> Message to write to output
    public static void Write(string msg)
    {
        System.Diagnostics.Debug.WriteLine(msg);
    }

    /// <summary>
    /// Write a message to the serial console
    /// ( Used to send motor commands )
    /// </summary>
    /// <param name="serial_msg"></param> Message to write to the serial console
    public static void SerialWrite(string portName, string serial_msg)
    {
        var msg = $"Sending move command to port {portName}";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.DEBUG);

        // Find the port in ports list
        SerialPort foundPort = _serialPorts.Find(port => port.PortName == portName);

        if (foundPort != null)
        {
            msg = $"Found port {portName}! Sending move command...";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.DEBUG);

            if (foundPort.IsOpen)
            {
                if (foundPort.BytesToRead <= 0)
                {
                    ASCIIEncoding encoding = new ASCIIEncoding();
                    byte[] data = encoding.GetBytes(serial_msg);

                    try
                    {
                        msg = "Trying to send data...";
                        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.WARN);
                        foundPort.Write(data, 0, data.Length);
                        foundPort.Write("\n\r");
                        msg = "Data sent.";
                        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.SUCCESS);
                    }
                    catch
                    {
                        msg = "Cannot write to serial port.";
                        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
                    }
                }
            }
            else
            {
                msg = $"Could not open port {portName}.";
                MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
            }
        }

        
    }

    /// <summary>
    /// Read a message from the serial console
    /// TODO: This method is untested
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    /// <returns></returns>
    public static string SerialRead(object sender, SerialDataReceivedEventArgs e)
    {
        var s = "";

        if (_serialPort.IsOpen)
        {
            if (_serialPort.BytesToWrite <= 0)
            {
                while (_serialPort.BytesToRead > 0)
                {
                    try
                    {
                        s = _serialPort.ReadLine();
                    }
                    catch (Exception ex)
                    {
                        MagnetoLogger.Log(ex.ToString(), LogFactoryLogLevel.LogLevel.ERROR);
                    }
                }
            }
        }

        return s;
    }

    #endregion
}
