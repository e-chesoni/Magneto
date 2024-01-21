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
    #region Navigation and Data Variables

    public INavigationService _navigationService;
    private readonly ISampleDataService _sampleDataService;
    private readonly ISamplePrintService _samplePrintService;

    #endregion

    #region Public Variables

    /// <summary>
    /// powder motor
    /// </summary>
    private static StepperMotor? _powderMotor;

    /// <summary>
    /// build motor
    /// </summary>
    private static StepperMotor? _buildMotor;

    /// <summary>
    /// sweep motor
    /// </summary>
    private static StepperMotor? _sweepMotor;

    /// <summary>
    /// build controller
    /// </summary>
    private static MotorController? _buildController;

    /// <summary>
    /// sweep controller
    /// </summary>
    private static MotorController? _sweepController;

    /// <summary>
    /// laser controller
    /// </summary>
    private static LaserController? _laserController;

    /// <summary>
    /// build manager
    /// </summary>
    private static BuildManager? _buildManager;

    /// <summary>
    /// Boolean to indicate whether to call InitializeMagneto when page loads
    /// </summary>
    private bool _initialPageLoaded = false;

    #endregion

    #region Public Variables

    /// <summary>
    /// mission control
    /// </summary>
    public MissionControl missionControl;

    #endregion

    /// <summary>
    /// Tasks to handle when application starts up
    /// TODO: May want to store in an "App Startup" class in the future
    /// </summary>
    private void InitializeMagnetoPorts()
    {
        // Set log level
        MagnetoLogger.LogFactoryOutputLevel = LogFactoryOutputLevel.LogOutputLevel.Debug;

        MagnetoSerialConsole.LogAvailablePorts();

        // Get config stuff
        foreach (var c in MagnetoConfig.GetAllCOMPorts())
        {
            MagnetoSerialConsole.InitializePort(MagnetoConfig.GetCOMPortName(c), c.baudRate, c.parity, c.dataBits, c.stopBits, c.handshake);
        }

        MagnetoSerialConsole.GetInitializedPorts();

        // Set default termread value
        MagnetoSerialConsole.ClearTermRead();

        // Set initial page loaded to true
        _initialPageLoaded = true;
    }

    private void InitializeMagnetoComponents()
    {
        // Initialize Motor configs
        MagnetoMotorConfig powderMotorConfig = MagnetoConfig.GetMotorByName("powder");
        MagnetoMotorConfig buildMotorConfig = MagnetoConfig.GetMotorByName("build");
        MagnetoMotorConfig sweepMotorConfig = MagnetoConfig.GetMotorByName("sweep");

        // initialize placeholder for error messages
        var msg = "";

        if (powderMotorConfig != null)
        {
            _powderMotor = new StepperMotor(powderMotorConfig.motorName, powderMotorConfig.COMPort, powderMotorConfig.axis, powderMotorConfig.maxPos, powderMotorConfig.minPos, powderMotorConfig.homePos, powderMotorConfig.velocity);
        }
        else
        {
            msg = $"Error; could not assign {powderMotorConfig} motor";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
        }

        if (buildMotorConfig != null)
        {
            _buildMotor = new StepperMotor(buildMotorConfig.motorName, buildMotorConfig.COMPort, buildMotorConfig.axis, buildMotorConfig.maxPos, buildMotorConfig.minPos, buildMotorConfig.homePos, buildMotorConfig.velocity);
        }
        else
        {
            msg = $"Error; could not assign {buildMotorConfig} motor";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
        }

        if (sweepMotorConfig != null)
        {
            _sweepMotor = new StepperMotor(sweepMotorConfig.motorName, sweepMotorConfig.COMPort, sweepMotorConfig.axis, sweepMotorConfig.maxPos, sweepMotorConfig.minPos, sweepMotorConfig.homePos, sweepMotorConfig.velocity);
        }
        else
        {
            msg = $"Error; could not assign {sweepMotorConfig} motor";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
        }

        // Set controllers
        _buildController = new(_powderMotor, _buildMotor);
        _sweepController = new(_sweepMotor);
        _laserController = new();

        // Set build manager
        _buildManager = new BuildManager(_buildController, _sweepController, _laserController);

        // Set mission control
        missionControl = new MissionControl(_buildManager);
    }


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

        if (!_initialPageLoaded)
        {
            InitializeMagnetoPorts();
            InitializeMagnetoComponents();
        }
        
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
