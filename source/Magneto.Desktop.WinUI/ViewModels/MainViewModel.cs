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

    /// <summary>
    /// Boolean to indicate whether to call InitializeMagneto when page loads
    /// </summary>
    private bool _initialPageLoaded = false;

    /// <summary>
    /// Tasks to handle when application starts up
    /// TODO: May want to store in an "App Startup" class in the future
    /// </summary>
    private void InitializeMagnetoPorts()
    {
        // Set log level
        MagnetoLogger.LogFactoryOutputLevel = LogFactoryOutputLevel.LogOutputLevel.Debug;

        MagnetoSerialConsole.GetAvailablePorts();

        // Get config stuff
        foreach (var c in MagnetoConfig.GetAllCOMPorts())
        {
            MagnetoSerialConsole.InitializePort(MagnetoConfig.GetCOMPortName(c), c.baudRate, c.parity, c.dataBits, c.stopBits, c.handshake);
        }

        MagnetoSerialConsole.GetInitializedPorts();

        // Set initial page loaded to true
        _initialPageLoaded = true;
    }

    private static StepperMotor _powderMotor;
    private static StepperMotor _buildMotor;
    private static StepperMotor _sweepMotor;

    private static MotorController _buildController;
    private static MotorController _sweepController;
    private static LaserController _laserController;
    private static BuildManager _buildManager;

    public MissionControl missionControl = new MissionControl(_buildManager);

    private void InitializeMagnetoComponents()
    {
        // Initialize Motors
        MagnetoMotor powderMotorConfig = MagnetoConfig.GetMotorByName("powder");
        MagnetoMotor buildMotorConfig = MagnetoConfig.GetMotorByName("build");
        MagnetoMotor sweepMotorConfig = MagnetoConfig.GetMotorByName("sweep");

        var msg = "";

        if (powderMotorConfig != null)
        {
            _powderMotor = new StepperMotor(powderMotorConfig.COMPort, powderMotorConfig.axis, powderMotorConfig.maxPos, powderMotorConfig.minPos, powderMotorConfig.homePos);
        }
        else
        {
            msg = $"Error; could not assign {powderMotorConfig} motor";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
        }

        if (buildMotorConfig != null)
        {
            _buildMotor = new StepperMotor(buildMotorConfig.COMPort, buildMotorConfig.axis, buildMotorConfig.maxPos, buildMotorConfig.minPos, buildMotorConfig.homePos);
        }
        else
        {
            msg = $"Error; could not assign {buildMotorConfig} motor";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
        }

        if (sweepMotorConfig != null)
        {
            _sweepMotor = new StepperMotor(sweepMotorConfig.COMPort, sweepMotorConfig.axis, sweepMotorConfig.maxPos, sweepMotorConfig.minPos, sweepMotorConfig.homePos);
        }
        else
        {
            msg = $"Error; could not assign {sweepMotorConfig} motor";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
        }

        _buildController = new(_powderMotor, _buildMotor);
        _sweepController = new(_sweepMotor);
        _laserController = new();

        _buildManager = new BuildManager(_buildController, _sweepController, _laserController);
        missionControl = new MissionControl(_buildManager);
    }


    public ICommand ItemClickCommand
    {
        get;
    }

    public ObservableCollection<SampleOrder> Source { get; } = new ObservableCollection<SampleOrder>();

    public MainViewModel(INavigationService navigationService, ISampleDataService sampleDataService, ISamplePrintService samplePrintService)
    {
        if (!_initialPageLoaded) 
        {
            InitializeMagnetoPorts();
            InitializeMagnetoComponents();
        }

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
