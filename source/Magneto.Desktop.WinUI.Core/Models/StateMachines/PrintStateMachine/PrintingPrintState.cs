using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Magneto.Desktop.WinUI.Core.Models.Print;
using Magneto.Desktop.WinUI.Core.Models.Controllers;
using Magneto.Desktop.WinUI.Core.Models.Artifact;
using Magneto.Desktop.WinUI.Core.Models.Motors;
using Magneto.Desktop.WinUI.Core.Contracts.Services.States;
using Magneto.Desktop.WinUI.Core.Models.States.PrintStates;
using static Magneto.Desktop.WinUI.Core.Models.States.PrintStates.PrintStateMachine;
using Magneto.Desktop.WinUI.Core.Services;
using Magneto.Desktop.WinUI.Core.Contracts.Services;


namespace Magneto.Desktop.WinUI.Core.Models.State.PrintStates;
public class PrintingPrintState : IPrintState
{
    private PrintStateMachine _psm;
    private RoutineStateMachine _rsm;
    private IMotorService _motorService;
    private double SWEEP_CLEARANCE = 2;
    public PrintingPrintState(PrintStateMachine psm)
    {
        _psm = psm;
        _rsm = _psm.rsm;
        _motorService = _psm.motorService;
    }
    public async Task InitializePlayAsync()
    {
        await Play();
    }
    public async Task Play()
    {
        await ExecuteLayerMove();
    }
    public async Task ExecuteLayerMove()
    {
        var buildMotor = _motorService.GetBuildMotor();
        var powderMotor = _motorService.GetPowderMotor();
        var sweepMotor = _motorService.GetSweepMotor();
        var thickness = CurrentLayerSettings.thickness;
        var amplifier = CurrentLayerSettings.amplifier;
        var clearance = SWEEP_CLEARANCE;
        var movePositive = true;

        // Enable rsm again
        _rsm.CANCELLATION_REQUESTED = false;
        _rsm.status = RoutineStateMachine.RoutineStateMachineStatus.Processing;

        // read and clear errors
        await _motorService.ReadAndClearAllErrors();

        // move build and supply motors down so sweep motor can pass
        var lowerBuildClearance = _rsm.WriteRelativeMoveProgramForBuildMotor(clearance, !movePositive);
        var lowerPowderClearance = _rsm.WriteRelativeMoveProgramForPowderMotor(clearance, !movePositive);
        // home sweep motor
        var homeSweep = _rsm.WriteAbsoluteMoveProgramForSweepMotor(sweepMotor.GetHomePos()); // sweep moves home first
        // raise build and supply motors by clearance
        var raiseBuildClearance = _rsm.WriteRelativeMoveProgramForBuildMotor(clearance, movePositive);
        var raisePowderClearance = _rsm.WriteRelativeMoveProgramForPowderMotor(clearance, movePositive);
        // TODO: raise supply by (amplifier * thickness)
        // TODO: lower build by thickness
        var raiseSupplyLayer = _rsm.WriteRelativeMoveProgramForPowderMotor((thickness * amplifier), movePositive);
        var lowerBuildLayer = _rsm.WriteRelativeMoveProgramForBuildMotor(thickness, !movePositive);
        // spread powder
        var spreadPowder = _rsm.WriteAbsoluteMoveProgramForSweepMotor(sweepMotor.GetMaxPos()); // then to max position

        // Add commands to program list
        // lower clearance
        _rsm.AddProgramLast(buildMotor.GetMotorName(), lowerBuildClearance);
        _rsm.AddProgramLast(powderMotor.GetMotorName(), lowerPowderClearance);
        // home sweep
        _rsm.AddProgramLast(sweepMotor.GetMotorName(), homeSweep);
        // raise clearance
        _rsm.AddProgramLast(buildMotor.GetMotorName(), raiseBuildClearance);
        _rsm.AddProgramLast(powderMotor.GetMotorName(), raisePowderClearance);
        // move motors for layer
        _rsm.AddProgramLast(powderMotor.GetMotorName(), raiseSupplyLayer);
        _rsm.AddProgramLast(buildMotor.GetMotorName(), lowerBuildLayer);
        // spread powder
        _rsm.AddProgramLast(sweepMotor.GetMotorName(), spreadPowder);

        await _rsm.Process();
    }
    public void Pause() => ChangeStateTo(new PausedPrintState(_psm));
    public void Redo() => throw new NotImplementedException();
    public void Cancel() => throw new NotImplementedException();
    public void ChangeStateTo(IPrintState state) => _psm.ChangeStateTo(state);
}
