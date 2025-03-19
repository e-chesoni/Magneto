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

    // Unique print id
    public Int64 UUID
    {
        get; set;
    }

    /// <summary>
    /// Path to directory with slices associated with print
    /// </summary>
    public string SliceDirectory
    {
        get; set;
    }

    /// <summary>
    /// Directory size
    /// </summary>
    public int DirectorySize
    {
        get; set;
    }

    /// <summary>
    /// Date/time print was started
    /// </summary>
    public DateTime StartTimestamp
    {
        get; set;
    }

    /// <summary>
    /// Date/time print was completed
    /// </summary>
    public DateTime EndTimestamp
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
    /// Number of layers printed currently
    /// </summary>
    public Int32 LayersPrinted
    {
        get; set; 
    }

    /// <summary>
    /// Path to O2 log
    /// </summary>
    public string O2Path
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

    /// <summary>
    /// Notes about print
    /// </summary>
    public string Notes
    {
        get; set;
    }

    #endregion
}
