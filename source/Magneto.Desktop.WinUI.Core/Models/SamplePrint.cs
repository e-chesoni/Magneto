using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Magneto.Desktop.WinUI.Core.Models;
public class SamplePrint
{
    public enum PrintStatus : ushort
    {
        NotStarted,
        InProgress,
        Paused,
        Canceled,
        Complete
    }

    public string FileName
    {
        get; set;
    }

    public string FileLocation
    {
        get; set;
    }

    public int FileSize
    {
        get; set;
    }

    public DateTime CreatedAt
    {
        get; set;
    }

    #region Symbols

    public PrintStatus Status
    {
        get; set;
    }

    public int SymbolCode
    {
        get; set;
    }

    public string SymbolName
    {
        get; set;
    }

    public char Symbol => (char)SymbolCode;

    #endregion
}
