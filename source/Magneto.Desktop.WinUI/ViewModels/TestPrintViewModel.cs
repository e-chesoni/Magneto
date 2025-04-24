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
            Debug.WriteLine("❌Slice service is null.");
            return null;
        }

        if (currentPrint == null)
        {
            Debug.WriteLine("❌Current print is null.");
            return null;
        }

        var printComplete = await _printService.IsPrintComplete(currentPrint.id);

        if (printComplete)
        {
            Debug.WriteLine("✅Print is complete. Returning last marked slice.");
            // update print in db
            await CompleteCurrentPrintAsync();
            return await _sliceService.GetLastSlice(currentPrint);
        }
        else
        {
            Debug.WriteLine("➡️Print is not complete. Returning next unmarked slice.");
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
            Debug.WriteLine("❌Current slice is null.");
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
            Debug.WriteLine($"❌Print with this file path {fullPath} already exists in the database. Canceling new print.");
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
            Debug.WriteLine("❌Cannot update print; print is null.");
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
                Debug.WriteLine("❌ No print found in DB.");
                return;
            }

            if (string.IsNullOrEmpty(currentPrint.id))
            {
                Debug.WriteLine("❌ Print ID is null or empty.");
                return;
            }

            Debug.WriteLine("✅Getting slices.");
            // use print service to get slices
            var slices = await _printService.GetSlicesByPrintId(currentPrint.id);
            foreach (var s in slices)
            {
                Debug.WriteLine($"Adding slice: {s.filePath}");
                sliceCollection.Add(s);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading data: {ex.Message}");
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
            Debug.WriteLine("❌Current slice is null.");
            return;
        }

        if (currentSlice.marked)
        {
            Debug.WriteLine("❌Slice already marked. Canceling operation");
            return;
        }
        Debug.WriteLine($"✅ Marking slice {currentSlice.fileName}.");
        currentSlice.layerThickness = thickness;
        currentSlice.power = power;
        currentSlice.scanSpeed = scanSpeed;
        currentSlice.hatchSpacing = hatchSpacing;
        currentSlice.energyDensity = power / (thickness * scanSpeed * hatchSpacing);
        currentSlice.marked = true;
        await _sliceService.EditSlice(currentSlice);
    }

    public async Task HandleMarkEntityAsync()
    {
        // Get entity to mark
        var entity = GetSliceFilePath();
        if (string.IsNullOrEmpty(entity))
        {
            Debug.WriteLine("❌Slice full path is null.");
            return;
        }
        Debug.WriteLine($"✅Marking slice at {entity}.");
        // TODO: TEST
        // TODO: mark slice
        //await _waverunnerService.MarkEntityAsync(entity);
    }

    public async Task PrintLayer(bool startWithMark, double thickness, double power, double scanSpeed, double hatchSpacing, double amplifier)
    {
        if (startWithMark)
        {
            // TODO: set waverunner mark parameters (not yet implemented)
            // await _waverunnerService.UpdatePen(power, scanSpeed); // calls UpdatePower(power) and UpdateScanSpeed(scanSpeed); may expand parameters in the future
            // mark
            await HandleMarkEntityAsync();
            // wait for mark to complete
            while (_waverunnerService.GetMarkStatus() != 0) { Task.Delay(100).Wait(); }
            // layer move
            await _motorService.LayerMove(thickness, amplifier);
            // wait for layer move to complete
            while (_motorService.MotorsRunning()) { await Task.Delay(100); }
        }
        else
        {
            // TODO: layer move first, then mark
        }
        await UpdateSliceCollectionAsync(thickness, power, scanSpeed, hatchSpacing);
        await GetNextSliceAndUpdateDisplay(); // this should update currentSlice
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
