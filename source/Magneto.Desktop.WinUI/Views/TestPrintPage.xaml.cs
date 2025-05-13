using Magneto.Desktop.WinUI.Core;
using Magneto.Desktop.WinUI.Core.Contracts.Services;
using Magneto.Desktop.WinUI.Core.Models;
using Magneto.Desktop.WinUI.Models.UIControl;
using Magneto.Desktop.WinUI.Popups;
using Magneto.Desktop.WinUI.Services;
using Magneto.Desktop.WinUI.ViewModels;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System.Diagnostics;
using Windows.Storage.Pickers;
using WinRT.Interop;
using MongoDB.Driver;
using static Magneto.Desktop.WinUI.Core.Models.Constants.MagnetoConstants;
using Magneto.Desktop.WinUI.Core.Models.Print;
using static Magneto.Desktop.WinUI.Core.Models.Print.RoutineStateMachine;
using System.Threading.Tasks;
using Magneto.Desktop.WinUI.Core.Models.States.PrintStates;
using static Magneto.Desktop.WinUI.Core.Models.States.PrintStates.PrintStateMachine;

namespace Magneto.Desktop.WinUI.Views;

/// <summary>
/// Test print page
/// </summary>
public sealed partial class TestPrintPage : Page
{
    // core
    private MissionControl _missionControl { get; set; }
    public TestPrintViewModel ViewModel { get; }
    private MotorPageService? _motorPageService;
    private WaverunnerPageService? _waverunnerPageService;

    // UI control groups
    private UIControlGroupMotors? _calibrateMotorUIControlGroup { get; set; }
    private UIControlGroupWaverunner? _waverunnerUiControlGroup { get; set; }

    // motor names for motor service calls
    private static readonly string buildMotorName = "build";
    private static readonly string powderMotorName = "powder";
    private static readonly string sweepMotorName = "sweep";

    // boundaries for print settings
    private double _layerThicknessLower;
    private double _layerThicknessUpper;
    private double _laserPowerLower;
    private double _laserPowerUpper;
    private double _scanSpeedLower;
    private double _scanSpeedUpper;
    private double _supplyAmplifierLower;
    private double _supplyAmplifierUpper;

    private bool KILL_OPERATION; // TODO: may remove later; used in old layer move to check for e-stop

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
        // set up flags
        KILL_OPERATION = false;
        //this.motorService = motorService;
    }
    #endregion

    #region Page Initialization Methods
    private async void InitPageServices() // combine page services initialization because motor services uses one of the UI groups
    {
        // UI page groups
        _calibrateMotorUIControlGroup = new UIControlGroupMotors(SelectBuildMotorButton, SelectPowderMotorButton, SelectSweepMotorButton,
                                                                BuildMotorCurrentPositionTextBox, PowderMotorCurrentPositionTextBox, SweepMotorCurrentPositionTextBox,
                                                                GetBuildMotorCurrentPositionButton, GetPowderMotorCurrentPositionButton, GetSweepMotorCurrentPositionButton,
                                                                BuildMotorAbsPositionTextBox, PowderMotorAbsPositionTextBox, SweepMotorAbsPositionTextBox,
                                                                MoveBuildToAbsPositionButton, MovePowderToAbsPositionButton, MoveSweepToAbsPositionButton,
                                                                BuildMotorStepTextBox, PowderMotorStepTextBox, SweepMotorStepTextBox,
                                                                StepBuildMotorUpButton, StepBuildMotorDownButton, StepPowderMotorUpButton, StepPowderMotorDownButton, StepSweepMotorLeftButton, StepSweepMotorRightButton,
                                                                StopBuildMotorButton, StopPowderMotorButton, StopSweepMotorButton,
                                                                HomeAllMotorsButton, EnableMotorsButton, StopMotorsButton);
        _waverunnerUiControlGroup = new UIControlGroupWaverunner(PrintDirectoryInputTextBox,
                                                            LayerTextBlock, FileNameTextBlock, LayerThicknessTextBlock, LaserPowerTextBlock, ScanSpeedTextBlock, HatchSpacingTextBlock, EnergyDensityTextBlock, SlicesToMarkTextBlock, SupplyAmplifierTextBlock, 
                                                            LayerTextBox, FileNameTextBox, LayerThicknessTextBox, LaserPowerTextBox, ScanSpeedTextBox, HatchSpacingTextBox, EnergyDensityTextBox, SlicesToMarkTextBox, SupplyAmplifierTextBox,
                                                            StartWithMarkCheckBox, PlayButton, PauseButton, RemarkLayerButton);
        // initialize motor page service
        _motorPageService = new MotorPageService(new UIControlGroupWrapper(_calibrateMotorUIControlGroup), ViewModel.GetRoutineStateMachine());
        // initialize Waverunner page service
        //_waverunnerPageService = new WaverunnerPageService(PrintDirectoryInputTextBox, PrintLayersButton);
        _waverunnerPageService = new WaverunnerPageService(new UIControlGroupWrapper(_waverunnerUiControlGroup));
        // populate motor positions on page load
        await _motorPageService.HandleGetAllPositionsAsync();
        // populate changeable pen settings
        if (_waverunnerPageService.WaverunnerRunning() != 0)
        {
            // get pen settings
            LaserPowerTextBox.Text = _waverunnerPageService.GetLaserPower().ToString();
            ScanSpeedTextBox.Text = _waverunnerPageService.GetMarkSpeed().ToString();
        }
        else
        {
            // use default power and scan speed
            LaserPowerTextBox.Text = _waverunnerPageService.GetDefaultLaserPower().ToString();
            ScanSpeedTextBox.Text = _waverunnerPageService.GetDefaultMarkSpeed().ToString();
        }
        HatchSpacingTextBox.Text = _waverunnerPageService.GetDefaultHatchSpacing().ToString();
        SupplyAmplifierTextBox.Text = _waverunnerPageService.GetDefaultSupplyAmplifier().ToString();

        // set upper and lower bounds for print settings
        _layerThicknessLower = 0.005;
        _layerThicknessUpper = 2.0;
        _laserPowerLower = 50;
        _laserPowerUpper = 500;
        _scanSpeedLower = 100;
        _scanSpeedUpper = 3000;
        _supplyAmplifierLower = 0;
        _supplyAmplifierUpper = 4;
    }
    #endregion

    #region Navigation Methods
    /// <summary>
    /// Handle page startup tasks
    /// </summary>
    /// <param name="e"></param>
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        InitPageServices();
    }
    #endregion

    #region Helpers
    private async Task HomeMotorsHelper()
    {
        string? msg;
        if (_motorPageService == null)
        {
            msg = "_motorPageService is null. Cannot home motors.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);
            return;
        }
        await _motorPageService.HomeAllMotorsAsync();
        // WARNING: if you home one at a time, you stay in this call stack;
        // when you resume any motion, the first thing it will do is resume the calls here
        return;
    }
    private async void StopMotorsHelper()
    {
        if (_motorPageService == null)
        {
            _ = PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", "Unable to stop motors.");
            return;
        }
        //_motorPageService.StopAllMotorsClearProgramList();
        _motorPageService.StopBuildMotorAndDisableControls();
        _motorPageService.StopPowderMotorAndDisableControls();
        _motorPageService.StopSweepMotorAndDisbleControls();
        await _motorPageService.HandleGetAllPositionsAsync();
    }
    #endregion

    #region Calibration Panel Methods
    #region Calibration Selectors
    private void SelectBuildMotorButton_Click(object sender, RoutedEventArgs e)
    {
        if (_motorPageService == null)
        {
            _ = PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", "Unable to select build motor.");
            return;
        }
        _motorPageService.SelectBuildMotor();
    }
    private void SelectPowderMotorButton_Click(object sender, RoutedEventArgs e)
    {
        if (_motorPageService == null)
        {
            _ = PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", "Unable to select powder motor.");
            return;
        }
        _motorPageService.SelectPowderMotor();
    }
    private void SelectSweepMotorButton_Click(object sender, RoutedEventArgs e)
    {
        if (_motorPageService == null)
        {
            _ = PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", "Unable to select sweep motor.");
            return;
        }
        _motorPageService.SelectSweepMotor();
    }
    #endregion

    #region Calibration Position Getters
    private async void GetBuildMotorCurrentPositionButton_Click(object sender, RoutedEventArgs e)
    {
        if (_motorPageService == null)
        {
            _ = PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", "Unable to get build motor position.");
            return;
        }
        await _motorPageService.HandleGetPosition(buildMotorName, _motorPageService.GetBuildPositionTextBox(), true);
    }
    private async void GetPowderMotorCurrentPositionButton_Click(object sender, RoutedEventArgs e)
    {
        if (_motorPageService == null)
        {
            _ = PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", "Unable to get powder motor position.");
            return;
        }
        await _motorPageService.HandleGetPosition(powderMotorName, _motorPageService.GetPowderPositionTextBox(), true);
    }
    private async void GetSweepMotorCurrentPositionButton_Click(object sender, RoutedEventArgs e)
    {
        if (_motorPageService == null)
        {
            _ = PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", "Unable to get sweep motor position");
            return;
        }
        await _motorPageService.HandleGetPosition(sweepMotorName, _motorPageService.GetSweepPositionTextBox(), true);
    }
    #endregion

    #region Calibration Absolute Movers
    private void MoveBuildToAbsPositionButton_Click(object sender, RoutedEventArgs e)
    {
        if (_motorPageService == null)
        {
            _ = PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", "Unable to move build motor.");
            return;
        }
        _motorPageService.HandleAbsMove(buildMotorName, _motorPageService.GetBuildAbsMoveTextBox(), this.Content.XamlRoot);
    }
    private void MovePowderToAbsPositionButton_Click(object sender, RoutedEventArgs e)
    {
        if (_motorPageService == null)
        {
            _ = PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", "Unable to move powder motor.");
            return;
        }
        _motorPageService.HandleAbsMove(powderMotorName, _motorPageService.GetPowderAbsMoveTextBox(), this.Content.XamlRoot);
    }
    private void MoveSweepToAbsPositionButton_Click(object sender, RoutedEventArgs e)
    {
        if (_motorPageService == null)
        {
            _ = PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", "Unable to move sweep motor.");
            return;
        }
        _motorPageService.HandleAbsMove(sweepMotorName, _motorPageService.GetSweepAbsMoveTextBox(), this.Content.XamlRoot);
    }
    #endregion

    #region Calibration Relative Movers
    private void StepBuildMotorUpButton_Click(object sender, RoutedEventArgs e)
    {
        if (_motorPageService == null)
        {
            _ = PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", "Unable to build motor up.");
            return;
        }
        _motorPageService.HandleRelMove(buildMotorName, _motorPageService.GetBuildStepTextBox(), true, this.Content.XamlRoot);
    }
    private void StepBuildMotorDownButton_Click(object sender, RoutedEventArgs e)
    {
        if (_motorPageService == null)
        {
            _ = PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", "Unable to move build motor down.");
            return;
        }
        _motorPageService.HandleRelMove(buildMotorName, _motorPageService.GetBuildStepTextBox(), false, this.Content.XamlRoot);
    }
    private void StepPowderMotorUpButton_Click(object sender, RoutedEventArgs e)
    {
        if (_motorPageService == null)
        {
            _ = PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", "Unable to move powder motor up.");
            return;
        }
        _motorPageService.HandleRelMove(powderMotorName, _motorPageService.GetPowderStepTextBox(), true, this.Content.XamlRoot);
    }
    private void StepPowderMotorDownButton_Click(object sender, RoutedEventArgs e)
    {
        if (_motorPageService == null)
        {
            _ = PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", "Unable to move powder motor down.");
            return;
        }
        _motorPageService.HandleRelMove(powderMotorName, _motorPageService.GetPowderStepTextBox(), false, this.Content.XamlRoot);
    }
    private void StepSweepMotorLeftButton_Click(object sender, RoutedEventArgs e)
    {
        if(_motorPageService == null)
        {
            _ = PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", "Unable to move sweep motor left.");
            return;
        }
        _motorPageService.HandleRelMove(sweepMotorName, _motorPageService.GetSweepStepTextBox(), true, this.Content.XamlRoot);
    }
    private void StepSweepMotorRightButton_Click(object sender, RoutedEventArgs e)
    {
        if (_motorPageService == null)
        {
            _ = PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", "Unable to move sweep motor right.");
            return;
        }
        _motorPageService.HandleRelMove(sweepMotorName, _motorPageService.GetSweepStepTextBox(), false, this.Content.XamlRoot);
    }
    #endregion

    #region Calibration Homing
    private async void HomeAllMotorsButton_Click(object sender, RoutedEventArgs e)
    {
        await HomeMotorsHelper();
    }
    #endregion

    #region Calibration Stoppers
    private void StopBuildMotorButton_Click(object sender, RoutedEventArgs e)
    {
        if (_motorPageService == null)
        {
            _ = PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", "Unable to stop build motor.");
            return;
        }
        _motorPageService.StopBuildMotorAndDisableControls();
    }
    private void StopPowderMotorButton_Click(object sender, RoutedEventArgs e)
    {
        if (_motorPageService == null)
        {
            _ = PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", "Unable to stop powder motor.");
            return;
        }
        _motorPageService.StopPowderMotorAndDisableControls();
    }
    private void StopSweepMotorButton_Click(object sender, RoutedEventArgs e)
    {
        if (_motorPageService == null)
        {
            _ = PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", "Unable to stop sweep motor.");
            return;
        }
        _motorPageService.StopSweepMotorAndDisbleControls();
    }
    private void StopMotorsButton_Click(object sender, RoutedEventArgs e)
    {
        StopMotorsHelper();
        LockCalibrationPanel();
    }
    #endregion

    #region Calibration Enablers
    // TODO: Implement enablers
    private void EnableBuildMotorButton_Click(object sender, RoutedEventArgs e)
    {
        if (_motorPageService == null)
        {
            _ = PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", "Cannot enable build motor.");
            return;
        }
        _motorPageService.EnableBuildMotor();
    }
    private void EnablePowderMotorButton_Click(object sender, RoutedEventArgs e)
    {
        if (_motorPageService == null)
        {
            _ = PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", "Cannot enable powder motor.");
            return;
        }
        _motorPageService.EnablePowderMotor();
    }
    private void EnableSweepMotorButton_Click(object sender, RoutedEventArgs e)
    {
        if (_motorPageService == null)
        {
            _ = PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", "Cannot enable sweep motor.");
            return;
        }
        _motorPageService.EnableSweepMotor();
    }
    private async void EnableMotorsButton_Click(object sender, RoutedEventArgs e)
    {
        if (_motorPageService == null)
        {
            _ = PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", "Unable to enable motors.");
            return;
        }
        //_motorPageService.ResumeProgram();

        // Show interactive warning
        var confirmed = await PopupInfo.ShowConfirmationDialog(
            this.Content.XamlRoot,
            "Warning",
            "Found a layer print in the queue. If you enable the calibration panel, the current layer print will be erased. Continue?"
        );

        if (!confirmed)
        {
            MagnetoLogger.Log("❌ User canceled enabling motors.", LogFactoryLogLevel.LogLevel.WARN);
            return;
        }
        // Proceed if confirmed
        // TODO:handle cancellation (clear rsm program list) and get current positions
        _motorPageService.ClearProgramList(); // TODO: Fix: still completing layer move
        await _motorPageService.HandleGetAllPositionsAsync();
        _motorPageService.EnableAllMotors();
        UnlockCalibrationPanel();
    }
    #endregion
    #endregion

    #region Locking
    private void UnlockCalibrationPanel()
    {
        if (_motorPageService == null)
        {
            _ = PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", "Cannot unlock calibration panel.");
            return;
        }
        _motorPageService.UnlockCalibrationPanel();
    }
    private void LockCalibrationPanel()
    {
        if (_motorPageService == null)
        {
            _ = PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", "Cannot lock calibration panel.");
            return;
        }
        _motorPageService.LockCalibrationPanel();
    }
    #endregion

    #region Print Methods
    #region Text Validation
    /// <summary>
    /// Checks print settings text boxes; if all required parameters are entered and valid, enables mark button
    /// </summary>
    /// <returns></returns>
    private int ReadyToPrint()
    {
        int res;
        double thickness;
        double power;
        double scanSpeed;
        double hatchSpacing;
        var layerInputEntered = (LayerTextBox != null && !string.IsNullOrWhiteSpace(LayerTextBox.Text));
        var fileNameInputEntered = (FileNameTextBox != null && !string.IsNullOrWhiteSpace(FileNameTextBox.Text));
        var thicknessInputEntered = (LayerThicknessTextBox != null && !string.IsNullOrWhiteSpace(LayerThicknessTextBox.Text));
        var powerInputEntered = (LaserPowerTextBox != null && !string.IsNullOrWhiteSpace(LaserPowerTextBox.Text));
        var scanSpeedInputEntered = (ScanSpeedTextBox != null && !string.IsNullOrWhiteSpace(ScanSpeedTextBox.Text));
        var slicesToMarkInputEntered = (SlicesToMarkTextBox != null && !string.IsNullOrWhiteSpace(SlicesToMarkTextBox.Text));
        var supplyAmplifierInputEntered = (SupplyAmplifierTextBox != null && !string.IsNullOrWhiteSpace(SupplyAmplifierTextBox.Text));
        if (thicknessInputEntered && powerInputEntered && scanSpeedInputEntered)
        {
            (res, thickness) = ConvertTextBoxTextToDouble(LayerThicknessTextBox);
            if (res == 0)
            {
                var msg = $"Layer thickness text box input is invalid.";
                MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
                return 0;
            }
            (res, power) = ConvertTextBoxTextToDouble(LaserPowerTextBox);
            if (res == 0)
            {
                var msg = $"Power text box input is invalid.";
                MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
                return 0;
            }
            (res, scanSpeed) = ConvertTextBoxTextToDouble(ScanSpeedTextBox);
            if (res == 0)
            {
                var msg = $"Scan speed text box input is invalid.";
                MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
                return 0;
            }
            (res, hatchSpacing) = ConvertTextBoxTextToDouble(HatchSpacingTextBox);
            if (res == 0)
            {
                var msg = $"Hatching text box input is invalid.";
                MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
                return 0;
            }
            // round energy density to 2 decimals
            EnergyDensityTextBox.Text = Math.Round(_waverunnerPageService.GetEnergyDensity(thickness, power, scanSpeed, hatchSpacing),2).ToString();
        }
        // if all text boxes are not empty, check for validity, else return 0
        if (layerInputEntered && fileNameInputEntered && thicknessInputEntered && powerInputEntered && scanSpeedInputEntered && slicesToMarkInputEntered && supplyAmplifierInputEntered)
        {
            return CheckForValidInputs();
        }
        return 0;
    }
    private int CheckForValidInputs()
    {
        // if one text box is invalid, return 0
        if ((TextBoxInputIsValid(LayerThicknessTextBox, _layerThicknessLower, _layerThicknessUpper) <= 0) || 
            (TextBoxInputIsValid(LaserPowerTextBox, _laserPowerLower, _laserPowerUpper) <= 0) || 
            (TextBoxInputIsValid(ScanSpeedTextBox, _scanSpeedLower, _scanSpeedUpper) <= 0))
        {
            return 0;
        }
        return 1;
    }
    private int TextBoxInputIsValid(TextBox textBox, double lowerBound, double upperBound)
    {
        if (double.TryParse(textBox.Text, out var value))
        {
            if (value <= lowerBound || value > upperBound)
            {
                return 0;
            }
            else
            {
                return 1;
            }
        }
        else
        {
            return 0;
        }
    }
    private void LayerThicknessTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        string msg;
        var layerThicknessIsValid = TextBoxInputIsValid(LayerThicknessTextBox, _layerThicknessLower, _layerThicknessUpper);
        if (_waverunnerPageService is null)
        {
            return;
        }
        if (layerThicknessIsValid == 0)
        {
            msg = $"⚠️ Layer thickness out of range (should {_layerThicknessLower}-{_layerThicknessUpper} mm)";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
            _ = PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", msg);
            LayerThicknessTextBox.Foreground = new SolidColorBrush(Colors.Red);
            _waverunnerPageService.LockMarking();
        }
        else if (layerThicknessIsValid == -1)
        {
            msg = "⚠️ Invalid number entered for layer thickness.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
            _ = PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", msg);
            LayerThicknessTextBox.Foreground = new SolidColorBrush(Colors.Red);
            _waverunnerPageService.LockMarking();
        }
        else
        {
            // Valid input, reset text color back to normal (white)
            LayerThicknessTextBox.Foreground = new SolidColorBrush(Colors.WhiteSmoke);
            if (ReadyToPrint() == 1)
            {
                _waverunnerPageService.UnlockMarking();
            }
        }
    }
    private void LaserPowerTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        string msg;
        var powerIsValid = TextBoxInputIsValid(LaserPowerTextBox, _laserPowerLower, _laserPowerUpper);
        if (_waverunnerPageService is null)
        {
            return;
        }
        if (powerIsValid == 0)
        {
            msg = $"⚠️ Laser power out of range (should be {_laserPowerLower}–{_laserPowerUpper} W)";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
            _ = PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", msg);
            LaserPowerTextBox.Foreground = new SolidColorBrush(Colors.Red);
            // TODO: disable marking buttons
            _waverunnerPageService.LockMarking();
        }
        else if (powerIsValid == -1)
        {
            msg = "⚠️ Invalid number entered for laser power.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
            _ = PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", msg);
            LaserPowerTextBox.Foreground = new SolidColorBrush(Colors.Red);
            // TODO: disable marking buttons
            _waverunnerPageService.LockMarking();
        }
        else
        {
            // Valid input, reset text color back to normal (white)
            LaserPowerTextBox.Foreground = new SolidColorBrush(Colors.WhiteSmoke);
            if (ReadyToPrint() == 1)
            {
                _waverunnerPageService.UnlockMarking();
            }
        }
    }
    private void ScanSpeedTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        string msg;
        var scanSpeedIsValid = TextBoxInputIsValid(ScanSpeedTextBox, _scanSpeedLower, _scanSpeedUpper);
        if (_waverunnerPageService is null)
        {
            return;
        }
        if (scanSpeedIsValid == 0)
        {
            msg = $"⚠️ Scan speed out of range (should be {_scanSpeedLower}-{_scanSpeedUpper} mm/s)";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
            _ = PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", msg);
            ScanSpeedTextBox.Foreground = new SolidColorBrush(Colors.Red);
            // TODO: disable marking buttons
            _waverunnerPageService.LockMarking();
        }
        else if (scanSpeedIsValid == -1)
        {
            msg = "⚠️ Invalid number entered for scan speed.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
            _ = PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", msg);
            ScanSpeedTextBox.Foreground = new SolidColorBrush(Colors.Red);
            // TODO: disable marking buttons
            _waverunnerPageService.LockMarking();
        }
        else
        {
            ScanSpeedTextBox.Foreground = new SolidColorBrush(Colors.WhiteSmoke);
            if (ReadyToPrint() == 1)
            {
                _waverunnerPageService.UnlockMarking();
            }
        }
    }
    private async void SlicesToMarkTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        string msg;
        // get slices to mark lower bound
        var totalSlices = await ViewModel.GetTotalSlicesAsync();
        var slicesMarked = await ViewModel.GetSlicesMarkedAsync();
        var slicesRemaining = totalSlices - slicesMarked;
        var slicesToMarkUpper = Convert.ToDouble(slicesRemaining); // explicit conversion is not necessary, but good to be deliberate
        // lower bound is 0 (marking no slices doesn't make much sense)
        var slicesToMarkLower = 0;
        // validate text box input
        var slicesToMarkValid = TextBoxInputIsValid(SlicesToMarkTextBox, slicesToMarkLower, slicesToMarkUpper);
        if (_waverunnerPageService is null)
        {
            return;
        }
        if (slicesToMarkValid == 0)
        {
            msg = $"⚠️ Slices to mark out of range (should be {slicesToMarkLower}-{slicesToMarkUpper} slices)";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
            _ = PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", msg);
            SlicesToMarkTextBox.Foreground = new SolidColorBrush(Colors.Red);
            _waverunnerPageService.LockMarking();
        }
        else if (slicesToMarkValid == -1)
        {
            msg = "⚠️ Invalid number entered for slices to mark.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
            _ = PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", msg);
            SlicesToMarkTextBox.Foreground = new SolidColorBrush(Colors.Red);
            _waverunnerPageService.LockMarking();
        }
        else
        {
            SlicesToMarkTextBox.Foreground = new SolidColorBrush(Colors.WhiteSmoke);
            if (ReadyToPrint() == 1)
            {
                _waverunnerPageService.UnlockMarking();
            }
        }
    }
    private void SupplyAmplifierTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        string msg;
        var scanSpeedIsValid = TextBoxInputIsValid(SupplyAmplifierTextBox, _supplyAmplifierLower, _supplyAmplifierUpper);
        if (_waverunnerPageService is null)
        {
            return;
        }
        if (scanSpeedIsValid == 0)
        {
            msg = $"⚠️ Supply amplifier out of range (should be {_supplyAmplifierLower}-{_supplyAmplifierUpper} mm/s)";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
            _ = PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", msg);
            SupplyAmplifierTextBox.Foreground = new SolidColorBrush(Colors.Red);
            // TODO: disable marking buttons
            _waverunnerPageService.LockMarking();
        }
        else if (scanSpeedIsValid == -1)
        {
            msg = "⚠️ Invalid number entered for supply amplifier.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
            _ = PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", msg);
            SupplyAmplifierTextBox.Foreground = new SolidColorBrush(Colors.Red);
            // TODO: disable marking buttons
            _waverunnerPageService.LockMarking();
        }
        else
        {
            SupplyAmplifierTextBox.Foreground = new SolidColorBrush(Colors.WhiteSmoke);
            if (ReadyToPrint() == 1)
            {
                _waverunnerPageService.UnlockMarking();
            }
        }
    }
    private void PauseButton_Click(object sender, RoutedEventArgs e)
    {
        if (_waverunnerPageService == null)
        {
            _ = PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", "Lost connection to Waverunner. Cannot pause laser.");
        }
        else
        {
            // stop mark
            _waverunnerPageService.StopMark(this.Content.XamlRoot);
        }
            
        if (_motorPageService == null)
        {
            _ = PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", "Lost connection to motors. Cannot pause motors.");
        }
        else
        {
            // pause motors
            ViewModel.PausePrint();
            //_motorPageService.PauseProgram(); // changes rsm state to pause; this is handled by psm when it changes state to pause
            // TODO: lock calibration panel (force user to re-enable to motors to use those commands; that way we get positions too)
            LockCalibrationPanel();
        }
    }
    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        // TODO: implement
        StopMotorsHelper();
    }
    private void RemarkLayerButton_Click(object sender, RoutedEventArgs e)
    {
        // TODO: implement remarking
    }
    #endregion
    #endregion

    #region Manage Text Population & Clearing
    private async void UpdatePrintAndSliceDisplayText()
    {
        var print = ViewModel.GetCurrentPrint();
        var slice = ViewModel.GetCurrentSlice(); // null
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
                        LayerTextBox.Text = slice.layer.ToString();
                        FileNameTextBox.Text = slice.fileName;
                        StatusTextBlock.Text = print?.complete == true ? "Complete" : "Incomplete";
                        SlicesMarkedTextBlock.Text = (await ViewModel.GetSlicesMarkedAsync()).ToString();
                        TotalSlicesTextBlock.Text = (await ViewModel.GetTotalSlicesAsync()).ToString();
                        // convert UTC to local time
                        var duration = print.duration;
                        var localStart = print.startTime.ToLocalTime();
                        var localEnd = print.endTime?.ToLocalTime();
                        var msg = $"📅 start: {localStart}, end: {localEnd}, duration: {print.duration}";
                        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);
                        DurationTextBlock.Text = duration?.ToString(@"hh\:mm\:ss") ?? "—";
                        await ViewModel.GetNextSliceAndUpdateDisplay();
                    }
                    else
                    {
                        MagnetoLogger.Log("❌ Slice image path is null or empty.", LogFactoryLogLevel.LogLevel.ERROR);
                        _ = PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", "Cannot find slice.");
                        return;
                    }
                }
                else
                {
                    MagnetoLogger.Log("❌ Current slice is null.", LogFactoryLogLevel.LogLevel.ERROR);
                    _ = PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", "Current slice missing.");
                    return;
                }
            }
            else
            {
                MagnetoLogger.Log("❌ Directory path is null or empty.", LogFactoryLogLevel.LogLevel.ERROR);
                _ = PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", "Cannot find directory.");
                return;
            }
        }
        else
        {
            MagnetoLogger.Log("❌ Current print is null.", LogFactoryLogLevel.LogLevel.ERROR);
            _ = PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", "Cannot find print.");
            return;
        }
    }
    private void ClearPageText()
    {
        PrintDirectoryInputTextBox.Text = "";
        PrintNameTextBlock.Text = "";
        FileNameTextBox.Text = "";
        StatusTextBlock.Text = "";
        DurationTextBlock.Text = "";
        SlicesMarkedTextBlock.Text = "";
        TotalSlicesTextBlock.Text = "";
        ViewModel.ClearData();
    }
    #endregion

    #region POC Button Methods
    private async void GetSlices_Click(object sender, RoutedEventArgs e)
    {
        await ViewModel.AddPrintToDatabaseAsync(PrintDirectoryInputTextBox.Text);
        if (ViewModel.GetCurrentSlice() != null)
        {
            if (ViewModel.GetCurrentSlice().filePath == null)
            {
                Debug.WriteLine("❌ImagePath is null.");
                return;
            }
            UpdatePrintAndSliceDisplayText();
        }
    }

    #region Text Box Text Converters
    // TODO: Move to helper class for conversions
    private (int result, double value) ConvertTextBoxTextToDouble(TextBox textBox)
    {
        if (textBox == null || !double.TryParse(textBox.Text, out _))
        {
            var msg = $"Text box input is invalid.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
            return (0, 0);
        }
        else
        {
            var val = double.Parse(textBox.Text);
            return (1, val);
        }
    }
    private (int result, Int64 value) ConvertTextBoxTextInt(TextBox textBox)
    {
        if (textBox == null || !Int64.TryParse(textBox.Text, out _))
        {
            var msg = $"Text box input is invalid.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
            return (0, 0);
        }
        else
        {
            var val = Int64.Parse(textBox.Text);
            return (1, val);
        }
    }
    #endregion

    private async void PlayButton_Click(object sender, RoutedEventArgs e)
    {
        int res;
        double thickness;
        double power;
        double scanSpeed;
        double hatchSpacing;
        double amplifier;
        Int64 slicesToMark;
        var startWithMark = StartWithMarkCheckBox.IsEnabled;
        if (_motorPageService == null)
        {
            var msg = $"Cannot print layer. Motor page service is null.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
            return;
        }
        if (startWithMark)
        {
            var msg = $"Start with mark requested.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.WARN);
        }
        // Check for valid data in required text boxes
        (res, thickness) = ConvertTextBoxTextToDouble(LayerThicknessTextBox);
        if (res == 0)
        {
            var msg = $"Layer thickness text box input is invalid.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
            return;
        }
        (res, power) = ConvertTextBoxTextToDouble(LaserPowerTextBox);
        if (res == 0)
        {
            var msg = $"Power text box input is invalid.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
            return;
        }
        (res, scanSpeed) = ConvertTextBoxTextToDouble(ScanSpeedTextBox);
        if (res == 0)
        {
            var msg = $"Scan speed text box input is invalid.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
            return;
        }
        (res, hatchSpacing) = ConvertTextBoxTextToDouble(HatchSpacingTextBox);
        if (res == 0)
        {
            var msg = $"Hatching text box input is invalid.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
            return;
        }
        (res, amplifier) = ConvertTextBoxTextToDouble(SupplyAmplifierTextBox);
        if (res == 0)
        {
            var msg = $"Supply amplifier text box input is invalid.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
            return;
        }
        (res, slicesToMark) = ConvertTextBoxTextInt(SlicesToMarkTextBox);
        if (res == 0)
        {
            var msg = $"Slices to mark text box input is invalid.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
            return;
        }
        // play button is print-related (not just motor related) so should use print state machine in view model
        await ViewModel.PrintLayer(startWithMark, thickness, power, scanSpeed, hatchSpacing, amplifier, this.Content.XamlRoot); // checks for pause state; calls resume() if paused; play() in any other state
        await _motorPageService.HandleGetAllPositionsAsync();
        UpdatePrintAndSliceDisplayText();
        _ = PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Done!", "Requested layer(s) printed.");
    }
    private async void DeletePrintButton_Click(object sender, RoutedEventArgs e)
    {
        // TODO: add guards to ask user if they're sure they want to delete this print
        if (ViewModel.GetCurrentPrint() == null)
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
    private async void BrowseButton_Click(object sender, RoutedEventArgs e)
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
        UpdatePrintAndSliceDisplayText();
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

    // TODO: Remove after testing
    private async void TEST_Click(object sender, RoutedEventArgs e)
    {
        if (_motorPageService == null)
        {
            return;
        }
        // Working!
        // TODO: think about how to convert this in to a method that writes the entire multi-layer move program,
        // then processes it
        var startWithMark = true;
        var thickness = 1;
        var power = 300;
        var scanSpeed = 800;
        var hatchSpacing = 0.12;
        var amplifier = 2;
        var numberOfLayers = 1; // TODO: figure out how to incorporate number of layers
        await ViewModel.PrintLayer(startWithMark, thickness, power, scanSpeed, hatchSpacing, amplifier, this.Content.XamlRoot);
    }
    private void StopTEST_Click(object sender, RoutedEventArgs e)
    {
        if (_motorPageService != null)
        {
            _motorPageService.StopAllMotorsClearProgramList();
        }
    }
}
