using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Magneto.Desktop.WinUI.Core.Contracts.Services.StateMachineServices;
using Magneto.Desktop.WinUI.Core.Models.Image;

namespace Magneto.Desktop.WinUI.Core.Models.State.BuildManagerStates;
public class PrintingBuildManagerState : IBuildManagerState
{
    public PrintFlag print_flag;

    public enum PrintFlag : ushort
    {   
        RESUME,
        PAUSE,
        CANCEL
    }

    private BuildManager BuildManagerSM;

    public ImageModel imageModel;

    public PrintingBuildManagerState(ImageModel im)
    {
        imageModel = im;
    }

    public void SetStateMachine(BuildManager bm)
    {
        BuildManagerSM = bm;
    }
    public void Cancel() => throw new NotImplementedException();
    public void Pause() => throw new NotImplementedException();
    public void Start() => throw new NotImplementedException();
    public void Draw()
    {
        foreach (var slice in imageModel.sliced_image)
        {

            switch (print_flag)
            {
                case PrintFlag.RESUME:
                    // TODO:
                    // Call laser to draw image
                    BuildManagerSM.laserController.Draw(slice);
                    break;

                case PrintFlag.PAUSE:
                    break;

                case PrintFlag.CANCEL:
                    BuildManagerSM.TransitionTo(new CancelledBuildManagerState());
                    break;

                default:
                    BuildManagerSM.TransitionTo(new CancelledBuildManagerState());
                    break;
            }
        }
    }
}
