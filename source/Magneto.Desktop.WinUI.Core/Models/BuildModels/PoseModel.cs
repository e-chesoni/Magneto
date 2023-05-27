using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Magneto.Desktop.WinUI.Core.Models.Image;

namespace Magneto.Desktop.WinUI.Core.Models.BuildModels;

/// <summary>
/// Poses couple slices with layer thickness
/// </summary>
public class PoseModel
{
    #region Public Variables

    /// <summary>
    /// Slice for this pose
    /// </summary>
    public Slice slice { get; set; }

    /// <summary>
    /// Thickness for this pose
    /// </summary>
    public double thickness { get; set; }

    /// <summary>
    /// Tuple containing thickness and slice for this pose
    /// </summary>
    public (double, Slice) Pose;

    #endregion

    #region Constructor

    public PoseModel((double, Slice) pose)
    {
        Pose = pose;
        thickness = pose.Item1;
        slice = pose.Item2;
    }

    #endregion
}
