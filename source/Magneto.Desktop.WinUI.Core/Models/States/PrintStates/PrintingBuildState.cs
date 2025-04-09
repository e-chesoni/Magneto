using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Magneto.Desktop.WinUI.Core.Models.BuildModels;
using Magneto.Desktop.WinUI.Core.Models.Controllers;
using Magneto.Desktop.WinUI.Core.Models.Artifact;
using Magneto.Desktop.WinUI.Core.Models.Motor;
using Magneto.Desktop.WinUI.Core.Services;
using Magneto.Desktop.WinUI.Core.Contracts.Services.States;


namespace Magneto.Desktop.WinUI.Core.Models.State.PrintStates;
public class PrintingBuildState : IPrintState
{
    private ActuationManager _BuildManagerSM { get; set; }

    public PrintingBuildState(ActuationManager bm)
    {
        var msg = "Entered PrintingBuildState...";
        MagnetoLogger.Log(msg, Contracts.Services.LogFactoryLogLevel.LogLevel.VERBOSE);

        // Set state build manger to build manager passed in on initialization
        _BuildManagerSM = bm;

        // Set the build flag to resume
        _BuildManagerSM.build_flag = ActuationManager.BuildFlag.RESUME;

        // Start drawing
        _ = Draw();
    }

    public void Cancel()
    {
        MagnetoLogger.Log("Cancel flag triggered!", Contracts.Services.LogFactoryLogLevel.LogLevel.WARN);
        _BuildManagerSM.build_flag = ActuationManager.BuildFlag.CANCEL;
        _BuildManagerSM.TransitionTo(new CancelledBuildState(_BuildManagerSM));
    }

    public void Pause()
    {
        MagnetoLogger.Log("Pause flag triggered!", Contracts.Services.LogFactoryLogLevel.LogLevel.WARN);
        _BuildManagerSM.build_flag = ActuationManager.BuildFlag.PAUSE;
        _BuildManagerSM.TransitionTo(new PausedBuildState(_BuildManagerSM));
    }

    public void Start(ArtifactModel im) => throw new NotImplementedException();

    public async Task Draw()
    {
        var msg = "";

        // TODO: MOVE TO CALIBRATE STATE: This should be only method in calibrate motors to start
        // INFO: Dance count = total slices. this number is obtained when user clicks "find print"
        // INFO: In testing, the number of slices is set by ArtifactHandler
        // INFO: ArtifactHandler references Magneto Config to get default slice number

        // Calculate artifact height
        var artifactHeight = MagnetoConfig.GetDefaultPrintThickness() * _BuildManagerSM.danceModel.dance.Count;

        // Get axes
        var sweepAxis = _BuildManagerSM.sweepController.GetSweepMotor().GetAxis();
        var powderAxis = _BuildManagerSM.buildController.GetPowderMotor().GetAxis();
        var buildAxis = _BuildManagerSM.buildController.GetBuildMotor().GetAxis();

        // Set the current print height on the build manager so we can display on print page
        _BuildManagerSM.SetCurrentPrintHeight(artifactHeight);

        // Log print height
        msg = $"Print Height: {artifactHeight}";
        MagnetoLogger.Log(msg, Contracts.Services.LogFactoryLogLevel.LogLevel.VERBOSE);

        // Log number of print layers
        msg = $"Print layers: {_BuildManagerSM.danceModel.dance.Count}";
        MagnetoLogger.Log(msg, Contracts.Services.LogFactoryLogLevel.LogLevel.VERBOSE);

        // Dance Routine:
        // 1. Move motors to calculated start position
        // 2. open adjustment screen
        // 3. allow user to adjust
        // 4. once user clicks 'done' close adjustment screen
        // 5. start dance
        // INTERRUPT dance if cancel is clicked

        // Move powder motor to start height
        // TODO: TEST ME
        // For now, home powder motor (to bottom)
        // in production, user will adjust this to the desired start height for powder distribution
        _ = _BuildManagerSM.AddCommand(ActuationManager.ControllerType.BUILD, powderAxis, ActuationManager.CommandType.AbsoluteMove, _BuildManagerSM.GetPowderMotor().GetHomePos());

        // TODO: Remove 2mm added for testing in production
        _ = _BuildManagerSM.AddCommand(ActuationManager.ControllerType.BUILD, powderAxis, ActuationManager.CommandType.RelativeMove, -(artifactHeight + 2)); // add 2mm for test so we can also test homing after print

        // Move build motor down print height + plate thickness (6mm + print height)
        var plateThickness = 6; // about 6mm
        _ = _BuildManagerSM.AddCommand(ActuationManager.ControllerType.BUILD, buildAxis, ActuationManager.CommandType.RelativeMove, -(artifactHeight + plateThickness));

        // TODO: Let user calibrate build start height

        // Initialize layer counter for logs
        var layerCtr = 0;

        foreach(var pose in _BuildManagerSM.danceModel.dance) // Dance pose = slice + shape
        {
            layerCtr++;

            // Print some information abut the current layer
            msg = $"Printing layer {layerCtr} with thickness {pose.thickness}";
            MagnetoLogger.Log(msg, Contracts.Services.LogFactoryLogLevel.LogLevel.VERBOSE);


            switch (_BuildManagerSM.build_flag)
            {
                case ActuationManager.BuildFlag.RESUME: // Build flag is set to resume in constructor
                    
                    // dance loop:
                    // 1. sweep
                    // 2. run laser
                    // 3. pose powder up
                    // 4. pose build down
                    // REPEAT until all slices have been processed

                    // Perform sweep
                    _ = _BuildManagerSM.AddCommand(ActuationManager.ControllerType.SWEEP, sweepAxis, ActuationManager.CommandType.AbsoluteMove, 280); // Test 280 first; e.o.p is actually 283 (max travel is 284.5)
                    _ = _BuildManagerSM.AddCommand(ActuationManager.ControllerType.SWEEP, sweepAxis, ActuationManager.CommandType.AbsoluteMove, _BuildManagerSM.GetSweepMotor().GetHomePos()); // sweep home position is set when mission control is initialized (value est. in Magneto Config)

                    // Get current slice (to pass to laser controller in switch)
                    var slice = pose.slice;
                    // TODO: Pass slice to laser controller
                    // TODO: Add artificial wait for laser drawing (?)
                    // TODO: Set LASER_OPERATING flag to true
                    // TODO: While LASER_OPERATING flag = true, poll laser
                    // TODO: WaveRunner should be able to set LASER_OPERATING flag is true
                    // TODO: break loop when LASER_OPERATING flag is false

                    // After calibration, powder motor moves up slice thickness
                    _ = _BuildManagerSM.AddCommand(ActuationManager.ControllerType.BUILD, powderAxis, ActuationManager.CommandType.RelativeMove, pose.thickness); // Currently uses default thickness set in IdleBuildState (value est. in Magneto Config)

                    // Build motor moves down slice thickness
                    _ = _BuildManagerSM.AddCommand(ActuationManager.ControllerType.BUILD, buildAxis, ActuationManager.CommandType.RelativeMove, -pose.thickness); // TODO: test you don't need to add () to pose.thickness here...

                    break;

                // TODO: Test interrupts
                case ActuationManager.BuildFlag.PAUSE:
                    Pause();
                    break;

                case ActuationManager.BuildFlag.CANCEL:
                    Cancel();
                    break;

                default:
                    Cancel();
                    break;
            }
        }
        _BuildManagerSM.TransitionTo(new DoneBuildState(_BuildManagerSM));
    }

    public void Resume() => throw new NotImplementedException();
    
    public void Done() => throw new NotImplementedException();

    public async Task Homing()
    {

        var powder_axis = _BuildManagerSM.buildController.GetPowderMotor().GetAxis();
        var build_axis = _BuildManagerSM.buildController.GetBuildMotor().GetAxis();

        _ = _BuildManagerSM.AddCommand(ActuationManager.ControllerType.BUILD, powder_axis, ActuationManager.CommandType.AbsoluteMove, 0);
        _ = _BuildManagerSM.AddCommand(ActuationManager.ControllerType.BUILD, build_axis, ActuationManager.CommandType.AbsoluteMove, 0);

        // Return to idle state
        _BuildManagerSM.TransitionTo(new IdleBuildState(_BuildManagerSM));
    }
}
