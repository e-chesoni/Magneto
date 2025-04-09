using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Magneto.Desktop.WinUI.Core.Models.Artifact;

namespace Magneto.Desktop.WinUI.Core.Models.Print;

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
    public (double thickness, Slice slice) Pose;

    #endregion

    #region Constructor

    public PoseModel((double thickness, Slice slice) pose)
    {
        //Pose = pose;
        thickness = pose.thickness;
        slice = pose.slice;
    }

    #endregion
}
