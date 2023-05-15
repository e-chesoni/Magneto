using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Magneto.Desktop.WinUI.Core.Services.MagnetoLogger;

namespace Magneto.Desktop.WinUI.Core.Services;
public static class MagnetoLogger
{
    public enum logLevels : ushort
    {
        DEBUG,
        VERBOSE,
        WARN,
        ERROR,
        SUCCESS
    }

    public static void LogToFile(int level, string msg)
    {
        throw new NotImplementedException();
    }

    public static void Log(string message, logLevels level)
    {
        // Save old color
        var category = "";

        // Get time stamp
        string currentTime = DateTime.Now.ToString("HH:mm:ss tt");

        // Color console based on level
        switch (level)
        {
            // Debug is blue
            case logLevels.DEBUG:
                category = "Information";
                break;
            // Verbose is gray
            case logLevels.VERBOSE:
                category = "verbose";
                break;

            // Warning is yellow
            case logLevels.WARN:
                category = "warning";
                break;

            // Error is red
            case logLevels.ERROR:
                category = "error";
                break;

            // Success is green
            case logLevels.SUCCESS:
                category = "-----";
                break;
        }

        var msg = $"[{currentTime}] : " + message;

        System.Diagnostics.Debug.WriteLine(msg, category);

    }
}