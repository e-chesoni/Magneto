using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Magneto.Desktop.WinUI.Core.Contracts.Services;
using Magneto.Desktop.WinUI.Core.Models.Print;
using Magneto.Desktop.WinUI.Core.Models.Motors;
using Magneto.Desktop.WinUI.Core.Models;
using Magneto.Desktop.WinUI.Core;
using Magneto.Desktop.WinUI.Popups;
using Magneto.Desktop.WinUI.Services;
using Magneto.Desktop.WinUI.ViewModels;
using SAMLIGHT_CLIENT_CTRL_EXLib;
using static Magneto.Desktop.WinUI.Core.Models.Print.ProgramsManager;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using Magneto.Desktop.WinUI.Helpers;
using Magneto.Desktop.WinUI.Models.UIControl;

namespace Magneto.Desktop.WinUI.Models.PageStates.TestPrintPageStateManagement;
public class TestPrintPageStateMachine
{
    #region Motor Variables

    private StepperMotor? _powderMotor;

    private StepperMotor? _buildMotor;

    private StepperMotor? _sweepMotor;

    private ProgramsManager? _bm;

    private bool _powderMotorSelected = false;

    private bool _buildMotorSelected = false;

    private bool _sweepMotorSelected = false;

    private bool _movingMotorToTarget = false;

    private bool _calibrationPanelEnabled = true;

    private bool _fileSettingsSectionEnabled = true;

    private bool _layerSettingsSectionEnabled = true;

    private bool _printPanelEnabled = true;

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


    #region Layer Thickness Variables

    private double _layerThickness;

    #endregion

    private MotorPageService _motorPageService;
    private WaverunnerPageService _waverunnerPageService;
    public MissionControl? _missionControl { get; set; }

    #region UI Calibrate Variables
    private Button SelectBuildMotorButton;
    private Button SelectPowderMotorButton;
    private Button SelectSweepMotorButton;
    private Button SelectBuildInPrintButton;
    private Button SelectPowderInPrintButton;
    private Button SelectSweepInPrintButton;
    private TextBox BuildMotorCurrentPositionTextBox;
    private TextBox PowderMotorCurrentPositionTextBox;
    private TextBox SweepMotorCurrentPositionTextBox;
    private TextBox BuildMotorStepTextBox;
    private TextBox PowderMotorStepTextBox;
    private TextBox SweepMotorStepTextBox;

    private Button GetBuildMotorCurrentPositionButton;
    private Button StepBuildMotorUpButton;
    private Button StepBuildMotorDownButton;
    private Button GetPowderMotorCurrentPositionButton;
    private Button StepPowderMotorUpButton;
    private Button StepPowderMotorDownButton;
    private Button GetSweepMotorCurrentPositionButton;
    private Button StepSweepMotorUpButton;
    private Button StepSweepMotorDownButton;

    private Button ToggleCalibrationPanelLockButton;

    #endregion

    #region UI Settings Variables
    private TextBox JobFileSearchDirectoryTextBox;
    private Button UpdateDirectoryButton;
    private TextBox JobFileNameTextBox;
    private Button GetJobButton;
    private Button UseDefaultJobButton;
    private Button ToggleFileSettingsLockButton;

    private TextBox SetLayerThicknessTextBox;
    private Button UpdateLayerThicknessButton;
    private Button ToggleLayerSettingsLockButton;

    #endregion

    #region UI In Print Variables
    private Button EnableLayerMoveButton;
    private Button MoveToNextLayerStartPositionButton;

    private Button EnableManualMoveButton;

    private Button IncrementBuildButton;
    private Button DecrementBuildButton;

    private Button IncrementPowderButton;
    private Button DecrementPowderButton;

    private Button SweepLeftButton;
    private Button SweepRightButton;

    private Button HomeAllMotorsButton;

    #endregion

    private UIControlGroupMotors _calibrateMotorUIControlGroup { get; set; }
    private UIControlGroupMotors _inPrintMotorUIControlGroup { get; set; }

    #region Test Page Setup

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

    public TestPrintPageStateMachine(MissionControl missionControl, MotorPageService motorPageService, WaverunnerPageService waverunnerPageService)
    {
        _missionControl = missionControl;
        _motorPageService = motorPageService;
        _waverunnerPageService = waverunnerPageService;

        //TODO: enable calibrate; disable settings and print
        ToggleFileSettingSectionHelper();
        ToggleLayerSectionHelper();
        LockPrintManager();

        var msg = "Initialized TestPrintPageStateMachine";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.DEBUG);
        //MagnetoSerialConsole.LogAvailablePorts();
    }

    private void SetDefaultPrintSettings(TextBox SetLayerThicknessTextBox)
    {
        _layerThickness = MagnetoConfig.GetDefaultPrintThickness();
        SetLayerThicknessTextBox.Text = _layerThickness.ToString();
    }

    #endregion

    #region Lock, Unlock, and Toggle Helpers

    private void UnlockCalibrationPanel()
    {
        _motorPageService.UnlockCalibrationPanel();
        ToggleCalibrationPanelLockButton.Content = "Unlock Calibration";
    }

    private void LockCalibrationPanel()
    {
        _motorPageService.LockCalibrationPanel();
        ToggleCalibrationPanelLockButton.Content = "Lock Calibration";
    }



    public void LockFileSettingSection()
    {
        JobFileSearchDirectoryTextBox.IsEnabled = false;
        UpdateDirectoryButton.IsEnabled = false;
        JobFileNameTextBox.IsEnabled = false;
        GetJobButton.IsEnabled = false;
        UseDefaultJobButton.IsEnabled = false;
        _fileSettingsSectionEnabled = false;


        ToggleFileSettingsLockButton.Content = "Unlock File Settings";
    }

    public void UnlockFileSettingSection()
    {
        JobFileSearchDirectoryTextBox.IsEnabled = true;
        UpdateDirectoryButton.IsEnabled = true;
        JobFileNameTextBox.IsEnabled = true;
        GetJobButton.IsEnabled = true;
        UseDefaultJobButton.IsEnabled = true;
        _fileSettingsSectionEnabled = true;
        ToggleFileSettingsLockButton.Content = "Lock File Settings";
    }

    private void ToggleFileSettingSectionHelper()
    {
        if (_fileSettingsSectionEnabled)
        {
            LockFileSettingSection();
        }
        else
        {
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
        }
        else
        {
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
}
