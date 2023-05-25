using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Magneto.Desktop.WinUI.Core.Models.Image;

namespace Magneto.Desktop.WinUI.Core.Models.BuildModels;
public class PoseModel
{
    public (Slice, double, double) Pose;

    public PoseModel((Slice, double, double) pose)
    {
        Pose = pose;
    }
}
