using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Magneto.Desktop.WinUI.Core.Contracts.Services.StateMachineServices;
using Magneto.Desktop.WinUI.Core.Models.BuildModels;
using Magneto.Desktop.WinUI.Core.Models.Image;
using Magneto.Desktop.WinUI.Core.Models.Motor;
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

        // Set the build flag to resume
        _BuildManagerSM.build_flag = BuildManager.BuildFlag.RESUME;

        // Start drawing
        _ = Draw();
    }

    public void Cancel()
    {
        MagnetoLogger.Log("PrintingBuildManagerState::Cancel -- Cancel flag triggered!",
            Contracts.Services.LogFactoryLogLevel.LogLevel.WARN);
        _BuildManagerSM.build_flag = BuildManager.BuildFlag.CANCEL;
        _BuildManagerSM.TransitionTo(new CancelledBuildManagerState(_BuildManagerSM));
    }

    public void Pause()
    {
        MagnetoLogger.Log("PrintingBuildManagerState::pause -- Pause flag triggered!",
            Contracts.Services.LogFactoryLogLevel.LogLevel.WARN);
        _BuildManagerSM.build_flag = BuildManager.BuildFlag.PAUSE;
        _BuildManagerSM.TransitionTo(new PausedBuildManagerState(_BuildManagerSM));
    }

    public void Start(ImageModel im) => throw new NotImplementedException();

    public async Task Draw()
    {
        // TODO: Find a way to set flag using interrupts (if user wants to pause/cancel print)

        while (_BuildManagerSM.danceModel.dance.Count > 0)
        {
            // Bust a move (pop a pose of the list)
            var move = _BuildManagerSM.danceModel.dance.Pop();
            
            // Get motor positions and slice from pose
            var thickness = move.thickness;
            var slice = move.slice;

            switch (_BuildManagerSM.build_flag)
            {
                case BuildManager.BuildFlag.RESUME:
                    // TODO: Motor should move down thickness

                    /* TOOD: UPDATE BUILD ORDER OF OPERATIONS:
                     * 1. Motor 1 moves down
                     * 2. Draw shape
                     * 3. Motor 2 moves up
                     * 4. Sweep
                     */
                    //_BuildManagerSM.buildController.MoveMotorRel(1, -thickness);

                    // TODO: Add artificial wait for draw before laser has been incorporated--will have to wait for user response (button) or timeout (abort)
                    // TODO: Add pop-up for user to execute draw, and indicate draw is complete

                    _BuildManagerSM.laserController.Draw(slice); // this was in original code

                    //_BuildManagerSM.buildController.MoveMotorRel(2, thickness);

                    _BuildManagerSM.buildController.MoveMotorsRel(thickness);
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
            // Sweep
            _BuildManagerSM.sweepController.MoveMotorRel(1, _BuildManagerSM.GetSweepDist());
            Thread.Sleep(2000); // Wait one second before sweeping back (for dramatic effect)
            _BuildManagerSM.sweepController.MoveMotorRel(1, -_BuildManagerSM.GetSweepDist());
        }
        _BuildManagerSM.TransitionTo(new DoneBuildManagerState(_BuildManagerSM));
    }

    public void Resume() => throw new NotImplementedException();
    public void Done() => throw new NotImplementedException();
    public async Task Homing()
    {
        // Home motors
        await _BuildManagerSM.buildController.HomeMotors();
        await _BuildManagerSM.sweepController.HomeMotors();

        // Return to idle state
        _BuildManagerSM.TransitionTo(new IdleBuildManagerState(_BuildManagerSM));
    }
}
