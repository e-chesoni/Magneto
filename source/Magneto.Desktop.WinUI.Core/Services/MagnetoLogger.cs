using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Magneto.Desktop.WinUI.Core.Services;
public class MagnetoLogger
{
    public enum logLevels : ushort
    {
        DEBUG,
        INFO,
        WARN,
        ERROR,
        CRITICAL
    }

    logLevels logLevel;

    // Constructor
    public MagnetoLogger()
    {
        this.logLevel = (int)logLevels.DEBUG;
        SetLogLevel(logLevel);
    }

    public void SetLogLevel(logLevels level)
    {
        this.logLevel = level;

        switch (logLevel)
        {
            case logLevels.DEBUG:
                MagnetoSerialConsole.SetForegroundColor("GRAY");
                break;
            case logLevels.INFO:
                MagnetoSerialConsole.SetForegroundColor("GREEN");
                break;
            case logLevels.WARN:
                MagnetoSerialConsole.SetForegroundColor("YELLOW");
                break;
            case logLevels.ERROR:
                MagnetoSerialConsole.SetForegroundColor("DARKYELLOW");
                break;
            case logLevels.CRITICAL:
                MagnetoSerialConsole.SetForegroundColor("RED");
                break;
        }
    }

    public void LogToConsole(logLevels level, string msg)
    {
        // Get time stamp
        string currentTime = DateTime.Now.ToString("HH:mm:ss tt ");

        // Set the foreground color to white to print the time stamp
        MagnetoSerialConsole.SetForegroundColor("WHITE");

        // Use write to keep ts and msg on the same line
        MagnetoSerialConsole.Write(currentTime);

        // Set log level (this will change the color of the message depending on log level)
        SetLogLevel(level);

        MagnetoSerialConsole.Write(msg);
    }

    public void LogToFile(int level, string msg)
    {
        throw new NotImplementedException();
    }
}