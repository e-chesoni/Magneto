using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Magneto.Desktop.WinUI.Core.Models;

/// <summary>
/// Class for sample print data
/// TOOD: Delete once app is in production
/// </summary>
public class SamplePrint
{
    #region Public Variables

    /// <summary>
    /// Various print status
    /// </summary>
    public enum PrintStatus : ushort
    {
        NotStarted,
        InProgress,
        Paused,
        Canceled,
        Complete
    }

    /// <summary>
    /// File name
    /// </summary>
    public string FileName
    {
        get; set;
    }

    /// <summary>
    /// Path to file
    /// </summary>
    public string FileLocation
    {
        get; set;
    }

    /// <summary>
    /// File size
    /// </summary>
    public int FileSize
    {
        get; set;
    }

    /// <summary>
    /// Date/time file was established in Magneto system
    /// </summary>
    public DateTime CreatedAt
    {
        get; set;
    }

    /// <summary>
    /// Print status
    /// </summary>
    public PrintStatus Status
    {
        get; set;
    }

    /// <summary>
    /// Symbol associated with a print
    /// </summary>
    public int SymbolCode
    {
        get; set;
    }

    /// <summary>
    /// Name of the symbol associated with this print
    /// </summary>
    public string SymbolName
    {
        get; set;
    }

    /// <summary>
    /// Symbol code
    /// </summary>
    public char Symbol => (char)SymbolCode;

    #endregion
}
