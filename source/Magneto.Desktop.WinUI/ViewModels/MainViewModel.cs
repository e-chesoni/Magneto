using System.Collections.ObjectModel;
using System.Windows.Input;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Magneto.Desktop.WinUI.Contracts.Services;
using Magneto.Desktop.WinUI.Contracts.ViewModels;
using Magneto.Desktop.WinUI.Core.Contracts.Services;
using Magneto.Desktop.WinUI.Core.Models;
using Magneto.Desktop.WinUI.Core.Models.BuildModels;
using Magneto.Desktop.WinUI.Core.Models.Controllers;

namespace Magneto.Desktop.WinUI.ViewModels;

public class MainViewModel : ObservableRecipient, INavigationAware
{
    private readonly INavigationService _navigationService;
    private readonly ISampleDataService _sampleDataService;
    private readonly ISamplePrintService _samplePrintService;

    private static MotorController _buildController = new();
    private static MotorController _sweepController = new();
    private static LaserController _laserController = new();

    private BuildManager bm = new BuildManager
    {
        buildController = _buildController,
        sweepController = _sweepController,
        laserController = _laserController
    };

    private void InitBuildController(MotorController buildController)
    {
        _buildManager.SetBuildController(buildController);
    }

    private void InitLaserController()
    {
    
    }

    private BuildManager _buildManager = new BuildManager(new MotorController(), new MotorController(), new LaserController());

    public MissionControl missionControl = new MissionControl();

    public ICommand ItemClickCommand
    {
        get;
    }

    public ObservableCollection<SampleOrder> Source { get; } = new ObservableCollection<SampleOrder>();

    public MainViewModel(INavigationService navigationService, ISampleDataService sampleDataService, ISamplePrintService samplePrintService)
    {
        _navigationService = navigationService;
        _sampleDataService = sampleDataService;
        _samplePrintService = samplePrintService;

        ItemClickCommand = new RelayCommand<SampleOrder>(OnItemClick);
    }

    public async void OnNavigatedTo(object parameter)
    {
        Source.Clear();

        // TODO: Replace with real data.
        var data = await _sampleDataService.GetContentGridDataAsync();
        foreach (var item in data)
        {
            Source.Add(item);
        }
    }

    public void OnNavigatedFrom()
    {
    }

    private void OnItemClick(SampleOrder? clickedItem)
    {
        if (clickedItem != null)
        {
            _navigationService.SetListDataItemForNextConnectedAnimation(clickedItem);
            _navigationService.NavigateTo(typeof(MainDetailViewModel).FullName!, clickedItem.OrderID);
        }
    }
}
