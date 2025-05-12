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

    #region Getters
    public RoutineStateMachine GetRoutineStateMachine() => _psm.rsm;
    public PrintModel? GetCurrentPrint() => _psm.GetCurrentPrint();
    public SliceModel? GetCurrentSlice() => _psm.GetCurrentSlice();
    private async Task<SliceModel?> GetNextSliceAsync() => await _psm.GetNextSliceAsync();
    public async Task<long> GetSlicesMarkedAsync() => await _psm.GetSlicesMarkedAsync();
    public async Task<long> GetTotalSlicesAsync() => await _psm.GetTotalSlicesAsync();
    public string GetSliceFilePath() => _psm.GetSliceFilePath();
    #endregion

    #region Access CRUD Methods
    // TODO: move to print state machine
    public async Task AddPrintToDatabaseAsync(string fullPath)
    {
        // check if print already exists in db
        var existingPrint = await _psm.GetPrintByDirectory(fullPath);

        if (existingPrint != null)
        {
            MagnetoLogger.Log($"❌Print with this file path {fullPath} already exists in the database. Canceling new print.", LogFactoryLogLevel.LogLevel.ERROR);
        }
        else
        {
            // seed prints
            await _seeder.CreatePrintInMongoDb(fullPath);
        }

        //await SetCurrentPrintAsync(fullPath); // calls update slices
        // set current print in psm
        await _psm.SetCurrentPrintAsync(fullPath);
        return;
    }
    private async Task CompleteCurrentPrintAsync()
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
        //await _waverunnerService.MarkEntityAsync(entity);
    }

    public async Task<int>PrintLayer(bool startWithMark, double thickness, double power, double scanSpeed, double hatchSpacing, double amplifier, XamlRoot xamlRoot)
    {
        string msg;
        var layerComplete = false;
        // TODO: Uncomment laser methods after testing motor movement
        /*
        if (_waverunnerService.IsRunning() == 0)
        {
            msg = $"Waverunner is not running";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
            return 0;
        }
        */
        // update pen settings
        //_waverunnerService.SetLaserPower(power);
        //_waverunnerService.SetMarkSpeed(scanSpeed);
        if (startWithMark)
        {
            // mark
            //await HandleMarkEntityAsync();
            // wait for mark to complete
            //while (_waverunnerService.GetMarkStatus() != 0) { Task.Delay(100).Wait(); }

            // layer move
            _psm.SetCurrentPrintSettings(thickness, amplifier);
            layerComplete = await _psm.Play();
            // wait for layer move to complete
            //while (motorPageService.MotorsRunning()) { await Task.Delay(100); }
        }
        else
        {
            // layer move
            _psm.SetCurrentPrintSettings(thickness, amplifier);
            layerComplete = await _psm.Play();
            // wait for layer move to complete
            //while (_motorService.MotorsRunning()) { await Task.Delay(100); }
            //while (motorPageService.MotorsRunning()) { await Task.Delay(100); }

            // mark
            //await HandleMarkEntityAsync();
            // wait for mark to complete
            //while (_waverunnerService.GetMarkStatus() != 0) { Task.Delay(100).Wait(); }

        }
        // TODO: if psm status is paused, layer did not complete; don't update yet
        if (layerComplete)
        {
            await _psm.UpdateCurrentSliceAsync(thickness, power, scanSpeed, hatchSpacing); // handles backend data
            await _psm.SetCurrentSliceToNextAsync(); // handles backend data
        }
        else
        {
            MagnetoLogger.Log("⚠️ Layer move was paused or canceled. Skipping print and slice update.", LogFactoryLogLevel.LogLevel.WARN);
        }
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
