using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Magneto.Desktop.WinUI.Core.Models.Image;
using Magneto.Desktop.WinUI.Core.Services;

namespace Magneto.Desktop.WinUI.Core.Models.BuildModels;

/// <summary>
/// Dance model stores all poses to print an image model
/// </summary>
public class DanceModel
{
    #region Public Variables

    /// <summary>
    /// A stack of PoseModels
    /// </summary>
    public Stack<PoseModel> dance = new();

    #endregion

    #region Constructor

    /// <summary>
    /// DanceModel constructor
    /// </summary>
    public DanceModel() { }

    #endregion

    #region Getters

    /// <summary>
    /// Generates a stack of poses
    /// </summary>
    /// <param name="slice"></param> Slice stack
    /// <param name="thickness"></param> Desired thickness of each layer for print
    /// <returns></returns> A stack of poses for one print
    public Stack<PoseModel> GetPoseStack(Stack<Slice> slices, double thickness)
    {
        MagnetoLogger.Log("DanceModel::GetPoseStack -- Getting pose stack...", Contracts.Services.LogFactoryLogLevel.LogLevel.VERBOSE);

        foreach (Slice s in slices) 
        {
            (double, Slice) pose = (thickness, s);
            PoseModel poseModel = new PoseModel(pose);
            dance.Push(poseModel);
        }

        return dance;
    }

    #endregion
}
