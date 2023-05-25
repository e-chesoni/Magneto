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
using Magneto.Desktop.WinUI.Core.Models.Motor;

namespace Magneto.Desktop.WinUI.ViewModels;

public class MainViewModel : ObservableRecipient, INavigationAware
{
    public INavigationService _navigationService;
    private readonly ISampleDataService _sampleDataService;
    private readonly ISamplePrintService _samplePrintService;

    private static StepperMotor _motor1 = new StepperMotor(1);
    private static StepperMotor _motor2 = new StepperMotor(2);
    private static StepperMotor _powderDistMotor = new StepperMotor(1);

    private static MotorController _buildController = new(_motor1, _motor2);
    private static MotorController _sweepController = new(_powderDistMotor);
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
