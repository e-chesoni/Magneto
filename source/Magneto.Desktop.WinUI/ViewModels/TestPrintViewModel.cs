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

namespace Magneto.Desktop.WinUI.ViewModels;

public class TestPrintViewModel : ObservableRecipient
{
    #region Private Variables
    private readonly IPrintService _printService;
    private readonly ISliceService _sliceService;
    private readonly IPrintSeeder _seeder;
    private readonly IMotorService _motorService;
    private readonly IWaverunnerService _waverunnerService;
    // TODO: Add motor service
    #endregion

    #region Public Variables
    public ObservableCollection<SliceModel> sliceCollection { get; } = new ObservableCollection<SliceModel>();
    public PrintModel? currentPrint = new();
    public SliceModel? currentSlice = new();
    #endregion

    public TestPrintViewModel(IPrintSeeder seeder, IPrintService printService, ISliceService sliceService, IMotorService motorService, IWaverunnerService waverunnerService)
    {
        _printService = printService;
        _sliceService = sliceService;
        _seeder = seeder;
        _motorService = motorService;
        _waverunnerService = waverunnerService;
    }
    public void TestWaverunnerConnection()
    {
        _waverunnerService.TestConnection();
    }

    #region Setters
    public async Task SetCurrentPrintAsync(string directoryPath)
    {
        currentPrint = await _printService.GetPrintByDirectory(directoryPath);
        await GetNextSliceAndUpdateDisplay(); // ✅ await this now
    }
    #endregion

    #region Getters
    private async Task<SliceModel?> GetNextSliceAsync()
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
        return await _printService.MarkedOrUnmarkedCount(currentPrint.id);
    }
    public async Task<long> GetTotalSlicesAsync()
    {
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
    #endregion

    #region Access CRUD Methods
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
            await _seeder.CreatePrintFromDirectory(fullPath);
        }

        // set print on view model
        await SetCurrentPrintAsync(fullPath); // calls update slices

        return;
    }
    private async Task CompleteCurrentPrintAsync()
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
    public async Task DeleteCurrentPrintAsync()
    {
        await _printService.DeletePrint(currentPrint); // deletes slices associated with print
    }
    #endregion

    #region Page Data Management
    public async Task LoadSliceDataAsync()
    {
        sliceCollection.Clear();
        try
        {
            // check for null values
            if (currentPrint == null)
            {
                MagnetoLogger.Log("❌ No print found in DB.", LogFactoryLogLevel.LogLevel.ERROR);
                return;
            }

            if (string.IsNullOrEmpty(currentPrint.id))
            {
                MagnetoLogger.Log("❌ Print ID is null or empty.", LogFactoryLogLevel.LogLevel.ERROR);
                return;
            }

            Debug.WriteLine("✅Getting slices.");
            // use print service to get slices
            var slices = await _printService.GetSlicesByPrintId(currentPrint.id);
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
        currentPrint = null;
        currentSlice = null;
    }
    #endregion

    #region Helpers
    private async Task GetNextSliceAndUpdateDisplay()
    {
        sliceCollection.Clear();
        await LoadSliceDataAsync();
        currentSlice = await GetNextSliceAsync();
    }
    #endregion

    #region Print Methods
    // TODO: in the future should we be able to pass a full slice to this method?
    private async Task UpdateSliceCollectionAsync(double thickness, double power, double scanSpeed, double hatchSpacing)
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
        currentSlice.energyDensity = Math.Round(power / (thickness * scanSpeed * hatchSpacing),2);
        currentSlice.marked = true;
        await _sliceService.EditSlice(currentSlice);
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

    public async Task<int>PrintLayer(MotorPageService motorPageService, bool startWithMark, double thickness, double power, double scanSpeed, double hatchSpacing, double amplifier, XamlRoot xamlRoot)
    {
        string msg;
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
            //await _motorService.LayerMove(thickness, amplifier);
            await motorPageService.ExecuteLayerMove(thickness, amplifier, xamlRoot);

            // wait for layer move to complete
            //while (motorPageService.MotorsRunning()) { await Task.Delay(100); }
        }
        else
        {
            // layer move
            //await _motorService.LayerMove(thickness, amplifier);
            await motorPageService.ExecuteLayerMove(thickness, amplifier, xamlRoot);
            // wait for layer move to complete
            //while (_motorService.MotorsRunning()) { await Task.Delay(100); }
            //while (motorPageService.MotorsRunning()) { await Task.Delay(100); }

            // mark
            //await HandleMarkEntityAsync();
            // wait for mark to complete
            //while (_waverunnerService.GetMarkStatus() != 0) { Task.Delay(100).Wait(); }

        }
        await UpdateSliceCollectionAsync(thickness, power, scanSpeed, hatchSpacing);
        await GetNextSliceAndUpdateDisplay(); // this should update currentSlice
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
