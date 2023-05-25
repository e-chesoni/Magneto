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
    private static SerialPort _serialPort = new SerialPort();
    static bool _success;
    private static string _defaultPortName = "COM4";
    private static int _defaultBaudRate = 38400;
    private static string _defaultParity = "None";
    private static int _defaultDataBits = 8;
    private static string _defaultStopBits = "One";
    private static string _defaultHandshake = "None";

    /****************************************************************/
    /*                         PORT SETUP                           */
    /****************************************************************/

    /// <summary>
    /// Print list of available ports
    /// </summary>
    public static void GetAvailablePorts()
    {
        var msg = "";
        MagnetoLogger.Log("Available Ports:", LogFactoryLogLevel.LogLevel.DEBUG);
        foreach (string s in SerialPort.GetPortNames())
        {
            msg = $"   {s}";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);
        }
    }

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

    private static int SetDataBits(int dataBits)
    {
        var msg = $"Setting DataBits to {dataBits}";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.DEBUG);
        
        return dataBits;
    }

    private static StopBits SetStopBits(string stopBits)
    {
        var msg = $"Setting stopBits to {stopBits}";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.DEBUG);

        return (StopBits)Enum.Parse(typeof(StopBits), stopBits, true);
    }

    private static Handshake SetHandshake(string handshake)
    {
        var msg = $"Setting handshake to {handshake}";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.DEBUG);

        return (Handshake)Enum.Parse(typeof(Handshake), handshake, true);
    }

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

    /****************************************************************/
    /*                     OPEN & CLOSE METHODS                     */
    /****************************************************************/
    public static bool OpenSerialPort()
    {
        var msg = "Opening serial port...";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.DEBUG);

        // Try opening the serial port
        try { _serialPort.Open(); }
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

        if (_serialPort.IsOpen) { _success = true; }

        return _success;
    }

    public static bool CloseSerialPort()
    {
        var msg = "Closing serial port...";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.DEBUG);

        // Try opening the serial port
        try { _serialPort.Close(); }
        catch
        {
            msg = "Cannot close serial port!";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
            _success = false;
        }

        if (!_serialPort.IsOpen) { _success = true; }

        return _success;
    }

    /****************************************************************/
    /*                     READ/WRITE METHODS                       */
    /****************************************************************/
    public static void Write(string msg)
    {
        System.Diagnostics.Debug.WriteLine(msg);
    }

    public static void SerialWrite(string serial_msg)
    {
        var msg = "Sending move command...";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.DEBUG);

        if (_serialPort.IsOpen)
        {
            if (_serialPort.BytesToRead <= 0)
            {
                ASCIIEncoding encoding = new ASCIIEncoding();
                byte[] data = encoding.GetBytes(serial_msg);

                try
                {
                    msg = "Trying to send data...";
                    MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.WARN);
                    _serialPort.Write(data, 0, data.Length);
                    _serialPort.Write("\n\r");
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
            msg = "Serial port not open.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
        }
    }

    public static string SerialRead(object sender, SerialDataReceivedEventArgs e)
    {
        string s = "";

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

}
