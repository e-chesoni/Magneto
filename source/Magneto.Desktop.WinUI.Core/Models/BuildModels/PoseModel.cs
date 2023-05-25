using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Magneto.Desktop.WinUI.Core.Models.Image;

namespace Magneto.Desktop.WinUI.Core.Models.BuildModels;
public class PoseModel
{
    public (double, double, Slice) Pose;

    public double motor1Position { get; set; }

    public double motor2Position { get; set; }

    public Slice slice { get; set; }

    public PoseModel((double, double, Slice) pose)
    {
        Pose = pose;
        motor1Position = pose.Item1;
        motor2Position = pose.Item2;
        slice = pose.Item3;
    }
}
