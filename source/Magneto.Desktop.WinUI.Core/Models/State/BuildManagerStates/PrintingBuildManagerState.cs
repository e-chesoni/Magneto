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

        // Set state build manger to build manager passed in on initialization
        _BuildManagerSM = bm;

        // Set the build flag to resume
        _BuildManagerSM.build_flag = BuildManager.BuildFlag.RESUME;

        // TODO: Test main draw method
        // Start drawing
        _ = Draw();

        // Draw method to test build manager queue implementation -- remove once og Draw has been vetted again
        //_ = TestDraw();
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
        // dance routine:
        // 1. move motors to calculated start position
        // 2. open adjustment screen
        // 3. allow user to adjust
        // 4. once user clicks 'done' close adjustment screen
        // 5. start dance
        // INTERRUPT dance if cancel is clicked

        var sweep_axis = _BuildManagerSM.sweepController.GetSweepMotor().GetAxis();
        var powder_axis = _BuildManagerSM.buildController.GetPowderMotor().GetAxis();
        var build_axis = _BuildManagerSM.buildController.GetBuildMotor().GetAxis();
        var total_print_height = 10;

        // TODO: Add dummy calibration move
        // Loop below runs 5 times, and each motor moves 2mm per loop
        // Therefore, calibration step for the powder motor is down 2*5=10mm
        _ = _BuildManagerSM.AddCommand(BuildManager.ControllerType.BUILD, powder_axis, BuildManager.CommandType.RelativeMove, -(total_print_height + 2)); // add 2mm for test so we can also test homing after print
        
        // Move build motor down print height + plate thickness (6mm + 10mm)
        var plate_thickness = 6; // about 6mm
        _ = _BuildManagerSM.AddCommand(BuildManager.ControllerType.BUILD, build_axis, BuildManager.CommandType.RelativeMove, -(total_print_height + plate_thickness));

        // Each motor should move 5 times in test
        for (var i = 0; i < 5; i++)
        {
            // dance loop:
            // 1. sweep
            // 2. run laser
            // 3. move powder up
            // 4. move build down
            // REPEAT until all slices have been processed

            // Perform sweep
            _ = _BuildManagerSM.AddCommand(BuildManager.ControllerType.SWEEP, sweep_axis, BuildManager.CommandType.AbsoluteMove, 280); // Test 280 first; e.o.p is actually 283 (max travel is 284.5)
            _ = _BuildManagerSM.AddCommand(BuildManager.ControllerType.SWEEP, sweep_axis, BuildManager.CommandType.AbsoluteMove, 0);

            // After calibration, powder motor moves up slice thickness
            _ = _BuildManagerSM.AddCommand(BuildManager.ControllerType.BUILD, powder_axis, BuildManager.CommandType.RelativeMove, 2);

            // Build motor moves down slice thickness
            _ = _BuildManagerSM.AddCommand(BuildManager.ControllerType.BUILD, build_axis, BuildManager.CommandType.RelativeMove, -2);

            // TODO: Set LASER_OPERATING flag to true

            // TODO: While LASER_OPERATING flag = true, poll laser
            // TODO: WaveRunner should be able to set LASER_OPERATING flag is true
            // TODO: break loop when LASER_OPERATING flag is false

        }

        // Move build motor back to zero position (so we can remove build plate)
        _ = _BuildManagerSM.AddCommand(BuildManager.ControllerType.BUILD, build_axis, BuildManager.CommandType.AbsoluteMove, 0);
    }

    public void Draw()
    {
        var msg = "";

        // TODO: MOVE TO CALIBRATE STATE: This should be only method in calibrate motors to start
        // INFO: Dance count = total slices. this number is obtained when user clicks "find print"
        // INFO: In testing, the number of slices is set by ImageHandler
        // INFO: ImageHandler references Magneto Config to get slice number

        // Get print height
        var print_height = MagnetoConfig.GetDefaultPrintThickness() * _BuildManagerSM.danceModel.dance.Count;

        // Get axes
        var sweep_axis = _BuildManagerSM.sweepController.GetSweepMotor().GetAxis();
        var powder_axis = _BuildManagerSM.buildController.GetPowderMotor().GetAxis();
        var build_axis = _BuildManagerSM.buildController.GetBuildMotor().GetAxis();

        // Set the current print height on the build manager so we can display on print page
        _BuildManagerSM.SetCurrentPrintHeight(print_height);

        // Log print height
        msg = $"Print Height: {print_height}";
        MagnetoLogger.Log(msg, Contracts.Services.LogFactoryLogLevel.LogLevel.VERBOSE);

        // Log number of print layers
        msg = $"Print layers: {_BuildManagerSM.danceModel.dance.Count}";
        MagnetoLogger.Log(msg, Contracts.Services.LogFactoryLogLevel.LogLevel.VERBOSE);

        // Dance Routine:
        // 1. move motors to calculated start position
        // 2. open adjustment screen
        // 3. allow user to adjust
        // 4. once user clicks 'done' close adjustment screen
        // 5. start dance
        // INTERRUPT dance if cancel is clicked

        // Move powder motor to start height
        _ = _BuildManagerSM.AddCommand(BuildManager.ControllerType.BUILD, powder_axis, BuildManager.CommandType.RelativeMove, -(print_height + 2)); // add 2mm for test so we can also test homing after print

        // Move build motor down print height + plate thickness (6mm + print height)
        var plate_thickness = 6; // about 6mm
        _ = _BuildManagerSM.AddCommand(BuildManager.ControllerType.BUILD, build_axis, BuildManager.CommandType.RelativeMove, -(print_height + plate_thickness));

        // TODO: Let user calibrate build start height

        // Initialize layer counter for logs
        var layer_ctr = 0;

        foreach(var move in _BuildManagerSM.danceModel.dance) // Dance move = slice + shape
        {
            layer_ctr++;

            msg = $"Printing layer {layer_ctr}";
            MagnetoLogger.Log(msg, Contracts.Services.LogFactoryLogLevel.LogLevel.VERBOSE);
            
            var thickness = move.thickness; // Get motor thickness from Pose.move
            msg = $"Layer Thickness: {thickness}";
            MagnetoLogger.Log(msg, Contracts.Services.LogFactoryLogLevel.LogLevel.VERBOSE);
            
            // TODO: Use slice information to coordinate laser when image is incorporated
            var slice = move.slice;

            switch (_BuildManagerSM.build_flag)
            {
                case BuildManager.BuildFlag.RESUME: // Build flag is set to resume when Printing Build Manger State is initialized
                    
                    // dance loop:
                    // 1. sweep
                    // 2. run laser
                    // 3. move powder up
                    // 4. move build down
                    // REPEAT until all slices have been processed

                    // Perform sweep
                    _ = _BuildManagerSM.AddCommand(BuildManager.ControllerType.SWEEP, sweep_axis, BuildManager.CommandType.AbsoluteMove, 280); // Test 280 first; e.o.p is actually 283 (max travel is 284.5)
                    _ = _BuildManagerSM.AddCommand(BuildManager.ControllerType.SWEEP, sweep_axis, BuildManager.CommandType.AbsoluteMove, 0);

                    // After calibration, powder motor moves up slice thickness
                    _ = _BuildManagerSM.AddCommand(BuildManager.ControllerType.BUILD, powder_axis, BuildManager.CommandType.RelativeMove, 2);

                    // Build motor moves down slice thickness
                    _ = _BuildManagerSM.AddCommand(BuildManager.ControllerType.BUILD, build_axis, BuildManager.CommandType.RelativeMove, -2);

                    // TODO: Add artificial wait for laser drawing (?)

                    // TODO: Set LASER_OPERATING flag to true
                    // TODO: While LASER_OPERATING flag = true, poll laser
                    // TODO: WaveRunner should be able to set LASER_OPERATING flag is true
                    // TODO: break loop when LASER_OPERATING flag is false

                    break;

                // TODO: Test interrupts
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

        var powder_axis = _BuildManagerSM.buildController.GetPowderMotor().GetAxis();
        var build_axis = _BuildManagerSM.buildController.GetBuildMotor().GetAxis();

        _ = _BuildManagerSM.AddCommand(BuildManager.ControllerType.BUILD, powder_axis, BuildManager.CommandType.AbsoluteMove, 0);
        _ = _BuildManagerSM.AddCommand(BuildManager.ControllerType.BUILD, build_axis, BuildManager.CommandType.AbsoluteMove, 0);

        // Return to idle state
        _BuildManagerSM.TransitionTo(new IdleBuildManagerState(_BuildManagerSM));
    }
}
