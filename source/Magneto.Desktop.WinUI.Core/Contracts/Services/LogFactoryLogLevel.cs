using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Magneto.Desktop.WinUI.Core.Contracts.Services;
public class LogFactoryLogLevel
{
    /// <summary>
    /// The severity of the log message
    /// </summary>
    public enum LogLevel
    {
        /// <summary>
        /// Developer-specific information
        /// </summary>
        DEBUG = 1,

        /// <summary>
        /// Verbose information
        /// </summary>
        VERBOSE = 2,

        /// <summary>
        /// General information
        /// </summary>
        WARN = 3,

        /// <summary>
        /// An error
        /// </summary>
        ERROR = 4,

        /// <summary>
        /// A success
        /// </summary>
        SUCCES = 5,
    }
}
