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
    private BuildManager _BuildManagerSM { get; set; }

    public PrintingBuildManagerState(BuildManager bm)
    {
        MagnetoLogger.Log("PrintingBuildManagerState::PrintingBuildManagerState", 
            Contracts.Services.LogFactoryLogLevel.LogLevel.VERBOSE);

        _BuildManagerSM = bm;
    }

    public void Cancel()
    {
        _BuildManagerSM.build_flag = BuildManager.BuildFlag.CANCEL;
        _BuildManagerSM.TransitionTo(new CancelledBuildManagerState(_BuildManagerSM));
    }

    public void Pause()
    {
        _BuildManagerSM.build_flag = BuildManager.BuildFlag.PAUSE;
        _BuildManagerSM.TransitionTo(new PausedBuildManagerState(_BuildManagerSM));
    }

    public void Start(ImageModel im) => throw new NotImplementedException();

    public void Draw()
    {
        while (_BuildManagerSM.danceModel.dance.Count > 0)
        {
            // Bust a move (pop a pose of the list)
            var move = _BuildManagerSM.danceModel.dance.Pop();
            
            // Get motor positions and slice from pose
            var motor1Pos = move.motor1Position;
            var motor2Pos = move.motor2Position;
            var slice = move.slice;

            switch (_BuildManagerSM.build_flag)
            {
                case BuildManager.BuildFlag.RESUME:
                    _BuildManagerSM.laserController.Draw(slice);
                    _BuildManagerSM.buildController.MoveMotors(motor1Pos, motor2Pos);
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
    public void Done() => throw new NotImplementedException();
}
