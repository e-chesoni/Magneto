using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Transactions;
using Magneto.Desktop.WinUI.Core.Contracts.Services.StateMachineServices;
using Magneto.Desktop.WinUI.Core.Models.BuildModels;
using Magneto.Desktop.WinUI.Core.Models.Image;
using Magneto.Desktop.WinUI.Core.Services;

namespace Magneto.Desktop.WinUI.Core.Models.State.BuildManagerStates;
public class IdleBuildManagerState : IBuildManagerState
{
    private BuildManager BuildManagerSM;

    public IdleBuildManagerState(BuildManager bm)
    {
        MagnetoLogger.Log("IdleBuildManagerState::IdleBuildManagerState", 
            Contracts.Services.LogFactoryLogLevel.LogLevel.VERBOSE);
        BuildManagerSM = bm;
    }

    public void Cancel() => throw new NotImplementedException();
    public void Pause() => throw new NotImplementedException();
    public void Start() => throw new NotImplementedException();

    public void Start(ImageModel im)
    {
        MagnetoLogger.Log("Starting...", Contracts.Services.LogFactoryLogLevel.LogLevel.VERBOSE);

        // Get poses for print
        BuildManagerSM.danceModel.GetPoseStack(im.sliceStack);

        BuildManagerSM.TransitionTo(new PrintingBuildManagerState(BuildManagerSM));
    }

    public void Draw()
    {
        MagnetoLogger.Log("Drawing should not be called from this state!", Contracts.Services.LogFactoryLogLevel.LogLevel.WARN);
    }

    public void Resume() => throw new NotImplementedException();
    public void Done() => throw new NotImplementedException();
}
