using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static Magneto.Desktop.WinUI.Core.Contracts.Services.LogFactoryLogLevel;
using static Magneto.Desktop.WinUI.Core.Contracts.Services.LogFactoryOutputLevel;

namespace Magneto.Desktop.WinUI.Core.Contracts.Services;
public interface ILogFactory
{
    #region Properties

    /// <summary>
    /// The level of logging to output
    /// </summary>
    LogOutputLevel LogFactoryLogLevel
    {
        get; set;
    }

    /// <summary>
    /// If true, includes the origin of where the log message was logged from
    /// such as the class name, line number and file name
    /// </summary>
    bool IncludeLogOriginDetails
    {
        get; set;
    }

    #endregion

    #region Events

    /// <summary>
    /// Fires whenever a new log arrives
    /// </summary>
    event Action<(string Message, LogLevel Level)> NewLog;

    #endregion

    #region Methods

    /// <summary>
    /// Adds the specific logger to this factory
    /// </summary>
    /// <param name="logger">The logger</param>
    void AddLogger(IMagnetoLogger logger);

    /// <summary>
    /// Removes the specified logger from this factory
    /// </summary>
    /// <param name="logger">The logger</param>
    void RemoveLogger(IMagnetoLogger logger);

    /// <summary>
    /// Logs the specific message to all loggers in this factory
    /// </summary>
    /// <param name="message">The message to log</param>
    /// <param name="level">The level of the message being logged</param>
    /// <param name="origin">The method/function this message was logged in</param>
    /// <param name="filePath">The code filename that this message was logged from</param>
    /// <param name="lineNumber">The line of code in the filename this message was logged from</param>
    void Log(string message, LogLevel level = LogLevel.VERBOSE, [CallerMemberName] string origin = "", [CallerFilePath] string filePath = "", [CallerLineNumber] int lineNumber = 0);

    #endregion
}
