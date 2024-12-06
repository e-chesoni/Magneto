using System.IO.Ports;
using System.Reflection;
using CommunityToolkit.WinUI.UI.Animations;
using Magneto.Desktop.WinUI.Core;
using Magneto.Desktop.WinUI.Core.Contracts.Services;
using Magneto.Desktop.WinUI.Core.Contracts.Services.Motor;
using Magneto.Desktop.WinUI.Core.Helpers;
using Magneto.Desktop.WinUI.Core.Models;
using Magneto.Desktop.WinUI.Core.Models.BuildModels;
using Magneto.Desktop.WinUI.Core.Models.Motor;
using Magneto.Desktop.WinUI.Core.Services;
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
using static Magneto.Desktop.WinUI.Core.Models.BuildModels.ActuationManager;
using static Magneto.Desktop.WinUI.Views.TestPrintPage;

namespace Magneto.Desktop.WinUI.Views;

/// <summary>
/// Test print page
/// </summary>
public sealed partial class TestPrintPage : Page
{
    #region Motor Variables

    private StepperMotor? _powderMotor;

    private StepperMotor? _buildMotor;

    private StepperMotor? _sweepMotor;

    private ActuationManager? _bm;

    private bool _powderMotorSelected = false;

    private bool _buildMotorSelected = false;

    private bool _sweepMotorSelected = false;

    private bool _movingMotorToTarget = false;

    /// <summary>
    /// Struct for motor details
    /// </summary>
    public struct MotorDetails
    {
        public string MotorName { get; }
        public ControllerType ControllerType { get; }
        public int MotorAxis { get; }

        public MotorDetails(string motorName, ControllerType controllerType, int motorAxis)
        {
            MotorName = motorName;
            ControllerType = controllerType;
            MotorAxis = motorAxis;
        }
    }

    #endregion


    #region WaveRunner Variables

    /// <summary>
    /// WaveRunner client control interface
    /// </summary>
    private static readonly ScSamlightClientCtrlEx cci = new();

    /// <summary>
    /// Default job directory (to search for job files)
    /// </summary>
    private string _defaultJobDirectory { get; set; }

    /// <summary>
    /// Default job file name
    /// </summary>
    private string _defaultJobName { get; set; }

    /// <summary>
    /// Job directory (to search for files) -- can be defined by the user
    /// </summary>
    private string _jobDirectory { get; set; }

    /// <summary>
    /// Full file path to entity
    /// </summary>
    private string? _fullJobFilePath { get; set; }

    private bool _redPointerEnabled { get; set; }

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

    private double _layerThickness;

    private double _desiredPrintHeight;

    private double _totalLayersToPrint { get; set; }

    private int _layersPrinted { get; set; }

    private double _totalPrintHeight { get; set; }

    private Dictionary<double, int> _printHistoryDictionary { get; set; }


    #endregion

    #region Page Services

    private MotorPageService _motorPageService;
    private WaverunnerPageService _waverunnerPageService;

    #endregion


    #region UI Helper Variables

    private MotorUIControlGroup _calibrateMotorUIControlGroup { get; set; }
    private MotorUIControlGroup _inPrintMotorUIControlGroup { get; set; }
    private PrintSettingsUIControlGroup _printSettingsUIControlGroup { get; set; }
    private PrintSettingsUIControlGroup _layerSettingsUIControlGroup { get; set; }

    private PrintUIControlGroupHelper _printControlGroupHelper { get; set; }

    private bool _calibrationPanelEnabled = true;

    private bool _fileSettingsSectionEnabled = true;

    private bool _layerSettingsSectionEnabled = true;

    private bool _settingsPanelEnabled = true;

    private bool _printPanelEnabled = true;

    #endregion


    #region Core Page Functionality Variables

    /// <summary>
    /// Central control that gets passed from page to page
    /// </summary>
    public MissionControl? MissionControl
    {
        get; set;
    }

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

        _bm = MissionControl.GetActuationManger();

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

    // TODO: Lock settings and print panels by default

    #endregion

    #region Constructor

    /// <summary>
    /// Constructor for TestPrintPage. Initializes the ViewModel, sets up UI components, logs the page visit,
    /// retrieves configuration for build and sweep motors, and registers event handlers for their respective ports.
    /// </summary>
    public TestPrintPage()
    {
        ViewModel = App.GetService<TestPrintViewModel>();
        InitializeComponent();
        LockPrintManager();

        var msg = "Landed on Test Print Page";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.DEBUG);
        MagnetoSerialConsole.LogAvailablePorts();
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
                                                                StopBuildMotorInCalibrateButton, StopPowderMotorInCalibrateButton, StopSweepMotorInCalibrateButton,
                                                                HomeAllMotorsButton, StopAllMotorsInCalibrationPanelButton);

        _inPrintMotorUIControlGroup = new MotorUIControlGroup(SelectBuildInPrintButton, SelectPowderInPrintButton, SelectSweepInPrintButton,
                                                              BuildMotorCurrentPositionTextBox, PowderMotorCurrentPositionTextBox, SweepMotorCurrentPositionTextBox,
                                                              BuildMotorStepInPrintTextBox, PowderMotorStepTextBox, SweepMotorStepTextBox,
                                                              IncrementBuildButton, DecrementBuildButton, IncrementPowderButton, DecrementPowderButton, SweepLeftButton, SweepRightButton,
                                                              StopBuildMotorButton, StopPowderMotorButton, StopSweepButton,
                                                              HomeAllMotorsButton, StopAllMotorsInCalibrationPanelButton);


        var printSettingsControls = new List<object> { JobFileSearchDirectoryTextBox, UpdateDirectoryButton, JobFileNameTextBox, UseDefaultJobButton, GetJobButton, TestWaverunnerConnectionButton, ToggleRedPointerButton, StartMarkButton };
        _printSettingsUIControlGroup = new PrintSettingsUIControlGroup(printSettingsControls);

        var layersettingsControls = new List<object> { SetLayerThicknessTextBox, UpdateLayerThicknessButton, DesiredPrintHeightTextBox, UpdateDesiredPrintHeightButton, EstimatedLayersToPrintTextBlock };
        _layerSettingsUIControlGroup = new PrintSettingsUIControlGroup(layersettingsControls);

        _printControlGroupHelper = new PrintUIControlGroupHelper(_calibrateMotorUIControlGroup, _inPrintMotorUIControlGroup, _printSettingsUIControlGroup, _layerSettingsUIControlGroup);

        // Initialize motor page service
        _motorPageService = new MotorPageService(MissionControl.GetActuationManger(), _printControlGroupHelper);

        // initialize waverunner page service
        _waverunnerPageService = new WaverunnerPageService(JobFileSearchDirectoryTextBox, JobFileNameTextBox,
                                                           ToggleRedPointerButton, StartMarkButton);

        // Set default job file
        _waverunnerPageService.SetDefaultJobFileName("steel-3D-test-11-22-24.sjf");
    }

    private void SetDefaultPrintSettings()
    {
        _layerThickness = 0.08; // set default layer height to be 0.08mm based on first steel print
        _desiredPrintHeight = 5; // set default print height to be mm
        DesiredPrintHeightTextBox.Text = _desiredPrintHeight.ToString();
        SetLayerThicknessTextBox.Text = _layerThickness.ToString();
    }

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
        MissionControl = (MissionControl)e.Parameter;

        InitPageServices();
        SetDefaultPrintSettings();

        var msg = string.Format("TestPrintPage::OnNavigatedTo -- {0}", MissionControl.FriendlyMessage);
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.DEBUG);
    }

    #endregion

    #region Motor Helpers

    private void HomeMotorsHelper()
    {

        var msg = "Homing all motors";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);

        if (_motorPageService.buildMotor != null)
        {
            _motorPageService.HandleHomeMotorAndUpdateTextBox(_motorPageService.buildMotor, _motorPageService.GetBuildPositionTextBox());
        }
        else
        {
            MagnetoLogger.Log("Build Motor is null, cannot home motor.", LogFactoryLogLevel.LogLevel.ERROR);
        }

        if (_motorPageService.powderMotor != null)
        {
            _motorPageService.HandleHomeMotorAndUpdateTextBox(_motorPageService.powderMotor, _motorPageService.GetPowderPositionTextBox());
        }
        else
        {
            MagnetoLogger.Log("Powder Motor is null, cannot home motor.", LogFactoryLogLevel.LogLevel.ERROR);
        }

        if (_motorPageService.sweepMotor != null)
        {
            _motorPageService.HandleHomeMotorAndUpdateTextBox(_motorPageService.sweepMotor, _motorPageService.GetSweepPositionTextBox());
        }
        else
        {
            MagnetoLogger.Log("Sweep Motor is null, cannot home motor.", LogFactoryLogLevel.LogLevel.ERROR);
        }
    }

    private void KillAll()
    {
        // stop mark
        _waverunnerPageService.StopMark();

        // stop motors
        _motorPageService.GetActuationManager().HandleStopRequest(_motorPageService.buildMotor);
        _motorPageService.GetActuationManager().HandleStopRequest(_motorPageService.powderMotor);
        _motorPageService.GetActuationManager().HandleStopRequest(_motorPageService.sweepMotor);
    }

    #endregion


    #region Calibration Panel Methods

    private void SelectBuildMotorButton_Click(object sender, RoutedEventArgs e)
    {
        _motorPageService.printUiControlGroupHelper.SelectMotor(_motorPageService.buildMotor);
    }

    private void SelectPowderMotorButton_Click(object sender, RoutedEventArgs e)
    {
        _motorPageService.printUiControlGroupHelper.SelectMotor(_motorPageService.powderMotor);
    }

    private void SelectSweepMotorButton_Click(object sender, RoutedEventArgs e)
    {
        _motorPageService.printUiControlGroupHelper.SelectMotor(_motorPageService.sweepMotor);
    }

    private void GetBuildMotorCurrentPositionButton_Click(object sender, RoutedEventArgs e)
    {
        _motorPageService.HandleGetPosition(_motorPageService.buildMotor, _motorPageService.GetBuildPositionTextBox());
    }

    private void GetPowderMotorCurrentPositionButton_Click(object sender, RoutedEventArgs e)
    {
        _motorPageService.HandleGetPosition(_motorPageService.powderMotor, _motorPageService.GetPowderPositionTextBox());
    }

    private void GetSweepMotorCurrentPositionButton_Click(object sender, RoutedEventArgs e)
    {
        _motorPageService.HandleGetPosition(_motorPageService.sweepMotor, _motorPageService.GetSweepPositionTextBox());
    }

    private void MoveBuildToAbsPositionButton_Click(object sender, RoutedEventArgs e)
    {
        _motorPageService.HandleAbsMove(_motorPageService.buildMotor, _motorPageService.GetBuildAbsMoveTextBox(), this.Content.XamlRoot);
    }

    private void MovePowderToAbsPositionButton_Click(object sender, RoutedEventArgs e)
    {
        _motorPageService.HandleAbsMove(_motorPageService.powderMotor, _motorPageService.GetPowderAbsMoveTextBox(), this.Content.XamlRoot);
    }

    private void MoveSweepToAbsPositionButton_Click(object sender, RoutedEventArgs e)
    {
        _motorPageService.HandleAbsMove(_motorPageService.sweepMotor, _motorPageService.GetSweepAbsMoveTextBox(), this.Content.XamlRoot);
    }

    private void StepBuildMotorUpButton_Click(object sender, RoutedEventArgs e)
    {
        MagnetoLogger.Log("step build up clicked", LogFactoryLogLevel.LogLevel.VERBOSE);
        _motorPageService.HandleRelMove(_motorPageService.buildMotor, _motorPageService.GetBuildStepTextBox(), true, this.Content.XamlRoot);
    }

    private void StepBuildMotorDownButton_Click(object sender, RoutedEventArgs e)
    {
        MagnetoLogger.Log("step build down clicked", LogFactoryLogLevel.LogLevel.VERBOSE);
        _motorPageService.HandleRelMove(_motorPageService.buildMotor, _motorPageService.GetBuildStepTextBox(), false, this.Content.XamlRoot);
    }

    private void StepPowderMotorUpButton_Click(object sender, RoutedEventArgs e)
    {
        _motorPageService.HandleRelMove(_motorPageService.powderMotor, _motorPageService.GetPowderStepTextBox(), true, this.Content.XamlRoot);
    }

    private void StepPowderMotorDownButton_Click(object sender, RoutedEventArgs e)
    {
        _motorPageService.HandleRelMove(_motorPageService.powderMotor, _motorPageService.GetPowderStepTextBox(), false, this.Content.XamlRoot);
    }

    private void StepSweepMotorLeftInCalibrateButton_Click(object sender, RoutedEventArgs e)
    {
        _motorPageService.HandleRelMove(_motorPageService.sweepMotor, _motorPageService.GetSweepStepTextBox(), true, this.Content.XamlRoot);
    }

    private void StepSweepMotorRightInCalibrateButton_Click(object sender, RoutedEventArgs e)
    {
        _motorPageService.HandleRelMove(_motorPageService.sweepMotor, _motorPageService.GetSweepStepTextBox(), false, this.Content.XamlRoot);
    }

    private void HomeAllMotorsInCalibrationPanelButton_Click(object sender, RoutedEventArgs e)
    {
        HomeMotorsHelper();
    }

    private void StopAllMotorsInCalibrationPanelButton_Click(object sender, RoutedEventArgs e)
    {
        StopMotorsHelper();
    }

    private void StopBuildMotorInCalibrateButton_Click(object sender, RoutedEventArgs e)
    {
        _motorPageService.GetActuationManager().HandleStopRequest(_motorPageService.buildMotor);
    }

    private void StopPowderMotorInCalibrateButton_Click(object sender, RoutedEventArgs e)
    {
        _motorPageService.GetActuationManager().HandleStopRequest(_motorPageService.powderMotor);
    }

    private void StopSweepMotorInCalibrateButton_Click(object sender, RoutedEventArgs e)
    {
        _motorPageService.GetActuationManager().HandleStopRequest(_motorPageService.sweepMotor);
    }

    #endregion


    #region Settings Methods

    private void UseDefaultJobButton_Click(object sender, RoutedEventArgs e)
    {
        _waverunnerPageService.UseDefaultJob();
    }

    private void UpdateDirectoryButton_Click(object sender, RoutedEventArgs e)
    {
        _waverunnerPageService.UpdateDirectory();
        CurrentPrintDirectory.Text = JobFileSearchDirectoryTextBox.Text;
    }

    private void GetJobButton_Click(object sender, RoutedEventArgs e)
    {
        // TODO: If job is invalid, need to wipe it from print manager (invalidate ready to print)
        _waverunnerPageService.GetJob(this.Content.XamlRoot);
        CurrentJobFile.Text = JobFileNameTextBox.Text;
    }

    private void ToggleRedPointerButton_Click(object sender, RoutedEventArgs e)
    {
        _waverunnerPageService.StartRedPointer();
    }


    private void StartMarkButton_Click(object sender, RoutedEventArgs e)
    {
        _ = _waverunnerPageService.MarkEntityAsync();
        //WARNING: do not increment layers here; this is a test in settings
    }

    private void StopMarkButton_Click(object sender, RoutedEventArgs e)
    {
        _waverunnerPageService.StopMark();
    }
    private void UpdateLayerThicknessButton_Click(object sender, RoutedEventArgs e)
    {
        // TODO: If layer thickness is invalid, need to wipe it from print manager (invalidate ready to print)
        _layerThickness = Math.Round(double.Parse(SetLayerThicknessTextBox.Text), 3);
        CurrentLayerThickness.Text = _layerThickness.ToString() + " mm";
    }

    private void UpdateDesiredPrintHeightButton_Click(object sender, RoutedEventArgs e)
    {
        // Get the total print height
        _totalPrintHeight = Math.Round(double.Parse(DesiredPrintHeightTextBox.Text), 3);
        CurrentTotalPrintHeight.Text = _totalPrintHeight.ToString() + " mm";

        // Calculate number of layers to print
        _totalLayersToPrint = (int)Math.Ceiling(_totalPrintHeight / _layerThickness); // rounds up to complete final layer

        // Update estimated print height text boxes in settings and print panel
        EstimatedLayersToPrintTextBlock.Text = _totalLayersToPrint.ToString();
        RemainingLayersToPrint.Text = _totalLayersToPrint.ToString(); // TODO: May need to rethink this when if print is updated mid cycle
    }

    #endregion

    #region Print Summary Methods

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

    // TODO: remove when done testing
    private void AddDummyPrintHistory()
    {
        _printHistoryDictionary[0.03] = 6;  // 6 layers printed at 0.03mm
        _printHistoryDictionary[0.05] = 21;  // 24 layers printed at 0.05mm
        _printHistoryDictionary[0.08] = 33;  // 33 layers printed at 0.08mm
    }

    private void StartNewPrintButton_Click(object sender, RoutedEventArgs e)
    {
        _printHistoryDictionary = new Dictionary<double, int>();

        // TODO: Remove print history dummy values when done testing
        AddDummyPrintHistory();
        TestPrintHistoryPopulate();

        // update layers printed
        updateLayerTrackingUI();
    }

    private void updateLayerTrackingUI()
    {
        _layersPrinted = _printHistoryDictionary.Any() ? _printHistoryDictionary.Values.Sum() : 0;
        LayersPrintedTextBlock.Text = _layersPrinted.ToString();
        RemainingLayersToPrint.Text = (_totalLayersToPrint - _layersPrinted).ToString();
    }
    private int incrementLayersPrinted()
    {
        //TODO: get current layer thickness, increment matching value in print history dictionary
        // if no value exists for current layer thickness, add it
        // DO above instead of _layersPrinted++;

        updateLayerTrackingUI();

        return _layersPrinted;
    }

    private void CancelPrintButton_Click(object sender, RoutedEventArgs e)
    {
        _printHistoryDictionary.Clear();
    }

    #endregion

    

    #region Print Layer Move Methods

    private void MoveToNextLayerStartPositionButton_Click(object sender, RoutedEventArgs e)
    {
        _motorPageService.MoveToNextLayer(_layerThickness);
    }

    private void StopSingleLayerMoveButton_Click(object sender, RoutedEventArgs e)
    {
        KillAll();
    }

    private void StartMarkInLayerMoveButton_Click(object sender, RoutedEventArgs e)
    {
        _ = _waverunnerPageService.MarkEntityAsync();

        _ = incrementLayersPrinted(); // TODO: TEST
    }

    private void StopMarkInLayerMoveButton_Click(object sender, RoutedEventArgs e)
    {
        _waverunnerPageService.StopMark();
    }

    private void ReturnSweepInLayerMoveButton_Click(object sender, RoutedEventArgs e)
    {
        _motorPageService.HandleHomeMotor(_motorPageService.sweepMotor);
    }

    private void StopReturnSweepInLayerMoveButton_Click(object sender, RoutedEventArgs e)
    {
        _motorPageService.GetActuationManager().HandleStopRequest(_motorPageService.sweepMotor);
    }

    private readonly Queue<Func<Task>> _operationQueue= new Queue<Func<Task>>();
    private bool _isProcessingQueue = false;

    private async Task EnqueueOperation(Func<Task> operation)
    {
        _operationQueue.Enqueue(operation);

        if (!_isProcessingQueue)
        {
            _isProcessingQueue = true;
            await ProcessQueue();
        }
    }

    private async Task ProcessQueue()
    {
        while (_operationQueue.Count > 0)
        {
            var operation = _operationQueue.Dequeue();
            try
            {
                await operation(); // execute the tasks. not sure if this will work
            }
            catch (Exception ex) 
            {
                MagnetoLogger.Log($"Error in operation: {ex.Message}", LogFactoryLogLevel.LogLevel.ERROR);
            }
        }
        _isProcessingQueue = false;
    }


    // TODO: TEST!!
    private async void StartMultiLayerMoveButton_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(MultiLayerMoveInputTextBox.Text) || !int.TryParse(MultiLayerMoveInputTextBox.Text, out var layers))
        {
            var msg = "MultiLayerMoveInputTextBox text is not a valid integer.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);

            // TODO: add pop up message for invalid input

            return; // Exit the method if the validation fails
        } else { 
            for (var i = 0; i < layers; i++)
            {
                // sweep powder
                await EnqueueOperation(() => Task.Run(() => _motorPageService.SweepAndApplyMaterial()));

                // Do a layer move for both motors
                await EnqueueOperation(() => Task.Run(() => _motorPageService.MoveToNextLayer(_layerThickness)));

                // Mark job
                //await EnqueueOperation(() => Task.Run(() => _waverunnerPageService.MarkEntityAsync()));

                // Update the number of layers printed
                await EnqueueOperation(() => Task.Run(() => incrementLayersPrinted()));

                // return sweep
                await EnqueueOperation(() => Task.Run(() => _motorPageService.HandleHomeMotorAndUpdateTextBox(_motorPageService.sweepMotor, _motorPageService.GetSweepPositionTextBox())));
            }
        }
    }

    private void StopMultiLayerMoveButton_Click(object sender, RoutedEventArgs e)
    {
        KillAll();
    }

    #endregion


    #region Print Manual Move Methods

    private void SelectBuildInPrintButton_Click(object sender, RoutedEventArgs e)
    {
        _motorPageService.printUiControlGroupHelper.SelectMotorInPrint(_motorPageService.buildMotor);
    }

    private void SelectPowderInPrintButton_Click(object sender, RoutedEventArgs e)
    {
        _motorPageService.printUiControlGroupHelper.SelectMotorInPrint(_motorPageService.powderMotor);
    }

    private void SelectSweepInPrintButton_Click(object sender, RoutedEventArgs e)
    {
        _motorPageService.printUiControlGroupHelper.SelectMotorInPrint(_motorPageService.sweepMotor);
    }

    private void IncrementBuildButton_Click(object sender, RoutedEventArgs e)
    {
        _motorPageService.HandleRelMoveInSitu(_motorPageService.buildMotor, BuildMotorStepInPrintTextBox, true, this.Content.XamlRoot);
    }

    private void DecrementBuildButton_Click(object sender, RoutedEventArgs e)
    {
        _motorPageService.HandleRelMoveInSitu(_motorPageService.buildMotor, BuildMotorStepInPrintTextBox, false, this.Content.XamlRoot);
    }

    private void IncrementPowderButton_Click(object sender, RoutedEventArgs e)
    {
        _motorPageService.HandleRelMoveInSitu(_motorPageService.powderMotor, PowderMotorStepInPrintTextBox, true, this.Content.XamlRoot);
    }

    private void DecrementPowderButton_Click(object sender, RoutedEventArgs e)
    {
        _motorPageService.HandleRelMoveInSitu(_motorPageService.powderMotor, PowderMotorStepInPrintTextBox, false, this.Content.XamlRoot);
    }

    private void SweepRightButton_Click(object sender, RoutedEventArgs e)
    {
        _motorPageService.HandleHomeMotorAndUpdateTextBox(_motorPageService.sweepMotor, _motorPageService.GetSweepPositionTextBox());
    }

    private void SweepLeftButton_Click(object sender, RoutedEventArgs e)
    {
        _motorPageService.SweepAndApplyMaterial();
    }

    private void StopSweepButton_Click(object sender, RoutedEventArgs e)
    {
        _motorPageService.GetActuationManager().HandleStopRequest(_motorPageService.sweepMotor);
    }

    private void HomeAllMotorsButton_Click(object sender, RoutedEventArgs e)
    {
        HomeMotorsHelper();
    }

    private void StopMotorsHelper()
    {
        _motorPageService.GetActuationManager().HandleStopRequest(_motorPageService.buildMotor);
        _motorPageService.GetActuationManager().HandleStopRequest(_motorPageService.powderMotor);
        _motorPageService.GetActuationManager().HandleStopRequest(_motorPageService.sweepMotor);
    }

    private void StopAllMotorsInPrintButton_Click(object sender, RoutedEventArgs e)
    {
        StopMotorsHelper();
    }

    private void StopBuildMotorButton_Click(object sender, RoutedEventArgs e)
    {
        _motorPageService.GetActuationManager().HandleStopRequest(_motorPageService.buildMotor);
    }

    private void StopPowderMotorButton_Click(object sender, RoutedEventArgs e)
    {
        _motorPageService.GetActuationManager().HandleStopRequest(_motorPageService.powderMotor);
    }

    #endregion


    #region Enable/Disable Panel Methods

    private void EnableLayerMoveButton_Click(object sender, RoutedEventArgs e)
    {

    }

    private void EnableManualMoveButton_Click(object sender, RoutedEventArgs e)
    {

    }

    private void EnableCalibrationPanel()
    {
        _motorPageService.printUiControlGroupHelper.DisableUIControlGroup(_calibrateMotorUIControlGroup);
        ToggleCalibrationPanelButtonLock.Content = "Unlock Calibration";
    }

    private void DisableCalibrationPanel()
    {
        _motorPageService.printUiControlGroupHelper.EnableUIControlGroup(_calibrateMotorUIControlGroup);
        ToggleCalibrationPanelButtonLock.Content = "Lock Calibration";
    }

    private void ToggleCalibrationPanelButtonLock_Click(object sender, RoutedEventArgs e)
    {
        if (_calibrationPanelEnabled)
        {
            DisableCalibrationPanel();
        } else {
            EnableCalibrationPanel();
        }
        _calibrationPanelEnabled = !_calibrationPanelEnabled;
    }

    public void LockSettingsPanel()
    {
        _motorPageService.printUiControlGroupHelper.DisableUIControlGroup(_printSettingsUIControlGroup);
        _motorPageService.printUiControlGroupHelper.DisableUIControlGroup(_layerSettingsUIControlGroup);
        //ToggleFileSettingsLockButton.Content = "Unlock File Settings";
    }

    public void UnlockSettingsPanel()
    {
        _motorPageService.printUiControlGroupHelper.EnableUIControlGroup(_printSettingsUIControlGroup);
        _motorPageService.printUiControlGroupHelper.EnableUIControlGroup(_layerSettingsUIControlGroup);
        //ToggleFileSettingsLockButton.Content = "Lock File Settings";
    }

    private void UnlockLayerSection()
    {
        SetLayerThicknessTextBox.IsEnabled = false;
        UpdateLayerThicknessButton.IsEnabled = false;
        _layerSettingsSectionEnabled = false;
        //ToggleLayerSettingsLockButton.Content = "Unlock Layer Settings";
    }

    private void UnLockLayerSection()
    {
        SetLayerThicknessTextBox.IsEnabled = true;
        UpdateLayerThicknessButton.IsEnabled = true;
        _layerSettingsSectionEnabled = true;
        //ToggleLayerSettingsLockButton.Content = "Lock Layer Settings";
    }

    private bool ValidateReadyToPrint()
    {
        var currJobToPrint = Path.Combine(JobFileSearchDirectoryTextBox.Text, JobFileNameTextBox.Text);
        if (!string.IsNullOrWhiteSpace(currJobToPrint) && !string.IsNullOrEmpty(CurrentLayerThickness.Text))
        {
            return true;
        }
        else
        {
            return false;
        }
    }

    private void ToggleSettingsPanelButton_Click(object sender, RoutedEventArgs e)
    {
        
        if (ValidateReadyToPrint())
        {
            if (_settingsPanelEnabled)
            {
                LockSettingsPanel();
                _settingsPanelEnabled = false;
                ToggleSettingsPanelButton.Content = "Unlock Settings";
                UnlockPrintManager();
            } else { // settings are locked
                UnlockSettingsPanel();
                _settingsPanelEnabled = true;
                ToggleSettingsPanelButton.Content = "Lock Settings";
                LockPrintManager();
            }
            
        } else {
            // TODO: show pop up calling out missing settings
        }
    }

    private void ToggleLayerSectionHelper()
    {
        if (_layerSettingsSectionEnabled)
        {
            UnlockLayerSection();
        } else {
            UnLockLayerSection();
        }
    }

    private void LockPrintManager()
    {
        // Layer Move Buttons
        EnableLayerMoveButton.IsEnabled = false;
        MoveToNextLayerStartPositionButton.IsEnabled = false;


        // Manual Move Buttons
        EnableManualMoveButton.IsEnabled = false;

        SelectBuildInPrintButton.IsEnabled = false;
        IncrementBuildButton.IsEnabled = false;
        DecrementBuildButton.IsEnabled = false;

        SelectPowderInPrintButton.IsEnabled = false;
        IncrementPowderButton.IsEnabled = false;
        DecrementPowderButton.IsEnabled = false;

        SelectSweepInPrintButton.IsEnabled = false;
        SweepLeftButton.IsEnabled = false;
        SweepRightButton.IsEnabled = false;

        HomeAllMotorsButton.IsEnabled = false;
    }

    private void UnlockPrintManager()
    {
        // Layer Move Buttons
        EnableLayerMoveButton.IsEnabled = true;
        MoveToNextLayerStartPositionButton.IsEnabled = true;


        // Manual Move Buttons
        EnableManualMoveButton.IsEnabled = true;

        SelectBuildInPrintButton.IsEnabled = true;
        IncrementBuildButton.IsEnabled = true;
        DecrementBuildButton.IsEnabled = true;

        SelectPowderInPrintButton.IsEnabled = true;
        IncrementPowderButton.IsEnabled = true;
        DecrementPowderButton.IsEnabled = true;

        SelectSweepInPrintButton.IsEnabled = true;
        SweepLeftButton.IsEnabled = true;
        SweepRightButton.IsEnabled = true;

        HomeAllMotorsButton.IsEnabled = true;
    }

    #endregion


    #region Reset Button Method

    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        // TODO: Clear page settings to start new print

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
    private void LogMessage(string uiMessage, Core.Contracts.Services.LogFactoryLogLevel.LogLevel logLevel, string logMessage = null)
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

}
