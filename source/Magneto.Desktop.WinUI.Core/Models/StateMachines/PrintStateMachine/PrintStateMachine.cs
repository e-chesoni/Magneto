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
    public PrintStateMachineStatus status { get; set; }
    public enum PrintStateMachineStatus
    {
        Idle,
        Printing,
        Paused
    }
    public static class CurrentLayerSettings
    {
        public static double thickness;
        public static double power;
        public static double scanSpeed;
        public static double hatchSpacing;
        public static double amplifier;
        public static double sweep_clearance = 2;
    }
    #endregion

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
        currentPrint = await GetMostRecentPrintByDirectory(directoryPath);
        currentSlice = await GetNextSliceAsync();
    }
    public async Task SetCurrentSliceToNextAsync() => currentSlice = await GetNextSliceAsync();
    #endregion

    #region Print and Slice Getters
    public RoutineStateMachineStatus GetRoutineStateMachineStatus() => rsm.status;
    public bool CancelRequestedOnRoutineStateMachine() => rsm.CANCELLATION_REQUESTED;
    //public async Task<PrintModel> GetPrintByDirectory(string directoryPath) => await _printService.GetPrintByDirectory(directoryPath);
    public async Task<PrintModel> GetMostRecentPrintByDirectory(string directoryPath) => await _printService.GetMostRecentPrintByDirectory(directoryPath);
    public PrintModel GetCurrentPrint() => currentPrint;
    public SliceModel GetCurrentSlice() => currentSlice;
    public async Task<SliceModel> GetNextSliceAsync()
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
    public async Task<long> GetSlicesMarkedAsync()
    {
        if (currentPrint == null)
        {
            MagnetoLogger.Log("❌Cannot return total slices. Current print is null.", LogFactoryLogLevel.LogLevel.ERROR);
            return 0;
        }
        return await _printService.MarkedSliceCount(currentPrint.id);
    }
    public async Task<long> GetTotalSlicesAsync()
    {
        if (currentPrint == null)
        {
            MagnetoLogger.Log("❌Cannot return total slices. Current print is null.", LogFactoryLogLevel.LogLevel.ERROR);
            return 0;
        }
        return await _printService.TotalSlicesCount(currentPrint.id);
    }
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
    public async Task AddPrintToDatabaseAsync(string fullPath, bool printModeStl)
    {
        // seed prints
        await _seeder.CreatePrintInMongoDb(fullPath, printModeStl);
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
    public async Task UpdateCurrentSliceAsync()
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
        var thickness = CurrentLayerSettings.thickness;
        var power = CurrentLayerSettings.power;
        var scanSpeed = CurrentLayerSettings.scanSpeed;
        var hatchSpacing = CurrentLayerSettings.hatchSpacing;
        currentSlice.layerThickness = thickness;
        currentSlice.power = power;
        currentSlice.scanSpeed = scanSpeed;
        currentSlice.hatchSpacing = hatchSpacing;
        currentSlice.energyDensity = Math.Round(power / (thickness * scanSpeed * hatchSpacing), 2);
        currentSlice.marked = true;
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

    public bool ShouldAbortLayerMove()
    {
        if (GetRoutineStateMachineStatus() == RoutineStateMachineStatus.Paused)
        {
            MagnetoLogger.Log("⏸ Print in paused state. Aborting print.", LogFactoryLogLevel.LogLevel.WARN);
            Pause();
            return true;
        }
        if (CancelRequestedOnRoutineStateMachine())
        {
            MagnetoLogger.Log("🛑 Cancel requested. Aborting print.", LogFactoryLogLevel.LogLevel.WARN);
            Cancel();
            return true;
        }
        return false;
    }

    public void EnablePrinting()
    {
        ChangeStateTo(new IdlePrintState(this));
        rsm.EnableProcessing();
    }

    #region State Machine Methods
    public async Task<bool> Play(int numberOfLayers = 1) => await _currentState.Play();
    public async Task<bool> Resume() => await _currentState.Resume();
    public void Pause() => _currentState.Pause();
    public void Redo() => _currentState.Redo();
    /// <summary>
    /// Calls stops all motors using motor service. Calls cancel on current print state machine state.
    /// </summary>
    public void Cancel()
    {
        motorService.StopAllMotors();
        // cancel print on psm and rsm
        _currentState.Cancel(); // clears program list and places cancellation token on RSM
    }
    public void ChangeStateTo(IPrintState state) => _currentState = state;
    #endregion

    public double SetCurrentLayerThickness(double thickness) => CurrentLayerSettings.thickness = thickness;
    public double SetCurrentLayerPower(double power) => CurrentLayerSettings.power = power;
    public double SetCurrentLayerScanSpeed(double scanSpeed) => CurrentLayerSettings.scanSpeed = scanSpeed;
    public double SetCurrentLayerHatchSpacing(double hatchSpacing) => CurrentLayerSettings.hatchSpacing = hatchSpacing;
    public double SetCurrentLayerAmplifier(double amplifier) => CurrentLayerSettings.amplifier = amplifier;
    public void SetCurrentPrintSettings(double thickness, double power, double scanSpeed, double hatchSpacing, double amplifier)
    {
        SetCurrentLayerThickness(thickness);
        SetCurrentLayerPower(power);
        SetCurrentLayerScanSpeed(scanSpeed);
        SetCurrentLayerHatchSpacing(hatchSpacing);
        SetCurrentLayerAmplifier(amplifier);
    }

}
