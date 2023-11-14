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
        var msg = "";

        // Move build motor to calculated print height
        //TODO: MOVE TO CALIBRATE STATE: This should be only method in calibrate motors to start
        var printHeight = MagnetoConfig.GetDefaultPrintThickness() * _BuildManagerSM.danceModel.dance.Count;
        msg = $"Print Height: {printHeight}";
        MagnetoLogger.Log(msg, Contracts.Services.LogFactoryLogLevel.LogLevel.VERBOSE);

        msg = $"Print layers: {_BuildManagerSM.danceModel.dance.Count}";
        MagnetoLogger.Log(msg, Contracts.Services.LogFactoryLogLevel.LogLevel.VERBOSE);

        _ = _BuildManagerSM.buildController.MoveMotorAbs(_BuildManagerSM.buildController.GetBuildMotor(), printHeight);
        // Wait for motor to get to height
        // TODO: Make wait more robust; right now waits for arbitrary 2 seconds
        // TODO: Could return a flag to indicate end wait
        /*
        var initialBuildWait = WaitTimeHelper(_BuildManagerSM.buildController.GetBuildMotor(), printHeight);
        Thread.Sleep(initialBuildWait);
        
        // Calculate sweep wait time once (used below)
        var sweepWait = WaitTimeHelper(_BuildManagerSM.sweepController.GetSweepMotor(), _BuildManagerSM.GetSweepDist());
        
        // Move sweep motor to starting position
        _ = _BuildManagerSM.sweepController.MoveMotorAbsAsync(_BuildManagerSM.sweepController.GetSweepMotor(), _BuildManagerSM.GetSweepDist());
        Thread.Sleep(sweepWait);

        // Initialize default wait times
        var buildWait = WaitTimeHelper(_BuildManagerSM.buildController.GetBuildMotor(), _BuildManagerSM.buildController.GetBuildMotor().GetMaxPos());
        var powderWait = WaitTimeHelper(_BuildManagerSM.buildController.GetPowderMotor(), _BuildManagerSM.buildController.GetPowderMotor().GetMaxPos());
        
        msg = $"Sweep wait time: {sweepWait} ms";
        MagnetoLogger.Log(msg, Contracts.Services.LogFactoryLogLevel.LogLevel.VERBOSE);
        */
        int ctr = 0;

        foreach(var move in _BuildManagerSM.danceModel.dance)
        {
            ctr++;
            msg = $"Executing loop {ctr}";
            MagnetoLogger.Log(msg, Contracts.Services.LogFactoryLogLevel.LogLevel.VERBOSE);

            // Bust a move (pop a pose of the list)
            //var move = _BuildManagerSM.danceModel.dance.Pop();
            
            // Get motor positions and slice from pose
            var thickness = move.thickness;
            msg = $"Layer Thickness: {thickness}";
            MagnetoLogger.Log(msg, Contracts.Services.LogFactoryLogLevel.LogLevel.VERBOSE);
            var slice = move.slice;

            switch (_BuildManagerSM.build_flag)
            {
                case BuildManager.BuildFlag.RESUME:
                    // TODO: Add artificial wait for draw before laser has been incorporated--will have to wait for user response (button) or timeout (abort)
                    // TODO: Add pop-up for user to execute draw, and indicate draw is complete

                    //_BuildManagerSM.laserController.Draw(slice); // this was in original code
                    // TODO: Calculate laser draw time
                    //Thread.Sleep(2000);

                    /*
                    _ = _BuildManagerSM.buildController.MoveMotorRelAsync(_BuildManagerSM.buildController.GetBuildMotor(), -thickness);
                    // Calculate build wait time
                    buildWait = WaitTimeHelper(_BuildManagerSM.buildController.GetBuildMotor(), thickness);
                    Thread.Sleep(buildWait);

                    _ = _BuildManagerSM.buildController.MoveMotorRelAsync(_BuildManagerSM.buildController.GetPowderMotor(), thickness);
                    // Calculate powder wait time
                    powderWait = WaitTimeHelper(_BuildManagerSM.buildController.GetPowderMotor(), thickness);
                    Thread.Sleep(powderWait);
                    */
                    // TODO: Try with await = (instead of _ =)
                    // Sweep
                    await _BuildManagerSM.sweepController.MoveMotorAbs(_BuildManagerSM.sweepController.GetSweepMotor(), -_BuildManagerSM.GetSweepDist());
                    await _BuildManagerSM.sweepController.MoveMotorAbs(_BuildManagerSM.sweepController.GetSweepMotor(), _BuildManagerSM.GetSweepDist());

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
