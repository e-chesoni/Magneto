using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Magneto.Desktop.WinUI.Core.Contracts.Services.Database.Seeders;
using Magneto.Desktop.WinUI.Core.Contracts.Services.Database;
using Magneto.Desktop.WinUI.Core.Models.Print.Database;
using Microsoft.UI.Xaml;
using System.Diagnostics;
using Magneto.Desktop.WinUI.Contracts.Services;
using Magneto.Desktop.WinUI.Core.Contracts.Services;
using Magneto.Desktop.WinUI.Core.Services;
using Magneto.Desktop.WinUI.Services;
using Microsoft.UI.Xaml.Controls;
using Magneto.Desktop.WinUI.Core.Models.Motors;
using Magneto.Desktop.WinUI.Core;
using Magneto.Desktop.WinUI.Core.Models.States.PrintStates;
using Magneto.Desktop.WinUI.Core.Models.Print;
using static Magneto.Desktop.WinUI.Core.Models.Print.RoutineStateMachine;
using static Magneto.Desktop.WinUI.Core.Models.States.PrintStates.PrintStateMachine;
using Magneto.Desktop.WinUI.Popups;

namespace Magneto.Desktop.WinUI.ViewModels;

public class TestPrintViewModel : ObservableRecipient
{
    #region Private Variables
    //private readonly IPrintService _printService;
    //private readonly ISliceService _sliceService;
    private readonly IPrintSeeder _seeder;
    private readonly IMotorService _motorService;
    private readonly IWaverunnerService _waverunnerService;
    private readonly PrintStateMachine _psm;
    // TODO: Add motor service
    #endregion

    #region Public Variables
    public ObservableCollection<SliceModel> sliceCollection { get; } = new ObservableCollection<SliceModel>();
    //public PrintModel? currentPrint = new();
    //public SliceModel? currentSlice = new();
    #endregion

    public TestPrintViewModel(IPrintSeeder seeder, IPrintService printService, ISliceService sliceService, IMotorService motorService, IWaverunnerService waverunnerService)
    {
        _psm = App.GetService<PrintStateMachine>();
        /*
        _printService = printService;
        _sliceService = sliceService;
        */
        _seeder = seeder;
        _motorService = motorService;
        _waverunnerService = waverunnerService;
    }

    public bool IsPrintPaused() => _psm.status == PrintStateMachineStatus.Paused;
    public void EnablePrintStateMachinePrinting() => _psm.EnablePrinting();
    public bool CancellationRequested() => _psm.CancelRequestedOnRoutineStateMachine();

    #region Getters
    public PrintStateMachineStatus GetPrintStateMachineStatus() => _psm.status;
    public RoutineStateMachine GetRoutineStateMachine() => _psm.rsm;
    public PrintModel? GetCurrentPrint() => _psm.GetCurrentPrint();
    public SliceModel? GetCurrentSlice() => _psm.GetCurrentSlice();
    private async Task<SliceModel?> GetNextSliceAsync() => await _psm.GetNextSliceAsync();
    public async Task<long> GetSlicesMarkedAsync() => await _psm.GetSlicesMarkedAsync();
    public async Task<long> GetTotalSlicesAsync() => await _psm.GetTotalSlicesAsync();
    public string GetSliceFilePath() => _psm.GetSliceFilePath();
    #endregion

    #region Access CRUD Methods
    public async Task<bool> WasDirectoryPrinted(string fullPath)
    {
        var existingPrint = await _psm.GetMostRecentPrintByDirectory(fullPath);
        return existingPrint != null;
    }

    public async Task AddPrintToDatabaseAsync(string fullPath)
    {
        await _psm.AddPrintToDatabaseAsync(fullPath);
        return;
    }
    public async Task CompleteCurrentPrintAsync()
    {
        await _psm.CompleteCurrentPrintAsync();
    }
    public async Task DeleteCurrentPrintAsync()
    {
        //await _printService.DeletePrint(currentPrint); // deletes slices associated with print
        await _psm.DeleteCurrentPrintAsync();
    }
    #endregion

    #region Page Data Management
    public async Task DisplaySliceCollectionAsync()
    {
        sliceCollection.Clear();
        try
        {
            // check for null values
            if (_psm.GetCurrentPrint() == null)
            {
                MagnetoLogger.Log("❌ No print found in DB.", LogFactoryLogLevel.LogLevel.ERROR);
                return;
            }

            if (string.IsNullOrEmpty(_psm.GetCurrentPrint().id))
            {
                MagnetoLogger.Log("❌ Print ID is null or empty.", LogFactoryLogLevel.LogLevel.ERROR);
                return;
            }

            Debug.WriteLine("✅Getting slices.");
            // use print service to get slices
            var slices = await _psm.GetSlicesByPrintId(_psm.GetCurrentPrint().id); // should be handled by print service (not print state machine)
            foreach (var s in slices)
            {
                MagnetoLogger.Log($"Adding slice: {s.filePath}", LogFactoryLogLevel.LogLevel.VERBOSE);
                sliceCollection.Add(s);
            }
        }
        catch (Exception ex)
        {
            MagnetoLogger.Log($"Error loading data: {ex.Message}", LogFactoryLogLevel.LogLevel.ERROR);
        }
    }
    /// <summary>
    /// Clears slice collection data in view model and sets current print and current slice to null on print state machine.
    /// </summary>
    public void ClearData()
    {
        sliceCollection.Clear();
        _psm.ClearCurrentPrint();
    }
    #endregion

    #region Helpers
    public async Task GetNextSliceAndUpdateDisplay()
    {
        sliceCollection.Clear();
        await DisplaySliceCollectionAsync();
        //await _psm.SetCurrentSliceAsync();
    }
    #endregion

    #region Print Methods
    public void PausePrint()
    {
        _psm.Pause();
    }
    public async Task HandleMarkEntityAsync()
    {
        // Get entity to mark
        var entity = GetSliceFilePath();
        if (string.IsNullOrEmpty(entity))
        {
            MagnetoLogger.Log("❌Slice full path is null.", LogFactoryLogLevel.LogLevel.ERROR);
            return;
        }
        MagnetoLogger.Log($"✅Marking slice at {entity}.", LogFactoryLogLevel.LogLevel.SUCCESS);
        // TODO: TEST
        // mark slice
        await _waverunnerService.MarkEntityAsync(entity); // technically, this waits for mark to complete. second wait in PrintLayer() may be unecessary
    }

    private async Task<bool> ResumeOrStartPrintLayerAsync(bool wasPaused)
    {
        string msg;
        bool layerComplete;
        if (wasPaused)
        {
            msg = $"Print is paused. Calling resume()";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.WARN);
            layerComplete = await _psm.Resume();
        }
        else
        {
            msg = $"Print not paused. Calling play()";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.WARN);
            layerComplete = await _psm.Play();
        }
        return layerComplete;
    }

    private async void UpdateSliceIfComplete(bool layerComplete)
    {
        if (layerComplete)
        {
            //await _psm.UpdateCurrentSliceAsync(thickness, power, scanSpeed, hatchSpacing); // handles backend data
            await _psm.UpdateCurrentSliceAsync();
            await _psm.SetCurrentSliceToNextAsync(); // handles backend data
        }
        else
        {
            MagnetoLogger.Log("⚠️ Layer move was paused or canceled. Skipping print and slice update.", LogFactoryLogLevel.LogLevel.WARN);
        }
    }
    public bool ShouldAbortLayerMove() => _psm.ShouldAbortLayerMove();

    public async Task<int> MarkOnly(double power, double scanSpeed)
    {
        if (_waverunnerService.IsRunning()) // guard for at home testing
        {
            _waverunnerService.SetLaserPower(power);
            _waverunnerService.SetMarkSpeed(scanSpeed);
            await HandleMarkEntityAsync(); // waits for mark to complete in waverunner service
            return 1;
        }
        else
        {
            MagnetoLogger.Log("❌ Waverunner is not running.", LogFactoryLogLevel.LogLevel.ERROR);
            return 0;
        }
    }
    public async Task<int>PrintLayer(bool wasPaused, bool startWithMark, double thickness, double power, double scanSpeed, double hatchSpacing, double amplifier, int numberOfLayers, XamlRoot xamlRoot)
    {
        string msg;
        bool layerComplete;

        if (ShouldAbortLayerMove())
            return 0;

        if (startWithMark)
        {
            if (!wasPaused)
            {
                // mark
                if (_waverunnerService.IsRunning()) // guard for at home testing
                {
                    // update pen settings
                    _waverunnerService.SetLaserPower(power);
                    _waverunnerService.SetMarkSpeed(scanSpeed);
                    await HandleMarkEntityAsync(); // waits for mark to complete in waverunner service
                }
                else
                {
                    MagnetoLogger.Log("❌ Waverunner is not running; executing layer move only.", LogFactoryLogLevel.LogLevel.ERROR);
                }
            }
            // layer move
            _psm.SetCurrentPrintSettings(thickness, power, scanSpeed, hatchSpacing, amplifier);
            layerComplete = await ResumeOrStartPrintLayerAsync(wasPaused); // waits for move to complete in rsm.Process()
        }
        else
        {
            // layer move
            _psm.SetCurrentPrintSettings(thickness, power, scanSpeed, hatchSpacing, amplifier);
            layerComplete = await ResumeOrStartPrintLayerAsync(wasPaused);
            // mark
            // TODO: may need to handle differently when mid-mark pausing is implemented
            if (_waverunnerService.IsRunning())
            {
                await HandleMarkEntityAsync();
            }
            else
            {
                MagnetoLogger.Log("❌Waverunner is not running; executing layer move only.", LogFactoryLogLevel.LogLevel.ERROR);
            }
        }
        UpdateSliceIfComplete(layerComplete);
        return 1;
    }
    #endregion

    #region Navigation
    public async void OnNavigatedTo(object parameter)
    {
        sliceCollection.Clear();
    }
    public void OnNavigatedFrom()
    {
    }
    #endregion
}
