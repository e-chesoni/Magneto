using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Magneto.Desktop.WinUI.Core.Models.Constants.MicronixConstants;

namespace Magneto.Desktop.WinUI.Core.Models;
public class MicronixError
{
    public MICRONIX_ERROR_CODE code { get; set; }
    public string command { get; set; }
    public string message { get; set; }
}