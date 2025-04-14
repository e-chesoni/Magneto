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
    /*
    public string DistanceText
    {
        get
        {
            return _distanceText;
        }
        set
        {
            _distanceText = value;
            OnPropertyChanged(nameof(DistanceText)); // Implement INotifyPropertyChanged
        }
    }

    public string PositionText
    {
        get
        {
            return _positionText;
        }
        set
        {
            _positionText = value;
            OnPropertyChanged(nameof(PositionText)); // Implement INotifyPropertyChanged
        }
    }
    private void SetDistance(int distance)
    {
        //_distance = distance;
        //_distanceText = distance.ToString();
    }
    */

    public void TestWaverunnerConnection()
    {
        _waverunnerService.TestConnection();
    }

    #region Helpers
    public async Task<double> GetMotorPositionHelperAsync(Func<StepperMotor> getMotor)
    {
        return await _motorService.GetMotorPositionAsync(getMotor());
    }
    public void StepMotorHelper(string distanceString, bool moveUp, Func<StepperMotor> getMotor)
    {
        if (double.TryParse(distanceString, out var distance))
        {
            //_motorService.MoveMotorRel(getMotor(), distance, moveUp);
        }
        else
        {
            Debug.WriteLine("❌Could not parse distance.");
        }
    }
    public async Task StepMotorHelperAsync(string distanceString, bool moveUp, Func<StepperMotor> getMotor)
    {
        if (double.TryParse(distanceString, out var distance))
        {
            //await _motorService.MoveMotorRel(getMotor(), distance, moveUp);
        }
        else
        {
            Debug.WriteLine("❌Could not parse distance.");
        }
    }

    #endregion

    #region Getters
    public async Task<double> GetBuildMotorPositionAsync()
    {
        return await GetMotorPositionHelperAsync(_motorService.GetBuildMotor);
    }
    public async Task<double> GetPowderMotorPositionAsync()
    {
        return await GetMotorPositionHelperAsync(_motorService.GetPowderMotor);
    }
    public async Task<double> GetSweepMotorPositionAsync()
    {
        return await GetMotorPositionHelperAsync(_motorService.GetSweepMotor);
    }
    #endregion

    #region Movement
    public void StepBuildMotor(string distanceString, bool moveUp)
    {
        StepMotorHelper(distanceString, moveUp, _motorService.GetBuildMotor);
    }
    public async Task<double?> StepMotorAsync(string distanceString, bool moveUp, Func<StepperMotor> getMotor)
    {
        if (double.TryParse(distanceString, out var distance))
        {
            var motor = getMotor();
            //await _motorService.MoveMotorRel(motor, distance, moveUp);
            return await _motorService.GetMotorPositionAsync(motor);
        }
        else
        {
            MagnetoLogger.Log("❌Could not parse distance.", LogFactoryLogLevel.LogLevel.ERROR);
            return null;
        }
    }
    public Func<StepperMotor> GetBuildMotor => _motorService.GetBuildMotor;
    public Func<StepperMotor> GetPowderMotor => _motorService.GetPowderMotor;
    public Func<StepperMotor> GetSweepMotor => _motorService.GetSweepMotor;



    public void StepPowderMotor(string distanceString, bool moveUp)
    {
        StepMotorHelper(distanceString, moveUp, _motorService.GetPowderMotor);
    }
    public void StepSweepMotor(string distanceString, bool moveUp)
    {
        StepMotorHelper(distanceString, moveUp, _motorService.GetSweepMotor);
    }
    #endregion


    #region Setters
    public async Task SetCurrentPrintAsync(string directoryPath)
    {
        currentPrint = await _printService.GetPrintByDirectory(directoryPath);
        await UpdateSliceDisplay(); // ✅ await this now
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
    private async Task UpdateSliceDisplay()
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
        currentSlice.marked = true;
        await _sliceService.EditSlice(currentSlice);
    }
    // TODO: put layer movement logic here
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

    public async Task PrintLayer(bool startWithMark, double thickness, double power, double scanSpeed, double hatchSpacing)
    {
        if (startWithMark)
        {
            // TODO: set waverunner mark parameters (not yet implemented)
            // await _waverunnerService(power, scanSpeed, hatchSpacing);
            // mark
            await HandleMarkEntityAsync();
            // wait for making to finish
            while (_waverunnerService.GetMarkStatus() != 0) // wait until mark ends before proceeding
            {
                // wait
                Task.Delay(100).Wait();
            }
            // layer move (TODO: get slice thickness)
            //await _motorPageService.LayerMove(thickness);
            //while (_motorPageService.MotorsRunning()) { await Task.Delay(100); }
        }
        // TODO: update slice collection
        await UpdateSliceCollectionAsync(thickness, power, scanSpeed, hatchSpacing);
        // TODO: update display
        await UpdateSliceDisplay(); // this should update currentSlice
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
