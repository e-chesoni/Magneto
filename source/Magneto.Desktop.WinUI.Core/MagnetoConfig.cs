﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Magneto.Desktop.WinUI.Core.Contracts.Services;
using Magneto.Desktop.WinUI.Core.Models;


namespace Magneto.Desktop.WinUI.Core;

public class MagnetoMotorConfig
{
    public string motorName { get; set; }
    public string COMPort { get; set; }
    public int axis { get; set; }
    public double maxPos { get; set; }
    public double minPos { get; set; }
    public double homePos { get; set; }
}

public class COMPortConfig
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
    private static IEnumerable<COMPortConfig> AllCOMPorts()
    {
        return new List<COMPortConfig>()
        {
            new COMPortConfig()
            {
                port = 4,
                baudRate = 38400,
                parity = "None",
                dataBits = 8,
                stopBits = "One",
                handshake = "None",
            },
            new COMPortConfig()
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

    private static IEnumerable<MagnetoMotorConfig> AllMotors()
    {
        return new List<MagnetoMotorConfig>()
        {
            new MagnetoMotorConfig()
            {
                motorName = "powder",
                COMPort = GetCOMPortName(GetCOMPort(4)),
                axis = 1,
                maxPos = 35,
                minPos = 0,
                homePos = 0,
            },
            new MagnetoMotorConfig()
            {
                motorName = "build",
                COMPort = GetCOMPortName(GetCOMPort(4)),
                axis = 2,
                maxPos = 35,
                minPos = 0,
                homePos = 0,
            },
            new MagnetoMotorConfig()
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

    public static COMPortConfig GetCOMPort(int portNumber)
    {
        return AllCOMPorts().FirstOrDefault(port => port.port == portNumber);
    }

    public static string GetCOMPortName(COMPortConfig c)
    {
        return $"COM{c.port}";
    }

    public static MagnetoMotorConfig GetMotorByName(string name)
    {
        return AllMotors().FirstOrDefault(motor => motor.motorName == name);
    }

    public static IEnumerable<COMPortConfig> GetAllCOMPorts()
    {
        return AllCOMPorts();
    }

    public static IEnumerable<MagnetoMotorConfig> GetAllMotors()
    {
        return AllMotors();
    }
}