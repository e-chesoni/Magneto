﻿using System.IO.Ports;
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
        public string MotorName
        {
            get;
        }
        public ControllerType ControllerType
        {
            get;
        }
        public int MotorAxis
        {
            get;
        }

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
    private string _defaultJobDirectory
    {
        get; set;
    }

    /// <summary>
    /// Default job file name
    /// </summary>
    private string _defaultJobName
    {
        get; set;
    }

    /// <summary>
    /// Job directory (to search for files) -- can be defined by the user
    /// </summary>
    private string _jobDirectory
    {
        get; set;
    }

    /// <summary>
    /// Full file path to entity
    /// </summary>
    private string? _fullJobFilePath
    {
        get; set;
    }

    private bool _redPointerEnabled
    {
        get; set;
    }

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


    #region Layer Thickness Variables

    private double _layerThickness;

    #endregion

    private MotorPageService _motorPageService;
    private WaverunnerPageService _waverunnerPageService;


    #region Public Variables

    /// <summary>
    /// Central control that gets passed from page to page
    /// </summary>
    public MissionControl? MissionControl { get; set; }

    /// <summary>
    /// TestPrintViewModel view model
    /// </summary>
    public TestPrintViewModel ViewModel { get; }

    #endregion

    private MotorUIControlGroup _calibrateMotorUIControlGroup { get; set; }
    private MotorUIControlGroup _inPrintMotorUIControlGroup { get; set; }
    private PrintSettingsUIControlGroup _printSettingsUIControlGroup { get; set; }
    private PrintSettingsUIControlGroup _layerSettingsUIControlGroup { get; set; }

    private PrintUIControlGroupHelper _printControlGroupHelper { get; set; }

    private bool _calibrationPanelEnabled = true;

    private bool _fileSettingsSectionEnabled = true;

    private bool _layerSettingsSectionEnabled = true;

    private bool _printPanelEnabled = true;

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

    private void InitMotorPageService()
    {
        // create button groups for easy control
        // TODO: need to add stop buttons to calibrate and update initialization here (currently using stop buttons from in print to test)
        _calibrateMotorUIControlGroup = new MotorUIControlGroup(SelectBuildMotorButton, SelectPowderMotorButton, SelectSweepMotorButton,
                                                                             BuildMotorCurrentPositionTextBox, PowderMotorCurrentPositionTextBox, SweepMotorCurrentPositionTextBox,
                                                                             GetBuildMotorCurrentPositionButton, GetPowderMotorCurrentPositionButton, GetSweepMotorCurrentPositionButton,
                                                                             BuildMotorStepTextBox, PowderMotorStepTextBox, SweepMotorStepTextBox,
                                                                             StepBuildMotorUpButton, StepBuildMotorDownButton, StepPowderMotorUpButton, StepPowderMotorDownButton, StepSweepMotorUpButton, StepSweepMotorDownButton,
                                                                             StopBuildMotorInCalibrateButton, StopPowderMotorInCalibrateButton, StopSweepMotorInCalibrateButton,
                                                                             HomeAllMotorsButton, StopAllMotorsInCalibrationPanelButton
                                                                             );

        _inPrintMotorUIControlGroup = new MotorUIControlGroup(SelectBuildInPrintButton, SelectPowderInPrintButton, SelectSweepInPrintButton,
                                                                                        BuildMotorCurrentPositionTextBox, PowderMotorCurrentPositionTextBox, SweepMotorCurrentPositionTextBox,
                                                                                        BuildMotorStepInPrintTextBox, PowderMotorStepTextBox, SweepMotorStepTextBox,
                                                                                        IncrementBuildButton, DecrementBuildButton, IncrementPowderButton, DecrementPowderButton, SweepLeftButton, SweepRightButton,
                                                                                        StopBuildMotorButton, StopPowderMotorButton, StopSweepButton,
                                                                                        HomeAllMotorsButton, StopAllMotorsInCalibrationPanelButton);



        var printSettingsControls = new List<object> { JobFileSearchDirectory.IsEnabled, UpdateDirectoryButton.IsEnabled, JobFileNameTextBox.IsEnabled, GetJobButton.IsEnabled, UseDefaultJobButton.IsEnabled };

        _printSettingsUIControlGroup = new PrintSettingsUIControlGroup(printSettingsControls);

        var layersettingsControls = new List<object> { SetLayerThicknessTextBox, UpdateLayerThicknessButton};

        _layerSettingsUIControlGroup = new PrintSettingsUIControlGroup(layersettingsControls);

        _printControlGroupHelper = new PrintUIControlGroupHelper(_calibrateMotorUIControlGroup, _inPrintMotorUIControlGroup, _printSettingsUIControlGroup, _layerSettingsUIControlGroup);

        //_printControlGroupHelper = new PrintUIControlGroupHelper();

        _motorPageService = new MotorPageService(MissionControl.GetActuationManger(), _printControlGroupHelper);

    }

    private void InitWaverunnerPageService()
    {
        _waverunnerPageService = new WaverunnerPageService(JobFileSearchDirectory, JobFileNameTextBox,
                                                           ToggleRedPointerButton, StartMarkButton);
    }

    private void SetDefaultPrintSettings()
    {
        _layerThickness = MagnetoConfig.GetDefaultPrintThickness();
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

        InitMotorPageService();
        InitWaverunnerPageService();
        SetDefaultPrintSettings();

        var msg = string.Format("TestPrintPage::OnNavigatedTo -- {0}", MissionControl.FriendlyMessage);
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.DEBUG);
    }

    #endregion


    #region Position Methods

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

    #endregion


    #region Step Motor Button Commands

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

    private void StepSweepMotorUpButton_Click(object sender, RoutedEventArgs e)
    {
        _motorPageService.HandleRelMove(_motorPageService.sweepMotor, _motorPageService.GetSweepStepTextBox(), true, this.Content.XamlRoot);
    }

    private void StepSweepMotorDownButton_Click(object sender, RoutedEventArgs e)
    {
        _motorPageService.HandleRelMove(_motorPageService.sweepMotor, _motorPageService.GetSweepStepTextBox(), false, this.Content.XamlRoot);
    }

    #endregion


    #region Sweep Button Commands

    private void SweepRightButton_Click(object sender, RoutedEventArgs e)
    {
        _motorPageService.HandleHomeMotor(_motorPageService.sweepMotor, _motorPageService.GetSweepPositionTextBox());
    }

    private void SweepLeftButton_Click(object sender, RoutedEventArgs e)
    {
        _motorPageService.SweepLeft();
    }

    private void StopSweepButton_Click(object sender, RoutedEventArgs e)
    {
        _motorPageService.GetActuationManager().HandleStopRequest(_motorPageService.sweepMotor);
    }

    #endregion


    #region Marking Buttons

    private void UseDefaultJobButton_Click(object sender, RoutedEventArgs e)
    {
        _waverunnerPageService.UseDefaultJob();
    }

    private void UpdateDirectoryButton_Click(object sender, RoutedEventArgs e)
    {
        _waverunnerPageService.UpdateDirectory();
    }

    private void GetJobButton_Click(object sender, RoutedEventArgs e)
    {
        // TODO: If job is invalid, need to wipe it from print manager (invalidate ready to print)
        _waverunnerPageService.GetJob(this.Content.XamlRoot);
        CurrentJobToPrint.Text = Path.Combine(JobFileSearchDirectory.Text, JobFileNameTextBox.Text);
        if (ValidateReadyToPrint())
        {
            UnlockPrintManager();
        }
    }

    private void ToggleRedPointerButton_Click(object sender, RoutedEventArgs e)
    {
        _waverunnerPageService.StartRedPointer();
    }


    private void StartMarkButton_Click(object sender, RoutedEventArgs e)
    {
        _ = _waverunnerPageService.MarkEntityAsync();
    }

    private void StopMarkButton_Click(object sender, RoutedEventArgs e)
    {
        _waverunnerPageService.StopMark();
    }

    #endregion


    #region Move to Next Layer Button Commands 

    private void UpdateLayerThicknessButton_Click(object sender, RoutedEventArgs e)
    {
        // TODO: If layer thickness is invalid, need to wipe it from print manager (invalidate ready to print)
        _layerThickness = (double)Math.Round(double.Parse(SetLayerThicknessTextBox.Text), 3);
        CurrentLayerThickness.Text = _layerThickness.ToString() + " mm";
        if (ValidateReadyToPrint())
        {
            UnlockPrintManager();
        }
    }

    private void MoveToNextLayerStartPositionButton_Click(object sender, RoutedEventArgs e)
    {
        _motorPageService.MoveToNextLayer(_layerThickness);
    }

    #endregion

    private bool ValidateReadyToPrint()
    {
        if (!string.IsNullOrWhiteSpace(CurrentJobToPrint.Text) && !string.IsNullOrEmpty(CurrentLayerThickness.Text))
        {
            // Lock settings
            LockFileSettingSection();
            LockLayerSection();
            return true;
        } else {
            return false;
        }
    }


    #region Reset Button Commands

    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
        // TODO: Clear page settings to start new print

    }

    #endregion

    
    #region Logging Methods
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

    #endregion

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

    public void LockFileSettingSection()
    {
        _motorPageService.printUiControlGroupHelper.DisableUIControlGroup(_layerSettingsUIControlGroup);
        ToggleFileSettingsLockButton.Content = "Unlock File Settings";
    }

    public void UnlockFileSettingSection()
    {
        _motorPageService.printUiControlGroupHelper.EnableUIControlGroup(_layerSettingsUIControlGroup);
        ToggleFileSettingsLockButton.Content = "Lock File Settings";
    }

    private void ToggleFileSettingSectionHelper()
    {
        if (_fileSettingsSectionEnabled)
        {
            LockFileSettingSection();
        } else {
            UnlockFileSettingSection();
        }
    }

    private void LockLayerSection()
    {
        SetLayerThicknessTextBox.IsEnabled = false;
        UpdateLayerThicknessButton.IsEnabled = false;
        _layerSettingsSectionEnabled = false;
        ToggleLayerSettingsLockButton.Content = "Unlock Layer Settings";
    }

    private void UnLockLayerSection()
    {
        SetLayerThicknessTextBox.IsEnabled = true;
        UpdateLayerThicknessButton.IsEnabled = true;
        _layerSettingsSectionEnabled = true;
        ToggleLayerSettingsLockButton.Content = "Lock Layer Settings";
    }

    private void ToggleLayerSectionHelper()
    {
        if (_layerSettingsSectionEnabled)
        {
            LockLayerSection();
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

    #region Helpers
    private void HomeMotorsHelper()
    {

        var msg = "Homing all motors";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);

        if (_motorPageService.buildMotor != null)
        {
            _motorPageService.HandleHomeMotor(_motorPageService.buildMotor, _motorPageService.GetBuildPositionTextBox());
        }
        else
        {
            MagnetoLogger.Log("Build Motor is null, cannot home motor.", LogFactoryLogLevel.LogLevel.ERROR);
        }

        if (_motorPageService.powderMotor != null)
        {
            _motorPageService.HandleHomeMotor(_motorPageService.powderMotor, _motorPageService.GetPowderPositionTextBox());
        }
        else
        {
            MagnetoLogger.Log("Powder Motor is null, cannot home motor.", LogFactoryLogLevel.LogLevel.ERROR);
        }

        if (_motorPageService.sweepMotor != null)
        {
            _motorPageService.HandleHomeMotor(_motorPageService.sweepMotor, _motorPageService.GetSweepPositionTextBox());
        }
        else
        {
            MagnetoLogger.Log("Sweep Motor is null, cannot home motor.", LogFactoryLogLevel.LogLevel.ERROR);
        }
    }

    private void StopMotorsHelper()
    {
        _motorPageService.GetActuationManager().HandleStopRequest(_motorPageService.buildMotor);
        _motorPageService.GetActuationManager().HandleStopRequest(_motorPageService.powderMotor);
        _motorPageService.GetActuationManager().HandleStopRequest(_motorPageService.sweepMotor);
    }
    #endregion

    private void EnableLayerMoveButton_Click(object sender, RoutedEventArgs e)
    {

    }

    private void EnableManualMoveButton_Click(object sender, RoutedEventArgs e)
    {

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

    private void ToggleLayerSettingsLockButton_Click(object sender, RoutedEventArgs e)
    {
        ToggleLayerSectionHelper();
        if (_layerSettingsSectionEnabled || _fileSettingsSectionEnabled)
        {
            LockPrintManager();
        }
        else if (!_layerSettingsSectionEnabled && !_fileSettingsSectionEnabled)
        {
            ValidateReadyToPrint();
        }
    }

    private void ToggleFileSettingsLockButton_Click(object sender, RoutedEventArgs e)
    {
        ToggleFileSettingSectionHelper();
        if (_layerSettingsSectionEnabled || _fileSettingsSectionEnabled)
        {
            LockPrintManager();
        }
        else if (!_layerSettingsSectionEnabled && !_fileSettingsSectionEnabled)
        {
            ValidateReadyToPrint();
        }
    }

    private void StopAllMotorsInCalibrationPanelButton_Click(object sender, RoutedEventArgs e)
    {
        StopMotorsHelper();
    }

    private void HomeAllMotorsButton_Click(object sender, RoutedEventArgs e)
    {
        HomeMotorsHelper();
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

    private void HomeAllMotorsInCalibrationPanelButton_Click(object sender, RoutedEventArgs e)
    {
        HomeMotorsHelper();
    }
}
