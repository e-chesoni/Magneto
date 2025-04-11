using System.Runtime.CompilerServices;
using static Magneto.Desktop.WinUI.Core.Contracts.Services.LogFactoryLogLevel;
using static Magneto.Desktop.WinUI.Core.Contracts.Services.LogFactoryOutputLevel;

namespace Magneto.Desktop.WinUI.Core;

/// <summary>
/// Logger for Magneto app
/// </summary>
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

    #region Logging Methods

    /// <summary>
    /// Store log message in a file
    /// </summary>
    /// <param name="level"></param> Log level
    /// <param name="msg"></param> Log message
    /// <exception cref="NotImplementedException"></exception>
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
        // Initialize category for log
        var category = "";

        // Get time stamp
        var currentTime = DateTime.Now.ToString("HH:mm:ss tt");

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

        // Create a header with the class and method name
        var fileName = Path.GetFileName(filePath);
        var className = fileName.Substring(0, fileName.IndexOf("."));
        var msg = $"[{currentTime}] : {className}::{origin} -- {message}";

        // If the user wants to know where the log originated from...
        if (IncludeLogOriginDetails)
            msg = $"{msg} [{fileName} > {origin}() > Line {lineNumber}]" ;

        // Print message in output
        System.Diagnostics.Debug.WriteLine(msg, category);
    }

    #endregion
}