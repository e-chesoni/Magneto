using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Magneto.Desktop.WinUI.Core.Models.Motors;
public class PositionReportedEventArgs : EventArgs
{
    public int Axis
    {
        get;
    }
    public double Position
    {
        get;
    }

    public PositionReportedEventArgs(int axis, double position)
    {
        Axis = axis;
        Position = position;
    }
}