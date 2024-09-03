using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Magneto.Desktop.WinUI.Core.Models.Artifact;
using Magneto.Desktop.WinUI.Core.Services;

namespace Magneto.Desktop.WinUI.Core.Models.BuildModels;

/// <summary>
/// Dance model stores all poses to print an image model
/// </summary>
public class DanceModel
{
    #region Public Variables

    /// <summary>
    /// A stack of poses (PoseModels)
    /// </summary>
    public Stack<PoseModel> dance = new();

    #endregion


    #region Generator

    /// <summary>
    /// Generates a stack of poses
    /// </summary>
    /// <param name="slice"></param> Slice stack
    /// <param name="thickness"></param> Desired thickness of each layer for print
    /// <returns></returns> A stack of poses for one print
    public Stack<PoseModel> GenerateDance(Stack<Slice> slices, double thickness)
    {
        var msg = $"Using {slices.Count} slices and thickness of {thickness} to create a dance...";
        MagnetoLogger.Log(msg, Contracts.Services.LogFactoryLogLevel.LogLevel.VERBOSE);

        foreach (Slice s in slices)
        {
            (double, Slice) pose = (thickness, s);
            PoseModel poseModel = new PoseModel(pose);
            dance.Push(poseModel);
        }

        return dance;
    }

    #endregion


    #region Constructor

    /// <summary>
    /// DanceModel constructor
    /// </summary>
    public DanceModel() { }

    #endregion

}
