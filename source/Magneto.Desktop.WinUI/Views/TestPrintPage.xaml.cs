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
    private void InitPageServices() // combine page services initialization because motor services uses one of the UI groups
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
                                                            StartWithMarkCheckBox, PrintLayersButton, PausePrintButton, RemarkLayerButton);
        // initialize motor page service
        _motorPageService = new MotorPageService(new UIControlGroupWrapper(_calibrateMotorUIControlGroup));
        // initialize Waverunner page service
        //_waverunnerPageService = new WaverunnerPageService(PrintDirectoryInputTextBox, PrintLayersButton);
        _waverunnerPageService = new WaverunnerPageService(new UIControlGroupWrapper(_waverunnerUiControlGroup));
        // populate motor positions on page load
        _motorPageService.HandleGetAllPositions();
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
    private async Task<int> HomeIfStopFlagIsFalse(string motorName)
    {
        string? msg;
        if (_motorPageService == null)
        {
            msg = "_motorPageService is null. Cannot home motors.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);
            return 0;
        }
        else
        {
            msg = "Homing all motors";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);
        }
        if (_motorPageService.CheckMotorStopFlag(motorName))
        {
            MagnetoLogger.Log($"{motorName} motor stop flag is up cannot home motor.", LogFactoryLogLevel.LogLevel.ERROR);
            return 0;
        }
        else
        {
            await _motorPageService.HomeMotorAndUpdateUI(motorName);
            return 1;
        }
    }
    private async Task HomeMotorsHelper()
    {
        await HomeIfStopFlagIsFalse("build");
        await HomeIfStopFlagIsFalse("powder");
        await HomeIfStopFlagIsFalse("sweep");
    }
    private void StopMotorsHelper()
    {
        if (_motorPageService == null)
        {
            _ = PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", "Unable to stop motors.");
            return;
        }
        _motorPageService.StopBuildMotorAndUpdateTextBox();
        _motorPageService.StopPowderMotorAndUpdateTextBox();
        _motorPageService.StopSweepMotorAndUpdateTextBox();
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
        _motorPageService.StopBuildMotorAndUpdateTextBox();
    }
    private void StopPowderMotorButton_Click(object sender, RoutedEventArgs e)
    {
        if (_motorPageService == null)
        {
            _ = PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", "Unable to stop powder motor.");
            return;
        }
        _motorPageService.StopPowderMotorAndUpdateTextBox();
    }
    private void StopSweepMotorButton_Click(object sender, RoutedEventArgs e)
    {
        if (_motorPageService == null)
        {
            _ = PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", "Unable to stop sweep motor.");
            return;
        }
        _motorPageService.StopSweepMotorAndUpdateTextBox();
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
    private void EnableMotorsButton_Click(object sender, RoutedEventArgs e)
    {
        if (_motorPageService == null)
        {
            _ = PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", "Unable to enable motors.");
            return;
        }
        _motorPageService.EnableMotors();
        UnlockCalibrationPanel();
    }
    #endregion
    #endregion

    /*
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
                    _ = _waverunnerPageService.MarkEntityAsync();
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
    */

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
    private void PausePrintButton_Click(object sender, RoutedEventArgs e)
    {
        if (_waverunnerPageService == null)
        {
            _ = PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", "Lost connection to Waverunner. Cannot pause print.");
            return;
        }
        // stop mark
        _waverunnerPageService.StopMark(this.Content.XamlRoot);
        // stop motors
        StopMotorsHelper();
        // TODO: Update print status to "paused"

    }
    private void RemarkLayerButton_Click(object sender, RoutedEventArgs e)
    {
        // TODO: implement remarking
    }
    #endregion


    #endregion

    #region Manage Text Population & Clearing
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

    private async void MarkButton_Click(object sender, RoutedEventArgs e)
    {
        int res;
        double thickness;
        double power;
        double scanSpeed;
        double hatchSpacing;
        double amplifier;
        Int64 slicesToMark;
        var startWithMark = StartWithMarkCheckBox.IsEnabled;
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
        if (_motorPageService == null)
        {
            var msg = $"Cannot print layer. Motor page service is null.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
            return;
        }
        // Print requested layers
        MagnetoLogger.Log($"Printing {slicesToMark} layers, each {thickness}mm thick, at {power}W, scan speed equal to {scanSpeed}mm/s, and {hatchSpacing}mm hatch spacing. Powder amplifier for each layer is: {amplifier}.", LogFactoryLogLevel.LogLevel.ERROR);
        for (var i = 0; i < slicesToMark; i++)
        {
            await ViewModel.PrintLayer(_motorPageService, startWithMark, thickness, power, scanSpeed, hatchSpacing, amplifier, this.Content.XamlRoot);
            PopulatePageText();
        }
        _ = PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Done!", "Requested layer(s) printed.");
    }
    private async void DeletePrintButton_Click(object sender, RoutedEventArgs e)
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

    private bool PAUSE_REQUESTED;
    // TODO: Remove after testing
    private async void TEST_Click(object sender, RoutedEventArgs e)
    {
        var target1 = 10;
        var target2 = 20;
        bool movePositive = true;
        if (_motorPageService == null)
        {
            return;
        }
        /*
        await _motorPageService.GetMotorService().GetBuildMotor().ReadErrors();
        await _motorPageService.GetMotorService().GetPowderMotor().ReadErrors();
        await _motorPageService.GetMotorService().GetSweepMotor().ReadErrors();
        
        Controller buildSupplyController = Controller.BUILD_AND_SUPPLY;
        Controller sweepController = Controller.SWEEP;
        var buildAxis = _motorPageService.GetMotorService().GetBuildMotor().GetAxis();
        var powderAxis = _motorPageService.GetMotorService().GetPowderMotor().GetAxis();
        var sweepAxis = _motorPageService.GetMotorService().GetSweepMotor().GetAxis();

        var prog1 = _motorPageService.GetMotorService().GetBuildMotor().WriteAbsMoveProgram(target1, false);
        var prog2 = _motorPageService.GetMotorService().GetPowderMotor().WriteAbsMoveProgram(target1, false);
        var prog3 = _motorPageService.GetMotorService().GetSweepMotor().WriteAbsMoveProgram(target1, true); // sweep moves in positive direction
        var prog4 = _motorPageService.GetMotorService().GetBuildMotor().WriteAbsMoveProgram(target2, false);
        var prog5 = _motorPageService.GetMotorService().GetPowderMotor().WriteAbsMoveProgram(target2, false);
        var prog6 = _motorPageService.GetMotorService().GetSweepMotor().WriteAbsMoveProgram(target2, true);
        
        // add last command first
        _motorPageService.GetCommandQueueManger().AddProgramToFront(prog6, sweepController, sweepAxis);
        _motorPageService.GetCommandQueueManger().AddProgramToFront(prog5, buildSupplyController, powderAxis);
        _motorPageService.GetCommandQueueManger().AddProgramToFront(prog4, buildSupplyController, buildAxis);
        _motorPageService.GetCommandQueueManger().AddProgramToFront(prog3, sweepController, sweepAxis);
        _motorPageService.GetCommandQueueManger().AddProgramToFront(prog2, buildSupplyController, powderAxis);
        _motorPageService.GetCommandQueueManger().AddProgramToFront(prog1, buildSupplyController, buildAxis);

        // TODO: translate into motor page service commands
        while (_motorPageService.GetCommandQueueManger().programLinkedList.Count > 0 && !PAUSE_REQUESTED)
        {
            string[] runProg;
            Controller controller;
            int axis;
            (runProg, controller, axis) = _motorPageService.GetCommandQueueManger().GetFirstProgram();

            if (runProg != null)
            {
                if (controller == Controller.BUILD_AND_SUPPLY)
                {
                    if (axis == 1)
                    {
                        _motorPageService.GetMotorService().GetBuildMotor().SendProgram(runProg);

                        while (await _motorPageService.GetMotorService().GetBuildMotor().IsProgramRunningAsync())
                        {
                            await Task.Delay(100);
                        }
                    }
                    else // axis == 2
                    {
                        _motorPageService.GetMotorService().GetPowderMotor().SendProgram(runProg);

                        while (await _motorPageService.GetMotorService().GetPowderMotor().IsProgramRunningAsync())
                        {
                            await Task.Delay(100);
                        }
                    }
                }
                else // sweep controller
                {
                    _motorPageService.GetMotorService().GetSweepMotor().SendProgram(runProg);

                    while (await _motorPageService.GetMotorService().GetSweepMotor().IsProgramRunningAsync())
                    {
                        await Task.Delay(100);
                    }
                }
            }
        }
        */
        // TODO: Test update!
        await _motorPageService.ReadAllErrors();
        var prog1 = _motorPageService.WriteAbsMoveProgramForBuildMotor(target1, !movePositive);
        var prog2 = _motorPageService.WriteAbsMoveProgramForPowderMotor(target1, !movePositive);
        var prog3 = _motorPageService.WriteAbsMoveProgramForSweepMotor(target1, movePositive); // sweep moves in positive direction
        var prog4 = _motorPageService.WriteAbsMoveProgramForBuildMotor(target2, !movePositive);
        var prog5 = _motorPageService.WriteAbsMoveProgramForPowderMotor(target2, !movePositive);
        var prog6 = _motorPageService.WriteAbsMoveProgramForSweepMotor(target2, movePositive); // sweep moves in positive direction

        _motorPageService.AddProgramFront(buildMotorName, prog6);
        _motorPageService.AddProgramFront(powderMotorName, prog5);
        _motorPageService.AddProgramFront(sweepMotorName, prog4);
        _motorPageService.AddProgramFront(buildMotorName, prog3);
        _motorPageService.AddProgramFront(powderMotorName, prog2);
        _motorPageService.AddProgramFront(sweepMotorName, prog1);

        while (_motorPageService.GetNumberOfPrograms() > 0 && !PAUSE_REQUESTED)
        {
            var result = _motorPageService.GetFirstProgram();
            if (result.HasValue)
            {
                var (runProg, controller, axis) = result.Value;
                if (controller == Controller.BUILD_AND_SUPPLY)
                {
                    if (axis == 1)
                    {
                        _motorPageService.SendProgram(buildMotorName, runProg);
                        while (await _motorPageService.IsProgramRunningAsync(buildMotorName))
                        {
                            await Task.Delay(100);
                        }
                    }
                    else // axis == 2
                    {
                        _motorPageService.SendProgram(powderMotorName, runProg);
                        while (await _motorPageService.IsProgramRunningAsync(powderMotorName))
                        {
                            await Task.Delay(100);
                        }
                    }
                }
                else // sweep controller
                {
                    _motorPageService.SendProgram(sweepMotorName, runProg);
                    while (await _motorPageService.IsProgramRunningAsync(sweepMotorName))
                    {
                        await Task.Delay(100);
                    }
                }
            }
        }
    }
    private void StopTEST_Click(object sender, RoutedEventArgs e)
    {
        PAUSE_REQUESTED = true;
        if (_motorPageService != null)
        {
            _motorPageService.StopAllMotors();
        }
    }
}
