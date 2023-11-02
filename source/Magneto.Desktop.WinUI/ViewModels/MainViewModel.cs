using System.Collections.ObjectModel;
using System.Windows.Input;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using Magneto.Desktop.WinUI.Contracts.Services;
using Magneto.Desktop.WinUI.Contracts.ViewModels;
using Magneto.Desktop.WinUI.Core;
using Magneto.Desktop.WinUI.Core.Contracts.Services;
using Magneto.Desktop.WinUI.Core.Models;
using Magneto.Desktop.WinUI.Core.Models.BuildModels;
using Magneto.Desktop.WinUI.Core.Models.Controllers;
using Magneto.Desktop.WinUI.Core.Models.Motor;
using Magneto.Desktop.WinUI.Core.Services;

namespace Magneto.Desktop.WinUI.ViewModels;

public class MainViewModel : ObservableRecipient, INavigationAware
{
    public INavigationService _navigationService;
    private readonly ISampleDataService _sampleDataService;
    private readonly ISamplePrintService _samplePrintService;
    private readonly IMagnetoConfig _magnetoConfig;

    // Get config


    // Create motors
    private static StepperMotor _powderMotor = new StepperMotor("COM4", 1, 35, 0, 0);
    private static StepperMotor _buildMotor = new StepperMotor("COM4", 2, 35, 0, 0);
    private static StepperMotor _sweepMotor = new StepperMotor("COM7", 1, 50, -50, 0); // Linear motor

    private static MotorController _buildController = new(_powderMotor, _buildMotor);
    private static MotorController _sweepController = new(_sweepMotor);
    private static LaserController _laserController = new();

    private static BuildManager bm = new BuildManager(_buildController, _sweepController, _laserController);

    public MissionControl missionControl = new MissionControl(bm);

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

        //var config = await _magnetoConfig.GetMotorDataAsync();
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
