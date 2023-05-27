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
        MagnetoLogger.Log("DanceModel::GetPoseStack -- Getting pose stack...", Contracts.Services.LogFactoryLogLevel.LogLevel.VERBOSE);

        foreach (Slice s in slice) 
        {
            // TODO: Calculate motor heights
            double height = CalculateMotorHeight(s);
            (double, Slice) pose = (height, s);

            PoseModel poseModel = new PoseModel(pose);
            dance.Push(poseModel);
        }

        return dance;
    }

    private double CalculateMotorHeight(Slice s)
    {
        MagnetoLogger.Log("DanceModel::CalculateMotorHeight -- Hard-coded values used to generate motor heights...", 
            Contracts.Services.LogFactoryLogLevel.LogLevel.WARN);

        double height = 5;
        return height;
    }
}
