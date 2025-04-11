using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Magneto.Desktop.WinUI.Core.Contracts.Services.Database.Seeders;
using Magneto.Desktop.WinUI.Core.Contracts.Services.Database;
using Magneto.Desktop.WinUI.Core.Models.Print.Database;
using Magneto.Desktop.WinUI.Core.Services;
using Microsoft.UI.Xaml;
using System.Diagnostics;

namespace Magneto.Desktop.WinUI.ViewModels;

public class TestPrintViewModel : ObservableRecipient
{
    #region Private Variables
    private readonly IPrintService _printService;
    private readonly ISliceService _sliceService;
    private readonly IPrintSeeder _seeder;
    private string? _distanceText;
    //private double? _distance;
    private string? _positionText;
    //private double? _position;
    #endregion

    #region Public Variables
    public ObservableCollection<SliceModel> sliceCollection { get; } = new ObservableCollection<SliceModel>();
    public PrintModel? currentPrint = new();
    public SliceModel? currentSlice = new();
    #endregion

    public TestPrintViewModel(IPrintSeeder seeder, IPrintService printService, ISliceService sliceService)
    {
        // Set default distance to move motors
        //SetDistance(10);
        _printService = printService;
        _sliceService = sliceService;
        _seeder = seeder;
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

    #region Setters
    public async Task SetCurrentPrintAsync(string directoryPath)
    {
        currentPrint = await _printService.GetPrintByDirectory(directoryPath);
        await UpdateSlicesHelper(); // ✅ await this now
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
    #endregion

    #region Helpers
    private async Task UpdateSlicesHelper()
    {
        sliceCollection.Clear();
        await LoadSliceDataAsync();
        currentSlice = await GetNextSliceAsync();
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

    #region Print Methods
    public async Task MarkSliceAsync()
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

        currentSlice.marked = true;
        await _sliceService.EditSlice(currentSlice);

        await UpdateSlicesHelper(); // this should update currentSlice
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
