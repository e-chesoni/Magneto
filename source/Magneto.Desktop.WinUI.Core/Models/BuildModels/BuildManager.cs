using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Magneto.Desktop.WinUI.Core.Contracts.Services;
using Magneto.Desktop.WinUI.Core.Contracts.Services.Controllers;
using Magneto.Desktop.WinUI.Core.Contracts.Services.StateMachineServices;
using Magneto.Desktop.WinUI.Core.Models.Controllers;
using Magneto.Desktop.WinUI.Core.Models.Image;
using Magneto.Desktop.WinUI.Core.Models.Monitor;
using Magneto.Desktop.WinUI.Core.Models.State.BuildManagerStates;
using Magneto.Desktop.WinUI.Core.Services;
using static Magneto.Desktop.WinUI.Core.Models.Motor.StepperMotor;

namespace Magneto.Desktop.WinUI.Core.Models.BuildModels;

/// <summary>
/// Coordinates printing tasks across components
/// </summary>
public class BuildManager : ISubsciber, IStateMachine
{
    #region Public Variables

    /// NOTE: Currently, Magneto has two motor controllers; 
    /// One for the build motors (on the base of the housing)
    /// And one for powder distribution (sweep) motor

    /// <summary>
    /// Controller for build motors
    /// </summary>
    public MotorController buildController;

    /// <summary>
    /// Controller for sweep motor
    /// </summary>
    public MotorController sweepController;

    /// <summary>
    /// Controller for laser and scan head
    /// </summary>
    public LaserController laserController;

    //public Stack<Slice> workingSlices;
    public DanceModel danceModel;

    /// <summary>
    /// Stack of lists coordinating image slices with motor positions
    /// </summary>
    //public Stack<PoseModel> printPoses;

    private IBuildManagerState _state = null;

    public BuildFlag build_flag;

    public enum BuildFlag : ushort
    {
        RESUME,
        PAUSE,
        CANCEL
    }

    #endregion

    #region Constructor

    /// <summary>
    /// 
    /// </summary>
    /// <param name="buildController"></param>
    /// <param name="sweepController"></param>
    /// <param name="laserController"></param>
    public BuildManager(MotorController bc, MotorController sc, LaserController lc)
    {
        buildController = bc;
        sweepController = sc;
        laserController = lc;

        TransitionTo(new IdleBuildManagerState(this));
    }

    #endregion

    public MotorStatus GetStatus()
    {
        throw new NotImplementedException();
    }

    #region State Machine Methods

    public void TransitionTo(IBuildManagerState state)
    {
        _state = state;
    }

    public void Start(ImageModel im)
    {
        _state.Start();
    }

    public void Pause()
    {
        _state.Pause();
    }

    public void Cancel()
    {
        _state.Cancel();
    }

    #endregion

    #region Subscriber Methods

    public void ReceiveUpdate(IPublisher publisher) => throw new NotImplementedException();

    public void HandleUpdate(IPublisher publisher) => throw new NotImplementedException();

    #endregion
}
