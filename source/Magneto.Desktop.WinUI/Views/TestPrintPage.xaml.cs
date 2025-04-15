using System.IO.Ports;
using System.Reflection;
using CommunityToolkit.WinUI.UI.Animations;
using Magneto.Desktop.WinUI.Core;
using Magneto.Desktop.WinUI.Core.Contracts.Services;
using Magneto.Desktop.WinUI.Core.Models;
using Magneto.Desktop.WinUI.Core.Models.Print;
using Magneto.Desktop.WinUI.Core.Models.Motors;
using Magneto.Desktop.WinUI.Helpers;
using Magneto.Desktop.WinUI.Models.UIControl;
using Magneto.Desktop.WinUI.Popups;
using Magneto.Desktop.WinUI.Services;
using Magneto.Desktop.WinUI.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Newtonsoft.Json.Bson;
using SAMLIGHT_CLIENT_CTRL_EXLib;
using Windows.Devices.SerialCommunication;
using static Magneto.Desktop.WinUI.Core.Models.Print.CommandQueueManager;
using static Magneto.Desktop.WinUI.Views.TestPrintPage;
using System.Diagnostics;
using Windows.Storage.Pickers;
using WinRT.Interop;
using SAMLIGHT_CLIENT_CTRL_EXLib;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Magneto.Desktop.WinUI.Views;

/// <summary>
/// Test print page
/// </summary>
public sealed partial class TestPrintPage : Page
{
    #region Motor Variables
    /*
    private StepperMotor? _powderMotor;
    private StepperMotor? _buildMotor;
    private StepperMotor? _sweepMotor;
    private ActuationManager? _am;
    */
    #endregion

    #region WaveRunner Variables

    /// <summary>
    /// WaveRunner client control interface
    /// </summary>
    private static readonly ScSamlightClientCtrlEx cci = new();

    /// <summary>
    /// Default job directory (to search for job files)
    /// </summary>
    private string? _defaultJobDirectory { get; set; }

    /// <summary>
    /// Default job file name
    /// </summary>
    private string? _defaultJobName { get; set; }

    /// <summary>
    /// Job directory (to search for files) -- can be defined by the user
    /// </summary>
    private string? _jobDirectory { get; set; }

    /// <summary>
    /// Full file path to entity
    /// </summary>
    private string? _fullJobFilePath { get; set; }

    private bool? _redPointerEnabled { get; set; }

    /// <summary>
    /// WaveRunner Execution statuses
    /// </summary>
    public enum ExecStatus
    {
        Success = 0,
        Failure = -1,
    }

    /// <summary>
    /// RedPointer Modes
    /// </summary>
    public enum RedPointerMode
    {
        IndividualOutline = 1,
        TotalOutline = 2,
        IndividualBorder = 3,
        OnlyRedPointerEntities = 4,
        OutermostBorder = 5
    }

    #endregion

    #region Layer/Print Variables

    private double _currentLayerThickness;

    private double _desiredPrintHeight;

    private double _totalLayersToPrint { get; set; }

    private int _layersPrinted { get; set; }

    private double _totalPrintHeight { get; set; }

    //private readonly Dictionary<double, int> _printHistoryDictionary = new Dictionary<double, int>();
    #endregion

    #region Services
    // TODO: make these singletons initialized in app.xaml.cs
    //private readonly IMotorService? _motorService;
    private MotorPageService? _motorPageService;
    private WaverunnerPageService? _waverunnerPageService;
    #endregion

    #region Flags

    private bool KILL_OPERATION;

    #endregion

    #region UI Helper Variables

    private MotorUIControlGroup? _calibrateMotorUIControlGroup { get; set; }
    /*
    private MotorUIControlGroup _inPrintMotorUIControlGroup { get; set; }
    private PrintSettingsUIControlGroup _printSettingsUIControlGroup { get; set; }
    private PrintSettingsUIControlGroup _layerSettingsUIControlGroup { get; set; }
    */
    private PrintUIControlGroupHelper? _printControlGroupHelper { get; set; }

    private bool _calibrationPanelEnabled = true;
    /*
    private bool _fileSettingsSectionEnabled = true;

    private bool _layerSettingsSectionEnabled = true;

    private bool _settingsPanelEnabled = true;

    private bool _printPanelEnabled = true;
    */
    #endregion

    #region Core Page Functionality Variables

    /// <summary>
    /// Central control that gets passed from page to page
    /// </summary>
    private MissionControl _missionControl { get; set; }

    /// <summary>
    /// TestPrintViewModel view model
    /// </summary>
    public TestPrintViewModel ViewModel
    {
        get;
    }

    #endregion

    #region Test Page Setup

    /// <summary>
    /// Sets up test motors for powder, build, and sweep operations by retrieving configurations 
    /// and initializing the respective StepperMotor objects. Logs success or error for each motor setup.
    /// Assumes motor order in configuration corresponds to powder, build, and sweep.
    /// </summary>
    /*
    private async void SetUpTestMotors()
    {
        if (MissionControl == null)
        {
            MagnetoLogger.Log("MissionControl is null. Unable to set up motors.", LogFactoryLogLevel.LogLevel.ERROR);
            await PopupInfo.ShowContentDialog(this.Content.XamlRoot,"Error", "MissionControl is not Connected.");
            return;
        }

        SetUpMotor("powder", MissionControl.GetPowderMotor(), out _powderMotor);
        SetUpMotor("build", MissionControl.GetBuildMotor(), out _buildMotor);
        SetUpMotor("sweep", MissionControl.GetSweepMotor(), out _sweepMotor);

        _am = MissionControl.GetActuationManger();

        //GetMotorPositions(); // TOOD: Fix--all positions are 0 on page load even if they're not...
    }
    /// <summary>
    /// Gets motor from mission control and assigns each motor to a private variable for easy access in the TestPrintPage class
    /// </summary>
    /// <param name="motorName">Motor name</param>
    /// <param name="motor">The actual stepper motor</param>
    /// <param name="motorField">Variable to assign motor to</param>
    private void SetUpMotor(string motorName, StepperMotor motor, out StepperMotor motorField)
    {
        if (motor != null)
        {
            motorField = motor;
            var msg = $"Found motor in config with name {motor.GetMotorName()}. Setting this to {motorName} motor in test.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);
        }
        else
        {
            motorField = null;
            MagnetoLogger.Log($"Unable to find {motorName} motor", LogFactoryLogLevel.LogLevel.ERROR);
        }
    }
    */
    #endregion

    #region Constructor
    /// <summary>
    /// Constructor for TestPrintPage. Initializes the ViewModel, sets up UI components, logs the page visit,
    /// retrieves configuration for build and sweep motors, and registers event handlers for their respective ports.
    /// </summary>
    public TestPrintPage()
    {
        ViewModel = App.GetService<TestPrintViewModel>();
        _missionControl = App.GetService<MissionControl>();
        InitializeComponent();
        var msg = "Landed on Test Print Page";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.DEBUG);
        MagnetoSerialConsole.LogAvailablePorts();

        // set up flags
        KILL_OPERATION = false;
        //this.motorService = motorService;
    }

    #endregion


    #region Page Initialization Methods

    private void InitPageServices() // combine page services initialization because motor services uses one of the UI groups
    {
        // UI page groups
        _calibrateMotorUIControlGroup = new MotorUIControlGroup(SelectBuildMotorButton, SelectPowderMotorButton, SelectSweepMotorButton,
                                                                BuildMotorCurrentPositionTextBox, PowderMotorCurrentPositionTextBox, SweepMotorCurrentPositionTextBox,
                                                                GetBuildMotorCurrentPositionButton, GetPowderMotorCurrentPositionButton, GetSweepMotorCurrentPositionButton,
                                                                BuildMotorAbsPositionTextBox, PowderMotorAbsPositionTextBox, SweepMotorAbsPositionTextBox, // NEW abs position text box features
                                                                BuildMotorStepTextBox, PowderMotorStepTextBox, SweepMotorStepTextBox,
                                                                StepBuildMotorUpButton, StepBuildMotorDownButton, StepPowderMotorUpButton, StepPowderMotorDownButton, StepSweepMotorLeftInCalibrateButton, StepSweepMotorRightInCalibrateButton,
                                                                StopBuildMotorButton, StopPowderMotorButton, StopSweepMotorButton,
                                                                HomeAllMotorsInCalibrationPanelButton, StopAllMotorsInCalibrationPanelButton);

        _printControlGroupHelper = new PrintUIControlGroupHelper(_calibrateMotorUIControlGroup);

        // Initialize motor page service
        _motorPageService = new MotorPageService(_missionControl.GetActuationManger(), _printControlGroupHelper);

        // initialize Waverunner page service
        _waverunnerPageService = new WaverunnerPageService(PrintDirectoryInputTextBox, PrintLayersButton);

        // Set default job file
        //_waverunnerPageService.SetDefaultJobFileName("2025-01-13_5x5_square_top_left.sjf");
        _motorPageService.HandleGetAllPositions();
    }
    /*
    private void SetDefaultPrintSettings()
    {
        _currentLayerThickness = 0.03; // set default layer height to be 0.03mm based on first steel print
        _desiredPrintHeight = 5; // set default print height to be mm
    }
    */
    #endregion


    #region Navigation Methods

    /// <summary>
    /// Handle page startup tasks
    /// </summary>
    /// <param name="e"></param>
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        // Get mission control (passed over when navigating from previous page)
        base.OnNavigatedTo(e);

        // Set mission control after navigating to new page
        //_missionControl = (MissionControl)e.Parameter;

        InitPageServices();
        //SetDefaultPrintSettings();
        _calibrationPanelEnabled = true;

        var msg = string.Format("TestPrintPage::OnNavigatedTo -- {0}", _missionControl.FriendlyMessage);
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.DEBUG);
    }

    #endregion


    #region Motor Helpers
    private async Task HomeMotorsHelper()
    {
        var msg = "Homing all motors";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);
        var buildMotor = _motorPageService.GetBuildMotor();
        var powderMotor = _motorPageService.GetPowderMotor();
        var sweepMotor = _motorPageService.GetSweepMotor();
        var buildTextBox = _motorPageService.GetBuildPositionTextBox();
        var powderTextBox = _motorPageService.GetPowderPositionTextBox();
        var sweepTextBox = _motorPageService.GetSweepPositionTextBox();
        if ((buildMotor != null) && (!_motorPageService.GetSweepMotor().STOP_MOVE_FLAG))
        {
            //_motorPageService.HandleHomeMotorAndUpdateTextBox(buildMotor, buildTextBox);
            await _motorPageService.HomeMotorAndUpdateTextBox(buildMotor);
        }
        else
        {
            MagnetoLogger.Log("Build Motor is null or stop flag is up cannot home motor.", LogFactoryLogLevel.LogLevel.ERROR);
        }

        if ((powderMotor != null) && (!_motorPageService.GetSweepMotor().STOP_MOVE_FLAG))
        {
            //_motorPageService.HandleHomeMotorAndUpdateTextBox(powderMotor, powderTextBox);
            await _motorPageService.HomeMotorAndUpdateTextBox(powderMotor);
        }
        else
        {
            MagnetoLogger.Log("Powder Motor is null or stop flag is up cannot home motor.", LogFactoryLogLevel.LogLevel.ERROR);
        }

        if ((sweepMotor != null) && (!_motorPageService.GetSweepMotor().STOP_MOVE_FLAG))
        {
            //_motorPageService.HandleHomeMotorAndUpdateTextBox(sweepMotor, sweepTextBox);
            await _motorPageService.HomeMotorAndUpdateTextBox(sweepMotor);
        }
        else
        {
            MagnetoLogger.Log("Sweep Motor is null or stop flag is up cannot home motor.", LogFactoryLogLevel.LogLevel.ERROR);
        }
    }
    #endregion

    #region Calibration Panel Methods

    private void SelectBuildMotorButton_Click(object sender, RoutedEventArgs e)
    {
        _motorPageService.printUiControlGroupHelper.SelectMotor(_motorPageService.GetBuildMotor());
    }
    private void SelectPowderMotorButton_Click(object sender, RoutedEventArgs e)
    {
        _motorPageService.printUiControlGroupHelper.SelectMotor(_motorPageService.GetPowderMotor());
    }
    private void SelectSweepMotorButton_Click(object sender, RoutedEventArgs e)
    {
        _motorPageService.printUiControlGroupHelper.SelectMotor(_motorPageService.GetSweepMotor());
    }
    private async void GetBuildMotorCurrentPositionButton_Click(object sender, RoutedEventArgs e)
    {
        await _motorPageService.HandleGetPosition(_motorPageService.GetBuildMotor(), _motorPageService.GetBuildPositionTextBox(), true);
    }
    private async void GetPowderMotorCurrentPositionButton_Click(object sender, RoutedEventArgs e)
    {
        await _motorPageService.HandleGetPosition(_motorPageService.GetPowderMotor(), _motorPageService.GetPowderPositionTextBox(), true);
    }
    private async void GetSweepMotorCurrentPositionButton_Click(object sender, RoutedEventArgs e)
    {
        await _motorPageService.HandleGetPosition(_motorPageService.GetSweepMotor(), _motorPageService.GetSweepPositionTextBox(), true);
    }
    private void MoveBuildToAbsPositionButton_Click(object sender, RoutedEventArgs e)
    {
        var buildMotor = _motorPageService.GetBuildMotor();
        _motorPageService.HandleAbsMove(buildMotor, _motorPageService.GetBuildAbsMoveTextBox(), this.Content.XamlRoot);
    }
    private void MovePowderToAbsPositionButton_Click(object sender, RoutedEventArgs e)
    {
        var powderMotor = _motorPageService.GetPowderMotor();
        _motorPageService.HandleAbsMove(powderMotor, _motorPageService.GetPowderAbsMoveTextBox(), this.Content.XamlRoot);
    }
    private void MoveSweepToAbsPositionButton_Click(object sender, RoutedEventArgs e)
    {
        var sweepMotor = _motorPageService.GetSweepMotor();
        _motorPageService.HandleAbsMove(sweepMotor, _motorPageService.GetSweepAbsMoveTextBox(), this.Content.XamlRoot);
    }
    private void StepBuildMotorUpButton_Click(object sender, RoutedEventArgs e)
    {
        MagnetoLogger.Log("step build up clicked", LogFactoryLogLevel.LogLevel.VERBOSE);
        var motor = _motorPageService.GetBuildMotor();
        _motorPageService.HandleRelMove(motor, _motorPageService.GetBuildStepTextBox(), true, this.Content.XamlRoot);
    }
    private void StepBuildMotorDownButton_Click(object sender, RoutedEventArgs e)
    {
        MagnetoLogger.Log("step build down clicked", LogFactoryLogLevel.LogLevel.VERBOSE);
        var motor = _motorPageService.GetBuildMotor();
        _motorPageService.HandleRelMove(motor, _motorPageService.GetBuildStepTextBox(), false, this.Content.XamlRoot);
    }
    private void StepPowderMotorUpButton_Click(object sender, RoutedEventArgs e)
    {
        var motor = _motorPageService.GetPowderMotor();
        _motorPageService.HandleRelMove(motor, _motorPageService.GetPowderStepTextBox(), true, this.Content.XamlRoot);
    }
    private void StepPowderMotorDownButton_Click(object sender, RoutedEventArgs e)
    {
        var motor = _motorPageService.GetPowderMotor();
        _motorPageService.HandleRelMove(motor, _motorPageService.GetPowderStepTextBox(), false, this.Content.XamlRoot);
    }
    private void StepSweepMotorLeftInCalibrateButton_Click(object sender, RoutedEventArgs e)
    {
        var motor = _motorPageService.GetSweepMotor();
        _motorPageService.HandleRelMove(motor, _motorPageService.GetSweepStepTextBox(), true, this.Content.XamlRoot);
    }
    private void StepSweepMotorRightInCalibrateButton_Click(object sender, RoutedEventArgs e)
    {
        var motor = _motorPageService.GetSweepMotor();
        _motorPageService.HandleRelMove(motor, _motorPageService.GetSweepStepTextBox(), false, this.Content.XamlRoot);
    }
    private async void HomeAllMotorsInCalibrationPanelButton_Click(object sender, RoutedEventArgs e)
    {
        await HomeMotorsHelper();
    }
    private void StopAllMotorsInCalibrationPanelButton_Click(object sender, RoutedEventArgs e)
    {
        StopMotorsHelper();
    }
    private void StopBuildMotorButton_Click(object sender, RoutedEventArgs e)
    {
        _motorPageService.StopBuildMotorAndUpdateTextBox();
    }
    private void StopPowderMotorButton_Click(object sender, RoutedEventArgs e)
    {
        _motorPageService.StopPowderMotorAndUpdateTextBox();
    }
    private void StopSweepMotorButton_Click(object sender, RoutedEventArgs e)
    {
        var sweepConfig = MagnetoConfig.GetMotorByName("sweep");
        var sweepMotor = _motorPageService.GetSweepMotor();
        MagnetoSerialConsole.SerialWrite(sweepConfig.COMPort, "1STP");
        sweepMotor.STOP_MOVE_FLAG = true;
    }
    #endregion

    #region Print Summary Methods
    /*
    // Dynamically populate a Grid
    public void PopulateGridWithLastThree(Grid targetGrid)
    {
        // Retrieve last 3 entries
        IEnumerable<KeyValuePair<double, int>> lastThreeEntries = _printHistoryDictionary.Reverse().Take(3);

        // Clear existing rows
        targetGrid.RowDefinitions.Clear();
        targetGrid.Children.Clear();

        // Add a header row
        targetGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        TextBlock header1 = new TextBlock { Text = "Layer Height", FontWeight = FontWeights.Bold, Margin = new Thickness(5) };
        Grid.SetColumn(header1, 0);
        targetGrid.Children.Add(header1);

        TextBlock header2 = new TextBlock { Text = "Number of Layers", FontWeight = FontWeights.Bold, Margin = new Thickness(5) };
        Grid.SetColumn(header2, 1);
        targetGrid.Children.Add(header2);

        // Add rows for the last 3 entries
        int rowIndex = 1;
        foreach (KeyValuePair<double, int> entry in lastThreeEntries)
        {
            targetGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            // Add Layer Height
            TextBlock layerHeightText = new TextBlock
            {
                Text = entry.Key.ToString("0.00") + " mm",
                Margin = new Thickness(5)
            };
            Grid.SetRow(layerHeightText, rowIndex);
            Grid.SetColumn(layerHeightText, 0);
            targetGrid.Children.Add(layerHeightText);

            // Add Number of Layers
            TextBlock layerCountText = new TextBlock
            {
                Text = entry.Value.ToString(),
                Margin = new Thickness(5)
            };
            Grid.SetRow(layerCountText, rowIndex);
            Grid.SetColumn(layerCountText, 1);
            targetGrid.Children.Add(layerCountText);

            rowIndex++;
        }
    }

    private void TestPrintHistoryPopulate()
    {
        PopulateGridWithLastThree(PrintHistoryGrid);
    }
    */

    // TODO: remove when done testing\
    /*
    private void AddDummyPrintHistory()
    {
        _printHistoryDictionary[0.03] = 1;  // 1 layers printed at 0.03mm
        _printHistoryDictionary[0.05] = 2;  // 2 layers printed at 0.05mm
        _printHistoryDictionary[0.08] = 3;  // 3 layers printed at 0.08mm
    }

    private void StartNewPrintButton_Click(object sender, RoutedEventArgs e)
    {
        // TODO: Remove print history dummy values when done testing
        //AddDummyPrintHistory();
        //TestPrintHistoryPopulate();

        // update layers printed
        //updateLayerTrackingUI();
        UnlockPrintManager();
    }
    private void DisplayLayersPrinted()
    {
        // Display layers printed
        _layersPrinted = _printHistoryDictionary.Any() ? _printHistoryDictionary.Values.Sum() : 0;
        LayersPrintedTextBlock.Text = _layersPrinted.ToString();
        RemainingLayersToPrint.Text = (_totalLayersToPrint - _layersPrinted).ToString();
        PopulateGridWithLastThree(PrintHistoryGrid); // TODO: Test
    }
    private void updateLayerTrackingUI()
    {
        if (_printHistoryDictionary.Count == 0)
        {
            // create a new entry for first layer; set to 0
            _printHistoryDictionary[_currentLayerThickness] = 0;
        } else { // search print history for current layer thickness
            if (!_printHistoryDictionary.ContainsKey(_currentLayerThickness)) // if no thickness, insert new entry
            {
                _printHistoryDictionary[_currentLayerThickness] = 1;
            }
            else {
                _printHistoryDictionary[_currentLayerThickness]++; // increment matching entry
            }
        }
        DisplayLayersPrinted();
    }

    private int incrementLayersPrinted()
    {
        updateLayerTrackingUI();
        return _layersPrinted;
    }

    private void CancelPrintButton_Click(object sender, RoutedEventArgs e)
    {
        _printHistoryDictionary.Clear();
    }
    */
    #endregion

    #region Print Layer Move Methods
    private void StopButton_Click(object sender, RoutedEventArgs e)
    {
        //KillAll(); // TODO: TEST; does the same as below, but has not always worked in methods
        // stop mark
        _waverunnerPageService.StopMark();
        // stop motors
        _motorPageService.StopSweepMotorAndUpdateTextBox();
        _motorPageService.StopBuildMotorAndUpdateTextBox();
        _motorPageService.StopPowderMotorAndUpdateTextBox();
    }
     #endregion

    private void WaitForMark()
    {
        _ = _waverunnerPageService.MarkEntityAsync();

        // wait until mark ends before proceeding
        while (_waverunnerPageService.GetMarkStatus() != 0)
        {
            // wait
            Task.Delay(100).Wait();
        }
    }
    private async void MultiLayerMoveButton_Click(object sender, RoutedEventArgs e)
    {
        var fullPath = "";
        var layerThickness = 0.03; // TODO: Replace with read from layerThickness text box
        var amplifier = 2; // TODO: Replace with read from amplifier text box
        if (string.IsNullOrWhiteSpace(SlicesToMarkTextBox.Text) || !int.TryParse(SlicesToMarkTextBox.Text, out var layers))
        {
            var msg = "MultiLayerMoveInputTextBox text is not a valid integer.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);

            // TODO: add pop up message for invalid input

            return; // Exit the method if the validation fails
        } else {
            // First layer of powder is laid down in calibrate, then
            var msg = "starting multilayer print...";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);

            for (var i = 0; i < layers; i++)
            {
                if (KILL_OPERATION)
                {
                    break;
                }
                
                if (StartWithMarkCheckbox.IsChecked == true)
                {
                    // MARK
                    msg = $"marking layer {i} in multi-layer print";
                    MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);
                    //_ = _waverunnerPageService.MarkEntityAsync();
                    // TOOD: TEST
                    await ViewModel.HandleMarkEntityAsync();
                    while (_waverunnerPageService.GetMarkStatus() != 0) // wait until mark ends before proceeding
                    {
                        // wait
                        Task.Delay(100).Wait();
                    }
                    
                    // INCREMENT LAYERS PRINTED
                    //msg = "incrementing layers printed...";
                    //MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);
                    //incrementLayersPrinted(); // TODO: Figure out how to increment in a timely manner; happening right away because this is an asynchronous method!

                    msg = "moving to next layer";
                    MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);

                    // LAYER MOVE
                    // order of layer move operations: home sweep, move powder up 2x, move build down, supply sweep
                    await _motorPageService.LayerMove(layerThickness, amplifier); // _ = means don't wait; technically you can use that here because queuing makes sure operations happen in order, but send occurs instantly, but using await just to be sure
                    while (_motorPageService.MotorsRunning()) { await Task.Delay(100); } // TODO: Test! now awaiting task delay to make this non-blocking
                }
                else 
                { // layer move first (homes sweep first)
                    msg = "moving to next layer";
                    MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);

                    // LAYER MOVE
                    await _motorPageService.LayerMove(layerThickness, amplifier);
                    while (_motorPageService.MotorsRunning()) { await Task.Delay(100); }

                    // MARK
                    msg = $"marking layer {i} in multi-layer print";
                    MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);
                    _ = _waverunnerPageService.MarkEntityAsync();
                    while (_waverunnerPageService.GetMarkStatus() != 0)
                    {
                        Task.Delay(100).Wait();
                    }
                    
                    // INCREMENT LAYERS PRINTED
                    //msg = "incrementing layers printed...";
                    //MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);
                    //incrementLayersPrinted();
                }
            }
            msg = "multi-layer move complete.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.SUCCESS);

            // reset stop request
            KILL_OPERATION = false;
        }
    }

    #region Print Manual Move Methods
    private void StopMotorsHelper()
    {
        // TODO: Does not work when moved to page service (only one motor stops)...no idea why...
        var buildConfig = MagnetoConfig.GetMotorByName("build");
        var sweepConfig = MagnetoConfig.GetMotorByName("sweep");
        var buildMotor = _motorPageService.GetBuildMotor();
        var powderMotor = _motorPageService.GetPowderMotor();
        var sweepMotor = _motorPageService.GetSweepMotor();
        MagnetoLogger.Log("✉️Writing to COM to stop", LogFactoryLogLevel.LogLevel.WARN);
        MagnetoSerialConsole.SerialWrite(buildConfig.COMPort, "1STP"); // build motor is on axis 1
        MagnetoSerialConsole.SerialWrite(buildConfig.COMPort, "2STP");
        MagnetoSerialConsole.SerialWrite(sweepConfig.COMPort, "1STP"); // sweep motor is on axis 1
        buildMotor.STOP_MOVE_FLAG = true;
        powderMotor.STOP_MOVE_FLAG = true;
        sweepMotor.STOP_MOVE_FLAG = true;
        _motorPageService.printUiControlGroupHelper.ChangeSelectButtonsBackground(_calibrateMotorUIControlGroup, Colors.Red);
        LockCalibrationPanel();
    }
    #endregion

    #region Enable/Disable Panel Methods
    private void EnableMotorsButton_Click(object sender, RoutedEventArgs e)
    {
        var buildMotor = _motorPageService.GetBuildMotor();
        var powderMotor = _motorPageService.GetPowderMotor();
        var sweepMotor = _motorPageService.GetSweepMotor();
        buildMotor.STOP_MOVE_FLAG = false;
        powderMotor.STOP_MOVE_FLAG = false;
        sweepMotor.STOP_MOVE_FLAG = false;
        UnlockCalibrationPanel();
        _motorPageService.printUiControlGroupHelper.ChangeSelectButtonsBackground(_calibrateMotorUIControlGroup, Colors.DarkGray);
    }
    private void UnlockCalibrationPanel()
    {
        _motorPageService.printUiControlGroupHelper.EnableUIControlGroup(_calibrateMotorUIControlGroup);
        EnableMotorsButton.Content = "Enable Calibration";
    }

    private void LockCalibrationPanel()
    {
        _motorPageService.printUiControlGroupHelper.DisableUIControlGroup(_calibrateMotorUIControlGroup);
        EnableMotorsButton.Content = "Enable Calibration";
    }

    #endregion

    #region POC Page Text Managers
    private async void PopulatePageText()
    {
        var print = ViewModel.currentPrint;
        var slice = ViewModel.currentSlice;
        if (print != null)
        {
            if (!string.IsNullOrWhiteSpace(print.directoryPath))
            {
                if (slice != null)
                {
                    if (!string.IsNullOrWhiteSpace(slice.filePath))
                    {
                        // ✅ All values are valid — update the UI
                        PrintNameTextBlock.Text = print.name;
                        CurrentSliceTextBox.Text = slice.fileName;
                        StatusTextBlock.Text = print?.complete == true ? "Complete" : "Incomplete";
                        SlicesMarkedTextBlock.Text = (await ViewModel.GetSlicesMarkedAsync()).ToString();
                        TotalSlicesTextBlock.Text = (await ViewModel.GetTotalSlicesAsync()).ToString();
                        // convert UTC to local time
                        var duration = print.duration;
                        var localStart = print.startTime.ToLocalTime();
                        var localEnd = print.endTime?.ToLocalTime();
                        Debug.WriteLine($"📅 start: {print.startTime}, end: {print.endTime}, duration: {print.duration}");
                        DurationTextBlock.Text = duration?.ToString(@"hh\:mm\:ss") ?? "—";
                    }
                    else
                    {
                        Debug.WriteLine("❌ Slice image path is null or empty.");
                        return;
                    }
                }
                else
                {
                    Debug.WriteLine("❌ Current slice is null.");
                    return;
                }
            }
            else
            {
                Debug.WriteLine("❌ Directory path is null or empty.");
                return;
            }
        }
        else
        {
            Debug.WriteLine("❌ Current print is null.");
            return;
        }
    }
    private void ClearPageText()
    {
        PrintDirectoryInputTextBox.Text = "";
        PrintNameTextBlock.Text = "";
        CurrentSliceTextBox.Text = "";
        StatusTextBlock.Text = "";
        DurationTextBlock.Text = "";
        SlicesMarkedTextBlock.Text = "";
        TotalSlicesTextBlock.Text = "";
        ViewModel.ClearData();
    }
    #endregion

    #region POC Button Methods
    private async void GetSlices_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        await ViewModel.AddPrintToDatabaseAsync(PrintDirectoryInputTextBox.Text);
        if (ViewModel.currentSlice != null)
        {
            if (ViewModel.currentSlice.filePath == null)
            {
                Debug.WriteLine("❌ImagePath is null.");
                return;
            }
            PopulatePageText();
        }
    }
    private async void PrintLayersButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        await ViewModel.HandleMarkEntityAsync();
        PopulatePageText();
    }
    private async void DeletePrintButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        // TODO: add guards to ask user if they're sure they want to delete this print
        if (ViewModel.currentPrint == null)
        {
            Debug.WriteLine("❌Current print is null");
            return;
        }
        else
        {
            Debug.WriteLine("✅Deleting print.");
            await ViewModel.DeleteCurrentPrintAsync();
            Debug.WriteLine("✅Removing data from display.");
            ClearPageText();
        }
    }
    private async void BrowseButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var folderPicker = new FolderPicker();
        folderPicker.SuggestedStartLocation = PickerLocationId.Desktop;
        folderPicker.FileTypeFilter.Add("*");

        var hwnd = WindowNative.GetWindowHandle(App.MainWindow);
        InitializeWithWindow.Initialize(folderPicker, hwnd);

        var folder = await folderPicker.PickSingleFolderAsync();
        // folder must contain .sjf files. if it does not contain any, error and return
        if (folder != null)
        {
            // Check for .sjf files in the selected folder
            var files = Directory.EnumerateFiles(folder.Path, "*.sjf");
            if (!files.Any())
            {
                Debug.WriteLine("❌ No .sjf files found in the selected folder.");
                ContentDialog dialog = new ContentDialog
                {
                    Title = "No Job Files in Folder",
                    Content = "The selected folder does not contain any .sjf files.",
                    CloseButtonText = "OK",
                    XamlRoot = this.Content.XamlRoot
                };
                await dialog.ShowAsync();
                return;
            }
            PrintDirectoryInputTextBox.Text = folder.Path;
            await ViewModel.AddPrintToDatabaseAsync(folder.Path);
        }
        PopulatePageText();
    }
    #endregion

    #region Logging Methods

    /// <summary>
    /// Log and display the same message
    /// </summary>
    /// <param name="LogLevel"></param>
    /// <param name="xamlRoot"></param>
    /// <param name="msg"></param>
    private async void LogAndDisplayMessage(LogFactoryLogLevel.LogLevel LogLevel, XamlRoot xamlRoot, string msg)
    {
        var PopupMessageType = GetPopupMessageType(LogLevel);

        MagnetoLogger.Log(msg, LogLevel);
        await PopupInfo.ShowContentDialog(xamlRoot, PopupMessageType, msg);
    }

    /// <summary>
    /// Update UI and log
    /// </summary>
    /// <param name="uiMessage"></param>
    /// <param name="logLevel"></param>
    /// <param name="logMessage"></param>
    private void LogMessage(string uiMessage, LogFactoryLogLevel.LogLevel logLevel, string logMessage = null)
    {
        // Update UI with the message
        //UpdateUITextHelper.UpdateUIText(IsMarkingText, uiMessage);

        // Use the provided log level for logging
        MagnetoLogger.Log(logMessage ?? uiMessage, logLevel);
    }

    private string GetPopupMessageType(LogFactoryLogLevel.LogLevel LogLevel)
    {
        switch (LogLevel)
        {
            case LogFactoryLogLevel.LogLevel.DEBUG:
                return "Debug";
            case LogFactoryLogLevel.LogLevel.VERBOSE:
                return "Info";
            case LogFactoryLogLevel.LogLevel.WARN:
                return "Warning";
            case LogFactoryLogLevel.LogLevel.ERROR:
                return "Error";
            case LogFactoryLogLevel.LogLevel.SUCCESS:
                return "Success";
            default:
                return "Unknown";
        }
    }

    /// <summary>
    /// Log and Display if you want to have a different log and pop up message
    /// </summary>
    /// <param name="LogLevel"></param>
    /// <param name="xamlRoot"></param>
    /// <param name="LogMessage"></param>
    /// <param name="PopupMessage"></param>
    private async void LogAndDisplayMessage(LogFactoryLogLevel.LogLevel LogLevel, XamlRoot xamlRoot, string LogMessage, string PopupMessage)
    {
        var PopupMessageType = GetPopupMessageType(LogLevel);

        MagnetoLogger.Log(LogMessage, LogLevel);
        await PopupInfo.ShowContentDialog(xamlRoot, PopupMessageType, PopupMessage);
    }

    #endregion
    private void TEST_Click(object sender, RoutedEventArgs e)
    {
        ViewModel.TestWaverunnerConnection();
    }
    
    private void EnableBuildCalibrationButton_Click(object sender, RoutedEventArgs e)
    {
    }
    private void EnablePowderCalibrationButton_Click(object sender, RoutedEventArgs e)
    {
    }
    private void EnableSweepCalibrationButton_Click(object sender, RoutedEventArgs e)
    {
    }
}
