using System.Collections.ObjectModel;

using CommunityToolkit.Mvvm.ComponentModel;

using Magneto.Desktop.WinUI.Contracts.ViewModels;
using Magneto.Desktop.WinUI.Core.Contracts.Services;
using Magneto.Desktop.WinUI.Core.Models;

namespace Magneto.Desktop.WinUI.ViewModels;

public class MaintenanceViewModel : ObservableRecipient, INavigationAware
{
    private readonly ISampleDataService _sampleDataService;
    private SampleOrder? _selected;

    private readonly ISamplePrintService _samplePrintService;
    private SamplePrint? _printSelected;

    public SampleOrder? Selected
    {
        get => _selected;
        set => SetProperty(ref _selected, value);
    }

    public SamplePrint? PrintSelected
    {
        get => _printSelected;
        set => SetProperty(ref _printSelected, value);
    }

    public ObservableCollection<SampleOrder> SampleItems { get; private set; } = new ObservableCollection<SampleOrder>();
    public ObservableCollection<SamplePrint> SamplePrints { get; private set; } = new ObservableCollection<SamplePrint>();

    public MaintenanceViewModel(ISamplePrintService samplePrintService)
    {
        //_sampleDataService = sampleDataService;
        _samplePrintService = samplePrintService;
    }

    public async void OnNavigatedTo(object parameter)
    {
        SamplePrints.Clear();

        // TODO: Replace with real data.
        //var data = await _sampleDataService.GetListDetailsDataAsync();
        var printData = await _samplePrintService.GetListDetailsDataAsync();

        foreach (var item in printData)
        {
            SamplePrints.Add(item);
        }
    }

    public void OnNavigatedFrom()
    {
    }

    public void EnsureItemSelected()
    {
        if (PrintSelected == null)
        {
            PrintSelected = SamplePrints.First();
        }
    }
}
