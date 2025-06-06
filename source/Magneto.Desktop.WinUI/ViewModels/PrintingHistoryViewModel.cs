using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;

using Magneto.Desktop.WinUI.Contracts.ViewModels;
using Magneto.Desktop.WinUI.Core.Contracts.Services;
using Magneto.Desktop.WinUI.Core.Contracts.Services.Database;
using Magneto.Desktop.WinUI.Core.Models;
using Magneto.Desktop.WinUI.Core.Models.Print.Database;
using Magneto.Desktop.WinUI.ViewModels.DisplayModels;

namespace Magneto.Desktop.WinUI.ViewModels;

public class PrintingHistoryViewModel : ObservableRecipient, INavigationAware
{
    //private readonly ISampleDataService _sampleDataService;
    private readonly IPrintService _printService;
    public ObservableCollection<PrintDisplayModel> printCollection { get; } = new ObservableCollection<PrintDisplayModel>();
    //public ObservableCollection<SampleOrder> Source { get; } = new ObservableCollection<SampleOrder>();

    public PrintingHistoryViewModel(IPrintService printService)
    {
        _printService = printService;
    }

    public async void OnNavigatedTo(object parameter)
    {
        printCollection.Clear();

        try
        {
            var prints = await _printService.GetAllPrints();

            var displayModels = prints.Select(p => new PrintDisplayModel
            {
                id = p.id,
                name = p.name,
                directoryPath = p.directoryPath,
                duration = p.duration?.ToString(@"hh\:mm\:ss") ?? "",
                startTimeLocal = p.startTime.ToLocalTime().ToString("MM/dd/yyyy HH:mm:ss"),
                endTimeLocal = p.endTime?.ToLocalTime().ToString("MM/dd/yyyy HH:mm:ss") ?? "",
                totalSlices = p.totalSlices,
                complete = p.complete,
            }).ToList();

            foreach (var item in displayModels)
            {
                printCollection.Add(item);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error loading data: {ex.Message}");
        }
    }

    public async Task<PrintModel> GetPrintByIdAsync(string id)
    {
        return await _printService.GetPrintById(id);
    }

    public async Task DeletePrintAsync(PrintModel printModel)
    {
        // remove from database
        await _printService.DeletePrint(printModel); // deletes slices associated with print
        // Remove from UI
        var displayItem = printCollection.FirstOrDefault(p => p.id == printModel.id);
        if (displayItem != null)
        {
            printCollection.Remove(displayItem);
        }
    }

    public void OnNavigatedFrom()
    {
    }
}
