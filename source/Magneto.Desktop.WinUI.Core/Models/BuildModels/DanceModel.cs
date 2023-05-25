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

    public Stack<PoseModel> GetPoseStack(Stack<Slice> slice)
    {
        MagnetoLogger.Log("Getting pose stack...", Contracts.Services.LogFactoryLogLevel.LogLevel.VERBOSE);

        foreach (Slice s in slice) 
        {
            // TODO: Calculate motor heights
            (double, double) t = CalculateMotorHeight(s);
            (Slice, double, double) pose = (s, t.Item1, t.Item2);

            PoseModel poseModel = new PoseModel(pose);
            dance.Push(poseModel);
        }

        return dance;
    }

    private (double, double) CalculateMotorHeight(Slice s)
    {
        MagnetoLogger.Log("Hard-coded values used to generate motor heights...", 
            Contracts.Services.LogFactoryLogLevel.LogLevel.WARN);
        (double, double) t = (5, 5);
        return t;
    }
}
