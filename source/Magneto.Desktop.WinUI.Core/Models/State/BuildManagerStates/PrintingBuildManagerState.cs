using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Magneto.Desktop.WinUI.Core.Contracts.Services.StateMachineServices;
using Magneto.Desktop.WinUI.Core.Models.BuildModels;
using Magneto.Desktop.WinUI.Core.Models.Image;
using Magneto.Desktop.WinUI.Core.Services;

namespace Magneto.Desktop.WinUI.Core.Models.State.BuildManagerStates;
public class PrintingBuildManagerState : IBuildManagerState
{
    private BuildManager BuildManagerSM { get; set; }

    public PrintingBuildManagerState(BuildManager bm)
    {
        MagnetoLogger.Log("PrintingBuildManagerState", Contracts.Services.LogFactoryLogLevel.LogLevel.VERBOSE);
        BuildManagerSM = bm;
    }

    public void Cancel()
    {
        BuildManagerSM.build_flag = BuildManager.BuildFlag.CANCEL;
        BuildManagerSM.TransitionTo(new CancelledBuildManagerState(BuildManagerSM));
    }

    public void Pause()
    {
        BuildManagerSM.build_flag = BuildManager.BuildFlag.PAUSE;
        BuildManagerSM.TransitionTo(new PausedBuildManagerState(BuildManagerSM));
    }

    public void Start() => throw new NotImplementedException();

    public void Draw()
    {
        while (BuildManagerSM.danceModel.dance.Count > 0)
        {
            var poseModel = BuildManagerSM.danceModel.dance.Pop();
            var slice = poseModel.Pose.Item1;
            var motor1Pos = poseModel.Pose.Item2;
            var motor2Pos = poseModel.Pose.Item3;

            switch (BuildManagerSM.build_flag)
            {
                case BuildManager.BuildFlag.RESUME:
                    // TODO: Call laser to draw image
                    BuildManagerSM.laserController.Draw(slice);
                    BuildManagerSM.buildController.MoveMotors(motor1Pos, motor2Pos);
                    break;

                case BuildManager.BuildFlag.PAUSE:
                    Pause();
                    break;

                case BuildManager.BuildFlag.CANCEL:
                    Cancel();
                    break;

                default:
                    Cancel();
                    break;
            }
        }
    }

    public void Resume() => throw new NotImplementedException();
}
