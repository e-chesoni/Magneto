using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Magneto.Desktop.WinUI.Core.Models.Image;
using Magneto.Desktop.WinUI.Core.Services;

namespace Magneto.Desktop.WinUI.Core.Models.BuildModels;
public class DanceModel
{
    public Stack<PoseModel> dance = new();

    public DanceModel()
    {

    }

    public Stack<PoseModel> GetPoseStack(Stack<Slice> slice, double thickness)
    {
        MagnetoLogger.Log("DanceModel::GetPoseStack -- Getting pose stack...", Contracts.Services.LogFactoryLogLevel.LogLevel.VERBOSE);

        foreach (Slice s in slice) 
        {
            (double, Slice) pose = (thickness, s);
            PoseModel poseModel = new PoseModel(pose);
            dance.Push(poseModel);
        }

        return dance;
    }
}
