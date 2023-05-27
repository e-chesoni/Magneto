using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Magneto.Desktop.WinUI.Core.Models.Image;

namespace Magneto.Desktop.WinUI.Core.Models.BuildModels;
public class PoseModel
{
    public (double, Slice) Pose;

    public double thickness { get; set; }

    public Slice slice { get; set; }

    public PoseModel((double, Slice) pose)
    {
        Pose = pose;
        thickness = pose.Item1;
        slice = pose.Item2;
    }
}
