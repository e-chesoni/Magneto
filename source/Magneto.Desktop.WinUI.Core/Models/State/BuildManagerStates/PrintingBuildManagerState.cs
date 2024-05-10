using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Magneto.Desktop.WinUI.Core.Contracts.Services.StateMachineServices;
using Magneto.Desktop.WinUI.Core.Models.BuildModels;
using Magneto.Desktop.WinUI.Core.Models.Controllers;
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
        //_ = Draw();

        // TODO: Test build manager queue
        _ = TestDraw();
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

    public async Task TestDraw()
    {
        // Each motor should move 4 times in test
        for (var i = 0; i < 5; i++)
        {
            //_BuildManagerSM.buildController.AddCommand(1, MotorController.CommandType.RelativeMove, -2);
            //_BuildManagerSM.buildController.AddCommand(2, MotorController.CommandType.RelativeMove, -2);

            _BuildManagerSM.AddCommand(BuildManager.ControllerType.BUILD, 1, BuildManager.CommandType.RelativeMove, -2);
            _BuildManagerSM.AddCommand(BuildManager.ControllerType.BUILD, 2, BuildManager.CommandType.RelativeMove, -2);
        }
    }

    public async Task Draw()
    {
        // TODO: Find a way to set flag using interrupts (if user wants to pause/cancel print)
        var msg = "";

        // TODO: MOVE TO CALIBRATE STATE: This should be only method in calibrate motors to start
        // INFO: dance count = total slices. this number is obtained when user clicks "find print"
        // In testing, the number of slices is set by ImageHandler
        // ImageHandler references Magneto Config to get slice number

        // Get print height
        var printHeight = MagnetoConfig.GetDefaultPrintThickness() * _BuildManagerSM.danceModel.dance.Count;

        // Set the current print height on the build manager
        // TODO: Why do you need to set the current height on the build manager?
        // Is it every referenced?
        // Could reference later to display height on print page
        _BuildManagerSM.SetCurrentPrintHeight(printHeight);

        // Log print height
        msg = $"Print Height: {printHeight}";
        MagnetoLogger.Log(msg, Contracts.Services.LogFactoryLogLevel.LogLevel.VERBOSE);

        // Log number of print layers
        msg = $"Print layers: {_BuildManagerSM.danceModel.dance.Count}";
        MagnetoLogger.Log(msg, Contracts.Services.LogFactoryLogLevel.LogLevel.VERBOSE);

        // TODO: Let user calibrate build start height

        // Move build plate down to start position: total print height + build plate thickness
        double build_plate_thickness = 5;

        // TODO: Change to controller.AddCommand(axis, moveType, dist);

        //var a = _BuildManagerSM.buildController.GetBuildMotor().GetAxis();

        await _BuildManagerSM.buildController.MoveMotorAbsAsync(_BuildManagerSM.buildController.GetBuildMotor(), -(build_plate_thickness + MagnetoConfig.GetDefaultPrintThickness()));

        // Move powder motor down to start position
        await _BuildManagerSM.buildController.MoveMotorAbsAsync(_BuildManagerSM.buildController.GetPowderMotor(), -printHeight);

        // Sweep motor moves to starting position (home)
        //_ = _BuildManagerSM.sweepController.MoveMotorAbsAsync(_BuildManagerSM.sweepController.GetSweepMotor(), _BuildManagerSM.sweepController.GetSweepMotor().GetHomePos());

        // TODO: Let user calibrate

        // TODO: Go to build print when user clicks 'Done' in calibrate state
        
        // Keep track of loop
        var ctr = 0;

        // Dance move = slice + shape
        foreach(var move in _BuildManagerSM.danceModel.dance)
        {
            // Increment loop counter
            ctr++;

            // Log loop count
            msg = $"Executing print loop {ctr}";
            MagnetoLogger.Log(msg, Contracts.Services.LogFactoryLogLevel.LogLevel.VERBOSE);
            
            // Get motor thickness from Pose.move
            var thickness = move.thickness;
            msg = $"Layer Thickness: {thickness}";
            MagnetoLogger.Log(msg, Contracts.Services.LogFactoryLogLevel.LogLevel.VERBOSE);
            
            // TODO: Use slice information to coordinate laser when image is incorporated
            var slice = move.slice;

            switch (_BuildManagerSM.build_flag)
            {
                case BuildManager.BuildFlag.RESUME:
                    // TODO: Add artificial wait for draw before laser has been incorporated--will have to wait for user response (button) or timeout (abort)
                    // TODO: Add pop-up for user to execute draw, and indicate draw is complete

                    _BuildManagerSM.laserController.Draw(slice); // this was in original code

                    await _BuildManagerSM.buildController.MoveMotorRelAsync(_BuildManagerSM.buildController.GetBuildMotor(), -thickness);

                    await _BuildManagerSM.buildController.MoveMotorRelAsync(_BuildManagerSM.buildController.GetPowderMotor(), thickness);

                    // Sweep
                    //await _BuildManagerSM.sweepController.MoveMotorAbsAsync(_BuildManagerSM.sweepController.GetSweepMotor(), _BuildManagerSM.GetSweepDist());
                    //await _BuildManagerSM.sweepController.MoveMotorAbsAsync(_BuildManagerSM.sweepController.GetSweepMotor(), -_BuildManagerSM.GetSweepDist());

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
