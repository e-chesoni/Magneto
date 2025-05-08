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
    #region Services
    private readonly IPrintService _printService;
    private readonly ISliceService _sliceService;
    private readonly IPrintSeeder _seeder;
    public IMotorService motorService;
    #endregion

    #region State Machine Variables
    private IPrintState _currentState;
    public RoutineStateMachine rsm { get; set; }
    #endregion

    #region Current Print and Slice Models
    public PrintModel? currentPrint;
    public SliceModel? currentSlice;
    #endregion

    #region Settings and Status
    public static class CurrentLayerSettings
    {
        public static double thickness;
        public static double amplifier;
    }
    public enum PrintStateMachineStatus
    {
        Idle,
        Printing,
        Paused
    }
    #endregion

    private double SWEEP_CLEARANCE = 2;

    #region Constructor
    public PrintStateMachine(IPrintSeeder seeder, IPrintService printService, ISliceService sliceService, RoutineStateMachine rsm, MotorService motorService)
    {
        _currentState = new IdlePrintState(this);
        _seeder = seeder;
        _printService = printService;
        _sliceService = sliceService;
        this.rsm = rsm;
        this.motorService = motorService;
    }
    #endregion

    #region Print and Slice Model Methods
    #region Print and Slice Setters
    public async Task SetCurrentPrintAsync(string directoryPath)
    {
        currentPrint = await GetPrintByDirectory(directoryPath);
        currentSlice = await GetNextSliceAsync();
    }
    public async Task SetCurrentSliceAsync() => currentSlice = await GetNextSliceAsync();
    #endregion

    #region Print and Slice Getters
    public async Task<PrintModel> GetPrintByDirectory(string directoryPath) => await _printService.GetPrintByDirectory(directoryPath);
    public PrintModel? GetCurrentPrint() => currentPrint;
    public SliceModel? GetCurrentSlice() => currentSlice;
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
    #endregion

    #region Print and Slice CRUD
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
    public async Task DeleteCurrentPrintAsync() => await _printService.DeletePrint(currentPrint); // deletes slices associated with print
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
    
    #region Print and Slice Helpers
    public void ClearCurrentPrint()
    {
        currentPrint = null;
        currentSlice = null;
    }
    public async Task NextSlice() => currentSlice = await GetNextSliceAsync();
    #endregion
    #endregion

    #region Routine State Machine Methods
    #region Program Getters
    /*
    public int GetNumberOfPrograms() => rsm.programNodes.Count;
    public ProgramNode? GetFirstProgramNode() => rsm.GetFirstProgramNode();
    public ProgramNode? GetLastProgramNode() => rsm.GetLastProgramNode();
    */
    #endregion

    #region Program Pause and Resume
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
            if (axis == motorService.GetBuildMotor().GetAxis())
            {
                motor = motorService.GetBuildMotor();
            }
            else
            {
                motor = motorService.GetPowderMotor();
            }
        }
        else if (controller == Controller.SWEEP)
        {
            motor = motorService.GetSweepMotor();
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
            var absoluteProgram = rsm.WriteAbsoluteMoveProgram(motor, target);
            rsm.AddProgramFront(motor.GetMotorName(), absoluteProgram);
        }
        // set the pause requested flag to false
        //EnableProgramProcessing();
        // resume executing process program
        await rsm.Process();
    }
    public void EnableProgramProcessing() => rsm.ResumeExecutionFlag(); // set the pause requested flag to false
    #endregion
    #endregion

    #region Multi-Motor Move Methods
    //public (string[] program, Controller controller, int axis)? ExtractProgramNodeVariables(ProgramNode programNode) => rsm.ExtractProgramNodeVariables(programNode);
    #endregion

    #region State Machine Methods
    public async Task Play() => await _currentState.Play();
    public void Pause() => _currentState.Pause();
    public void Redo() => _currentState.Redo();
    public void Cancel() => _currentState.Cancel();
    public void ChangeStateTo(IPrintState state) => _currentState = state;
    #endregion

    #region REMOVE
    public double SetCurrentLayerThickness(double thickness) => CurrentLayerSettings.thickness = thickness; // TODO: remove
    public double SetCurrentLayerAmplifier(double amplifier) => CurrentLayerSettings.amplifier = amplifier; // TODO: remove
    public void SetCurrentPrintSettings(double thickness, double amplifier) // TODO: remove
    {
        SetCurrentLayerThickness(thickness);
        SetCurrentLayerAmplifier(amplifier);
    }
    #endregion

}
