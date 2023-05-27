using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.PortableExecutable;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Magneto.Desktop.WinUI.Core.Contracts.Services;
using static System.Runtime.InteropServices.JavaScript.JSType;
using static Magneto.Desktop.WinUI.Core.Contracts.Services.LogFactoryLogLevel;
using static Magneto.Desktop.WinUI.Core.Contracts.Services.LogFactoryOutputLevel;
using static Magneto.Desktop.WinUI.Core.Services.MagnetoLogger;

namespace Magneto.Desktop.WinUI.Core.Services;
public static class MagnetoLogger
{
    #region Public Properties

    /// <summary>
    /// The level of logging to output
    /// </summary>
    public static LogOutputLevel LogFactoryOutputLevel
    {
        get; set;
    }

    /// <summary>
    /// If true, includes the origin of where the log message was logged from
    /// such as the class name, line number and file name
    /// </summary>
    public static bool IncludeLogOriginDetails { get; set; } = true;

    #endregion

    public static string Concat(string header, string method, string message)
    {
        return string.Format("{0}{1} -- {2}", header, method, message);
    }

    public static void LogWithHeader(string header, string method, string message, 
        LogLevel level = LogLevel.VERBOSE,
        [CallerMemberName] string origin = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        var msg = Concat(header, method, message);
        Log(msg, level, origin, filePath, lineNumber);
    }

    public static void LogToFile(int level, string msg)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Logs the specific message to all loggers in this factory
    /// </summary>
    /// <param name="message">The message to log</param>
    /// <param name="level">The level of the message being logged</param>
    /// <param name="origin">The method/function this message was logged in</param>
    /// <param name="filePath">The code filename that this message was logged from</param>
    /// <param name="lineNumber">The line of code in the filename this message was logged from</param>
    public static void Log(
        string message,
        LogLevel level = LogLevel.VERBOSE,
        [CallerMemberName] string origin = "",
        [CallerFilePath] string filePath = "",
        [CallerLineNumber] int lineNumber = 0)
    {
        // Init category for log
        var category = "";

        // Get time stamp
        string currentTime = DateTime.Now.ToString("HH:mm:ss tt");

        // Color console based on level
        switch (level)
        {
            // Debug is blue
            case LogLevel.DEBUG:
                category = "Information";
                break;
            // Verbose is gray
            case LogLevel.VERBOSE:
                category = "verbose";
                break;

            // Warning is yellow
            case LogLevel.WARN:
                category = "warning";
                break;

            // Error is red
            case LogLevel.ERROR:
                category = "error";
                break;

            // Success is green
            case LogLevel.SUCCESS:
                category = "-----";
                break;
        }

        // If we should not log the message as the level is too low...
        if ((int)level < (int)LogFactoryOutputLevel)
            return;

        var fileName = Path.GetFileName(filePath);
        var header = fileName.Substring(0, fileName.IndexOf("."));
        var msg = $"[{currentTime}] : {header}::{origin} -- {message}";

        // If the user wants to know where the log originated from...
        if (IncludeLogOriginDetails)
            msg = $"{msg} [{fileName} > {origin}() > Line {lineNumber}]" ;

        System.Diagnostics.Debug.WriteLine(msg, category);

    }
}