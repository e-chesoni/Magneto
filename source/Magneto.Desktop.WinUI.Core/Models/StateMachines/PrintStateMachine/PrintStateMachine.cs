using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Magneto.Desktop.WinUI.Core.Contracts.Services.Database.Seeders;
using Magneto.Desktop.WinUI.Core.Contracts.Services.Database;
using Magneto.Desktop.WinUI.Core.Contracts.Services.States;
using Magneto.Desktop.WinUI.Core.Models.Print;
using Magneto.Desktop.WinUI.Core.Models.State.PrintStates;
using Magneto.Desktop.WinUI.Core.Models.Print.Database;
using System.Collections.ObjectModel;
using Magneto.Desktop.WinUI.Core.Contracts.Services;
using Magneto.Desktop.WinUI.Core.Services;
using static Magneto.Desktop.WinUI.Core.Models.Constants.MagnetoConstants;
using static Magneto.Desktop.WinUI.Core.Models.Print.RoutineStateMachine;
using Magneto.Desktop.WinUI.Core.Models.Motors;
using Magneto.Desktop.WinUI.Core.Services.Database;

namespace Magneto.Desktop.WinUI.Core.Models.States.PrintStates;
public class PrintStateMachine
{
    private readonly IPrintService _printService;
    private readonly ISliceService _sliceService;
    private readonly IPrintSeeder _seeder;
    private IPrintState _currentState;
    private IMotorService _motorService;
    private double SWEEP_CLEARANCE = 2;

    //public ObservableCollection<SliceModel> sliceCollection { get; } = new ObservableCollection<SliceModel>(); // This should probably stay on front end
    public PrintModel? currentPrint;
    public SliceModel? currentSlice;
    public RoutineStateMachine rsm { get; set; }

    public enum PrintStateMachineStatus
    {
        Idle,
        Printing,
        Paused
    }

    public PrintStateMachine(IPrintSeeder seeder, IPrintService printService, ISliceService sliceService, RoutineStateMachine rsm, MotorService motorService)
    {
        _currentState = new IdlePrintState(this);
        _seeder = seeder;
        _printService = printService;
        _sliceService = sliceService;
        this.rsm = rsm;
        _motorService = motorService;
    }

    // Keep get print by directory in print service
    public async Task SetCurrentPrintAsync(string directoryPath)
    {
        currentPrint = await GetPrintByDirectory(directoryPath);
        currentSlice = await GetNextSliceAsync();
    }
    public async Task SetCurrentSliceAsync() => currentSlice = await GetNextSliceAsync();
    public async Task<PrintModel> GetPrintByDirectory(string directoryPath) => await _printService.GetPrintByDirectory(directoryPath);
    public PrintModel? GetCurrentPrint() => currentPrint;
    public SliceModel? GetCurrentSlice() => currentSlice;
    public RoutineStateMachine GetProgramsManager() => rsm; // temporary method TODO: remove later
    public async Task<SliceModel?> GetNextSliceAsync()
    {
        if (_sliceService == null)
        {
            MagnetoLogger.Log("❌Slice service is null.", LogFactoryLogLevel.LogLevel.ERROR);
            return null;
        }

        if (currentPrint == null)
        {
            MagnetoLogger.Log("❌Current print is null.", LogFactoryLogLevel.LogLevel.ERROR);
            return null;
        }

        var printComplete = await _printService.IsPrintComplete(currentPrint.id);

        if (printComplete)
        {
            MagnetoLogger.Log("✅Print is complete. Returning last marked slice.", LogFactoryLogLevel.LogLevel.SUCCESS);
            // update print in db
            await CompleteCurrentPrintAsync();
            return await _sliceService.GetLastSlice(currentPrint);
        }
        else
        {
            MagnetoLogger.Log("➡️Print is not complete. Returning next unmarked slice.", LogFactoryLogLevel.LogLevel.VERBOSE);
            return await _sliceService.GetNextSlice(currentPrint);
        }
    }
    public async Task<long> GetSlicesMarkedAsync() => await _printService.MarkedOrUnmarkedCount(currentPrint.id);
    public async Task<long> GetTotalSlicesAsync() => await _printService.TotalSlicesCount(currentPrint.id);
    public string GetSliceFilePath()
    {
        if (currentSlice == null)
        {
            MagnetoLogger.Log("❌Current slice is null.", LogFactoryLogLevel.LogLevel.ERROR);
            return "";
        }
        return currentSlice.filePath;
    }
    public async Task<IEnumerable<SliceModel>>? GetSlicesByPrintId(string printId) => await _printService.GetSlicesByPrintId(currentPrint.id);

    #region CRUD
    public async Task CompleteCurrentPrintAsync()
    {
        var print = currentPrint;
        if (print == null)
        {
            MagnetoLogger.Log("❌Cannot update print; print is null.", LogFactoryLogLevel.LogLevel.ERROR);
            return;
        }
        else
        {
            // update end time to now
            print.endTime = DateTime.UtcNow;
            // update print status to complete
            print.complete = true;
            // set current print to updated print
            currentPrint = print;
            // update print in db
            await _printService.EditPrint(print);
        }
    }
    public async Task DeleteCurrentPrintAsync() => await _printService.DeletePrint(currentPrint); // deletes slices associated with print
    public async Task AddPrintToDatabaseAsync(string fullPath)
    {
        // check if print already exists in db
        var existingPrint = await _printService.GetPrintByDirectory(fullPath);

        if (existingPrint != null)
        {
            MagnetoLogger.Log($"❌Print with this file path {fullPath} already exists in the database. Canceling new print.", LogFactoryLogLevel.LogLevel.ERROR);
        }
        else
        {
            // seed prints
            await _seeder.CreatePrintInMongoDb(fullPath);
        }

        // set print on view model
        await SetCurrentPrintAsync(fullPath); // calls update slices // TODO: line up with print service

        return;
    }
    public async Task UpdateSliceCollectionAsync(double thickness, double power, double scanSpeed, double hatchSpacing)
    {
        if (currentSlice == null)
        {
            MagnetoLogger.Log("❌Current slice is null.", LogFactoryLogLevel.LogLevel.ERROR);
            return;
        }

        if (currentSlice.marked)
        {
            MagnetoLogger.Log("❌Slice already marked. Canceling operation", LogFactoryLogLevel.LogLevel.ERROR);
            return;
        }
        MagnetoLogger.Log($"✅ Marking slice {currentSlice.fileName}.", LogFactoryLogLevel.LogLevel.SUCCESS);
        currentSlice.layerThickness = thickness;
        currentSlice.power = power;
        currentSlice.scanSpeed = scanSpeed;
        currentSlice.hatchSpacing = hatchSpacing;
        currentSlice.energyDensity = Math.Round(power / (thickness * scanSpeed * hatchSpacing), 2);
        currentSlice.marked = true;
        //await _sliceService.EditSlice(currentSlice);
        await _printService.EditSlice(currentSlice);
    }
    #endregion

    public void ClearCurrentPrint()
    {
        currentPrint = null;
        currentSlice = null;
    }
    public async Task NextSlice() => currentSlice = await GetNextSliceAsync();

    #region Routien Steate Machine Methods
    #region Program Getters
    public int GetNumberOfPrograms() => rsm.programNodes.Count;
    public ProgramNode? GetFirstProgramNode() => rsm.GetFirstProgramNode();
    public ProgramNode? GetLastProgramNode() => rsm.GetLastProgramNode();
    #endregion
    #endregion

    #region Write Program
    private string[] WriteAbsoluteMoveProgram(StepperMotor motor, double target) => motor.CreateMoveProgramHelper(target, true);
    public string[] WriteAbsoluteMoveProgramForBuildMotor(double target) => WriteAbsoluteMoveProgram(_motorService.GetBuildMotor(), target);
    public string[] WriteAbsoluteMoveProgramForPowderMotor(double target) => WriteAbsoluteMoveProgram(_motorService.GetPowderMotor(), target);
    public string[] WriteAbsoluteMoveProgramForSweepMotor(double target) => WriteAbsoluteMoveProgram(_motorService.GetSweepMotor(), target);

    private string[] WriteRelativeMoveProgram(StepperMotor motor, double steps, bool moveUp) => motor.CreateMoveProgramHelper(steps, false, moveUp);
    public string[] WriteRelativeMoveProgramForBuildMotor(double steps, bool moveUp) => WriteRelativeMoveProgram(_motorService.GetBuildMotor(), steps, moveUp);
    public string[] WriteRelativeMoveProgramForPowderMotor(double steps, bool moveUp) => WriteRelativeMoveProgram(_motorService.GetPowderMotor(), steps, moveUp);
    public string[] WriteRelativeMoveProgramForSweepMotor(double steps, bool moveUp) => WriteRelativeMoveProgram(_motorService.GetSweepMotor(), steps, moveUp);
    #endregion

    #region Add Program Front
    private void AddProgramFrontHelper(string[] program, Controller controller, int axis)
    {
        ProgramNode programNode = rsm.CreateProgramNode(program, controller, axis);
        rsm.AddProgramToFront(programNode);
    }
    private void AddBuildMotorProgramFront(string[] program)
    {
        var buildMotor = _motorService.GetBuildMotor();
        AddProgramFrontHelper(program, Controller.BUILD_AND_SUPPLY, buildMotor.GetAxis());
    }
    private void AddPowderMotorProgramFront(string[] program)
    {
        var powderMotor = _motorService.GetPowderMotor();
        AddProgramFrontHelper(program, Controller.BUILD_AND_SUPPLY, powderMotor.GetAxis());
    }
    private void AddSweepMotorProgramFront(string[] program)
    {
        var sweepMotor = _motorService.GetSweepMotor();
        AddProgramFrontHelper(program, Controller.SWEEP, sweepMotor.GetAxis());
    }
    public void AddProgramFront(string motorNameLower, string[] program)
    {
        switch (motorNameLower)
        {
            case "build":
                AddBuildMotorProgramFront(program);
                break;
            case "powder":
                AddPowderMotorProgramFront(program);
                break;
            case "sweep":
                AddSweepMotorProgramFront(program);
                break;
            default:
                return;
        }
    }
    #endregion

    #region Add Program Last
    private void AddProgramLastHelper(string[] program, Controller controller, int axis)
    {
        ProgramNode programNode = rsm.CreateProgramNode(program, controller, axis);
        rsm.AddProgramToBack(programNode);
    }
    private void AddBuildMotorProgramLast(string[] program)
    {
        var buildMotor = _motorService.GetBuildMotor();
        AddProgramLastHelper(program, Controller.BUILD_AND_SUPPLY, buildMotor.GetAxis());
    }
    private void AddPowderMotorProgramLast(string[] program)
    {
        var powderMotor = _motorService.GetPowderMotor();
        AddProgramLastHelper(program, Controller.BUILD_AND_SUPPLY, powderMotor.GetAxis());
    }
    private void AddSweepMotorProgramLast(string[] program)
    {
        var sweepMotor = _motorService.GetSweepMotor();
        AddProgramLastHelper(program, Controller.SWEEP, sweepMotor.GetAxis());
    }
    public void AddProgramLast(string motorNameLower, string[] program)
    {
        switch (motorNameLower)
        {
            case "build":
                AddBuildMotorProgramLast(program);
                break;
            case "powder":
                AddPowderMotorProgramLast(program);
                break;
            case "sweep":
                AddSweepMotorProgramLast(program);
                break;
            default:
                return;
        }
    }
    #endregion
    #region Pause and Resume Program
    public bool IsProgramPaused() => rsm.IsProgramPaused();
    public void PauseProgram()
    {
        rsm.PauseExecutionFlag(); // updates boolean (should stop ProcessPrograms())
        //StopAllMotorsClearProgramList();
    }
    /*
    public (double? value, bool isAbsolute) ParseMoveCommand(string[] program)
    {
        for (var i = program.Length - 1; i >= 0; i--)
        {
            var line = program[i];

            if (line.Contains("MVA") || line.Contains("MVR"))
            {
                var isAbsolute = line.Contains("MVA");
                var prefix = isAbsolute ? "MVA" : "MVR";
                var prefixIndex = line.IndexOf(prefix);

                var target = line.Substring(prefixIndex + 3); // after "MVA" or "MVR"
                if (double.TryParse(target, out var value))
                {
                    return (value, isAbsolute);
                }
                break;
            }
        }
        MagnetoLogger.Log($"No move command found.", LogFactoryLogLevel.LogLevel.ERROR);
        return (null, false);
    }
    private double CalculateTargetPosition(double startingPosition, ProgramNode programNode)
    {
        var (value, isAbsolute) = ParseMoveCommand(programNode.program);

        if (value == null)
        {
            throw new InvalidOperationException("Move command parsing failed: no value found.");
        }

        return isAbsolute ? value.Value : startingPosition + value.Value;
    }
    private double CalculateTargetPosition(LastMove lastMove)
    {
        var programNode = lastMove.programNode;
        var startingPosition = lastMove.startingPosition;
        var (value, isAbsolute) = ParseMoveCommand(programNode.program);

        if (value == null)
        {
            throw new InvalidOperationException("Move command parsing failed: no value found.");
        }

        return isAbsolute ? value.Value : startingPosition + value.Value;
    }
    */
    public async Task ResumeProgramReading()
    {
        StepperMotor motor;
        // Figure out if the last program finished:
        // get the last program node and extract its variables
        LastMove lastMove = rsm.GetLastMove();
        ProgramNode lastProgramNode = lastMove.programNode;
        (_, Controller controller, var axis) = rsm.ExtractProgramNodeVariables(lastProgramNode);
        // use controller and axis to determine which motor command was called on
        if (controller == Controller.BUILD_AND_SUPPLY)
        {
            if (axis == _motorService.GetBuildMotor().GetAxis())
            {
                motor = _motorService.GetBuildMotor();
            }
            else
            {
                motor = _motorService.GetPowderMotor();
            }
        }
        else if (controller == Controller.SWEEP)
        {
            motor = _motorService.GetSweepMotor();
        }
        else
        {
            MagnetoLogger.Log("Cannot resume reading program. No motor found.", LogFactoryLogLevel.LogLevel.ERROR);
            return;
        }
        // get the current position
        var currentPostion = await motor.GetPositionAsync(2);
        // calculate the targeted position
        var target = rsm.CalculateTargetPosition(lastMove);
        // if target is less than current position, moveUp = false
        var moveUp = target < currentPostion ? false : true;
        // if motor did not reach target, put absolute move command to move motor to target at the front of the program list
        if (currentPostion != target)
        {
            var absoluteProgram = WriteAbsoluteMoveProgram(motor, target);
            AddProgramFront(motor.GetMotorName(), absoluteProgram);
        }
        // set the pause requested flag to false
        EnableProgramProcessing();
        // resume executing process program
        await rsm.ProcessPrograms();
    }
    public void EnableProgramProcessing() => rsm.ResumeExecutionFlag(); // set the pause requested flag to false
    #endregion

    #region Multi-Motor Moves
    public (string[] program, Controller controller, int axis)? ExtractProgramNodeVariables(ProgramNode programNode) => rsm.ExtractProgramNodeVariables(programNode);
    public async Task ExecuteLayerMove(double thickness, double amplifier)
    {
        var buildMotor = _motorService.GetBuildMotor();
        var powderMotor = _motorService.GetPowderMotor();
        var sweepMotor = _motorService.GetSweepMotor();
        var clearance = SWEEP_CLEARANCE;
        var movePositive = true;

        // read and clear errors
        await _motorService.ReadAndClearAllErrors();

        // move build and supply motors down so sweep motor can pass
        var lowerBuildClearance = WriteRelativeMoveProgramForBuildMotor(clearance, !movePositive);
        var lowerPowderClearance = WriteRelativeMoveProgramForPowderMotor(clearance, !movePositive);
        // home sweep motor
        var homeSweep = WriteAbsoluteMoveProgramForSweepMotor(sweepMotor.GetHomePos()); // sweep moves home first
        // raise build and supply motors by clearance
        var raiseBuildClearance = WriteRelativeMoveProgramForBuildMotor(clearance, movePositive);
        var raisePowderClearance = WriteRelativeMoveProgramForPowderMotor(clearance, movePositive);
        // TODO: raise supply by (amplifier * thickness)
        // TODO: lower build by thickness
        var raiseSupplyLayer = WriteRelativeMoveProgramForPowderMotor((thickness * amplifier), movePositive);
        var lowerBuildLayer = WriteRelativeMoveProgramForBuildMotor(thickness, !movePositive);
        // spread powder
        var spreadPowder = WriteAbsoluteMoveProgramForSweepMotor(sweepMotor.GetMaxPos()); // then to max position

        // Add commands to program list
        // lower clearance
        AddProgramLast(buildMotor.GetMotorName(), lowerBuildClearance);
        AddProgramLast(powderMotor.GetMotorName(), lowerPowderClearance);
        // home sweep
        AddProgramLast(sweepMotor.GetMotorName(), homeSweep);
        // raise clearance
        AddProgramLast(buildMotor.GetMotorName(), raiseBuildClearance);
        AddProgramLast(powderMotor.GetMotorName(), raisePowderClearance);
        // move motors for layer
        AddProgramLast(powderMotor.GetMotorName(), raiseSupplyLayer);
        AddProgramLast(buildMotor.GetMotorName(), lowerBuildLayer);
        // spread powder
        AddProgramLast(sweepMotor.GetMotorName(), spreadPowder);

        await rsm.ProcessPrograms();
    }
    #endregion

    public void Play(PrintStateMachine context) => _currentState.Play();
    public void Pause(PrintStateMachine context) => _currentState.Pause();
    public void Redo(PrintStateMachine context) => _currentState.Redo();
    public void Cancel(PrintStateMachine context) => _currentState.Cancel();
    public void ChangeStateTo(IPrintState state) => _currentState = state;
}
