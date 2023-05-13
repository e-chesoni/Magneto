using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
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
    /*                  COLOR CODING FOR LOGGING                    */
    /****************************************************************/
    // Color setup for console logging

    enum magnetoConsoleColor
    {
        WHITE = ConsoleColor.White,
        GRAY = ConsoleColor.Gray,
        GREEN = ConsoleColor.Green,
        YELLOW = ConsoleColor.Yellow,
        DARKYELLOW = ConsoleColor.DarkYellow,
        RED = ConsoleColor.Red,
    }
    public static void SetForegroundColor(string color)
    {
        string formattedColor = color.ToUpper();

        switch (formattedColor)
        {
            case "GRAY":
                Console.ForegroundColor = ConsoleColor.Gray;
                break;
            case "GREEN":
                Console.ForegroundColor = ConsoleColor.Green;
                break;
            case "YELLOW":
                Console.ForegroundColor = ConsoleColor.Yellow;
                break;
            case "DARKYELLOW":
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                break;
            case "RED":
                Console.ForegroundColor = ConsoleColor.Red;
                break;
            default:
                Console.ForegroundColor = ConsoleColor.White;
                break;
        }
    }

    /****************************************************************/
    /*                   MAGNETO CONSOLE LOGGER                     */
    /****************************************************************/
    // Special Logger Methods for Magneto Console
    // (does not have access to logger b/c logger uses it)

    private static void LogCurrentTime()
    {
        SetForegroundColor("WHITE");
        Console.Write(DateTime.Now.ToString("HH:mm:ss tt "));
    }

    private static void MagnetoLogMessage(string format, string msg)
    {
        Console.WriteLine(format, msg);
    }

    private static void MagnetoLogMessage(string format, int msg)
    {
        Console.WriteLine(format, msg);
    }

    private static void MagnetoConsoleLogDEFAULT(string message)
    {
        LogCurrentTime();
        SetForegroundColor("WHITE");
        MagnetoLogMessage("{0}", message);
    }

    private static void MagnetoConsoleLogDEBUG(string message)
    {
        LogCurrentTime();
        SetForegroundColor("GRAY");
        MagnetoLogMessage("{0}", message);
    }

    private static void MagnetoConsoleLogINFO(string message)
    {
        LogCurrentTime();
        SetForegroundColor("GREEN");
        MagnetoLogMessage("{0}", message);
    }

    private static void MagnetoConsoleLogWARNING(string message)
    {
        LogCurrentTime();
        SetForegroundColor("YELLOW");
        MagnetoLogMessage("{0}", message);
    }

    private static void MagnetoConsoleLogERROR(string message)
    {
        LogCurrentTime();
        SetForegroundColor("DARKYELLOW");
        MagnetoLogMessage("{0}", message);
    }

    private static void MagnetoConsoleLogCRITICAL(string message)
    {
        LogCurrentTime();
        SetForegroundColor("RED");
        MagnetoLogMessage("{0}", message);
    }

    /****************************************************************/
    /*                         PORT SETUP                           */
    /****************************************************************/

    /// <summary>
    /// Print list of available ports
    /// </summary>
    public static void GetAvailablePorts()
    {
        MagnetoConsoleLogINFO("Available Ports:");
        foreach (string s in SerialPort.GetPortNames())
        {
            MagnetoLogMessage("   {0}", s);
        }
    }

    private static string SetPortName(string portName)
    {
        LogCurrentTime();
        SetForegroundColor("GREY");
        if (portName == "" || !(portName.ToLower()).StartsWith("com"))
        {
            portName = _defaultPortName;
        }
        MagnetoLogMessage("Setting port name to {0}", portName);
        return portName;
    }

    private static int SetBaudRate(int baudRate)
    {
        SetForegroundColor("GREY");
        if (baudRate == 0)
        {
            baudRate = _defaultBaudRate;
        }
        MagnetoLogMessage("Setting baud rate to {0}", baudRate);
        return baudRate;
    }

    private static Parity SetParity(string parity)
    {
        SetForegroundColor("GREY");
        if (parity == "")
        {
            parity = _defaultParity.ToString();
        }
        MagnetoLogMessage("Setting parity to {0}", parity);
        return (Parity)Enum.Parse(typeof(Parity), parity, true);
    }

    private static int SetDataBits(int dataBits)
    {
        SetForegroundColor("GREY");
        MagnetoLogMessage("Setting DataBits to {0}", dataBits);
        return dataBits;
    }

    private static StopBits SetStopBits(string stopBits)
    {
        SetForegroundColor("GREY");
        MagnetoLogMessage("Setting StopBits to {0}", stopBits);
        return (StopBits)Enum.Parse(typeof(StopBits), stopBits, true);
    }

    private static Handshake SetHandshake(string handshake)
    {
        SetForegroundColor("GREY");
        MagnetoLogMessage("Setting handshake to {0}", handshake);
        return (Handshake)Enum.Parse(typeof(Handshake), handshake, true);
    }

    public static void SetSerialPort(string port, int baud, string parity, int dataBits, string stopBits, string handshake)
    {
        MagnetoConsoleLogWARNING("Initializing user defined serial port...");

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

        MagnetoConsoleLogDEBUG("Initializing default serial port...");

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
        MagnetoConsoleLogDEBUG("Opening serial port...");

        // Try opening the serial port
        try { _serialPort.Open(); }
        catch (InvalidOperationException)
        {
            MagnetoConsoleLogINFO("The port is already open.");
        }
        catch (Exception e)
        {
            MagnetoConsoleLogCRITICAL(e.ToString());
            _success = false;
        }

        if (_serialPort.IsOpen) { _success = true; }

        return _success;
    }

    public static bool CloseSerialPort()
    {
        MagnetoConsoleLogDEBUG("Closing serial port...");

        // Try opening the serial port
        try { _serialPort.Close(); }
        catch
        {
            MagnetoConsoleLogCRITICAL("Cannot close serial port!");
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
        Console.Write(msg);
    }

    public static void SerialWrite(string msg)
    {
        MagnetoConsoleLogDEBUG("Sending move command...");

        if (_serialPort.IsOpen)
        {
            if (_serialPort.BytesToRead <= 0)
            {
                System.Text.ASCIIEncoding encoding = new System.Text.ASCIIEncoding();
                byte[] data = encoding.GetBytes(msg);

                try
                {
                    MagnetoConsoleLogWARNING("Trying to send data...");
                    _serialPort.Write(data, 0, data.Length);
                    _serialPort.Write("\n\r");
                    MagnetoConsoleLogINFO("Data sent.");
                }
                catch
                {
                    MagnetoConsoleLogCRITICAL("Cannot write to serial port.");
                }
            }
        }
        else
        {
            MagnetoConsoleLogCRITICAL("Serial port not open.");
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
                        MagnetoConsoleLogCRITICAL(ex.ToString());
                    }
                }
            }
        }

        return s;
    }

}
