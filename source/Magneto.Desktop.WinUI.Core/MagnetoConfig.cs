using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Magneto.Desktop.WinUI.Core.Contracts.Services;
using Magneto.Desktop.WinUI.Core.Models;


namespace Magneto.Desktop.WinUI.Core;

public class MagnetoMotor
{
    public string motorName { get; set; }
    public string COMPort { get; set; }
    public int axis { get; set; }
    public double maxPos { get; set; }
    public double minPos { get; set; }
    public double homePos { get; set; }
}

public class COMPort
{
    public int port { get; set; }
    public int baudRate { get; set; }
    public string parity { get; set; }
    public int dataBits { get; set; }
    public string stopBits { get; set; }
    public string handshake { get; set; }
}
public static class MagnetoConfig
{
    // TODO: Add getter methods to return motor info
    // TODO: Add IEnum for com ports
    // TODO: Change "COMPort" on motor to be a comport from enum
    private static List<MagnetoMotor> _allMotors;

    private static List<COMPort> _allCOMPorts;

    private static IEnumerable<COMPort> AllCOMPorts()
    {
        return new List<COMPort>()
        {
            new COMPort()
            {
                port = 4,
                baudRate = 38400,
                parity = "None",
                dataBits = 8,
                stopBits = "One",
                handshake = "None",
            },
            new COMPort()
            {
                port = 7,
                baudRate = 38400,
                parity = "None",
                dataBits = 8,
                stopBits = "One",
                handshake = "None",
            },
        };
    }

    private static IEnumerable<MagnetoMotor> AllMotors()
    {
        return new List<MagnetoMotor>()
        {
            new MagnetoMotor()
            {
                motorName = "powder",
                COMPort = GetCOMPortName(GetCOMPort(4)),
                axis = 1,
                maxPos = 35,
                minPos = 0,
                homePos = 0,
            },
            new MagnetoMotor()
            {
                motorName = "build",
                COMPort = GetCOMPortName(GetCOMPort(4)),
                axis = 2,
                maxPos = 35,
                minPos = 0,
                homePos = 0,
            },
            new MagnetoMotor()
            {
                motorName = "sweep",
                COMPort = GetCOMPortName(GetCOMPort(7)),
                axis = 1,
                maxPos = 50,
                minPos = -50,
                homePos = 0,
            },
        };
    }

    public static COMPort GetCOMPort(int portNumber)
    {
        return AllCOMPorts().FirstOrDefault(port => port.port == portNumber);
    }

    public static string GetCOMPortName(COMPort c)
    {
        return $"COM{c.port}";
    }

    public static MagnetoMotor GetMotorByName(string name)
    {
        return AllMotors().FirstOrDefault(motor => motor.motorName == name);
    }

    public static IEnumerable<COMPort> GetAllCOMPorts()
    {
        return AllCOMPorts();
    }

    public static IEnumerable<MagnetoMotor> GetAllMotors()
    {
        return AllMotors();
    }

    public static async Task<IEnumerable<MagnetoMotor>> GetMotorsDataAsync()
    {
        // if _allMotors is null, set equal to new...
        _allMotors ??= new List<MagnetoMotor>(AllMotors());

        await Task.CompletedTask;
        return _allMotors;
    }

    public static async Task<IEnumerable<MagnetoMotor>> GetCOMPortsDataAsync()
    {
        // if _allCOMPorts is null, set equal to new...
        _allCOMPorts ??= new List<COMPort>(AllCOMPorts());

        await Task.CompletedTask;
        return _allMotors;
    }

}
