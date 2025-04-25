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
using Magneto.Desktop.WinUI.Contracts.Services;

namespace Magneto.Desktop.WinUI.Views;

/// <summary>
/// Test print page
/// </summary>
public sealed partial class TestPrintPage : Page
{
    private MissionControl _missionControl { get; set; }
    public TestPrintViewModel ViewModel { get; }
    private MotorPageService? _motorPageService;
    private WaverunnerPageService? _waverunnerPageService;
    private MotorUIControlGroup? _calibrateMotorUIControlGroup { get; set; }

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
        _calibrateMotorUIControlGroup = new MotorUIControlGroup(SelectBuildMotorButton, SelectPowderMotorButton, SelectSweepMotorButton,
                                                                BuildMotorCurrentPositionTextBox, PowderMotorCurrentPositionTextBox, SweepMotorCurrentPositionTextBox,
                                                                GetBuildMotorCurrentPositionButton, GetPowderMotorCurrentPositionButton, GetSweepMotorCurrentPositionButton,
                                                                BuildMotorAbsPositionTextBox, PowderMotorAbsPositionTextBox, SweepMotorAbsPositionTextBox,
                                                                BuildMotorStepTextBox, PowderMotorStepTextBox, SweepMotorStepTextBox,
                                                                StepBuildMotorUpButton, StepBuildMotorDownButton, StepPowderMotorUpButton, StepPowderMotorDownButton, StepSweepMotorLeftButton, StepSweepMotorRightButton,
                                                                StopBuildMotorButton, StopPowderMotorButton, StopSweepMotorButton,
                                                                HomeAllMotorsButton, StopAllMotorsButton);
        // initialize motor page service
        _motorPageService = new MotorPageService(new PrintUIControlGroupHelper(_calibrateMotorUIControlGroup));
        // initialize Waverunner page service
        _waverunnerPageService = new WaverunnerPageService(PrintDirectoryInputTextBox, PrintLayersButton);
        // populate motor positions on page load
        _motorPageService.HandleGetAllPositions();
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

    #region Motor Helpers
    private async Task HomeMotorsHelper()
    {
        string? msg;
        if (_motorPageService == null)
        {
            msg = "_motorPageService is null. Cannot home motors.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);
            return;
        }
        else
        {
            msg = "Homing all motors";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);
        }

        var buildMotor = _motorPageService.GetBuildMotor();
        var powderMotor = _motorPageService.GetPowderMotor();
        var sweepMotor = _motorPageService.GetSweepMotor();

        if ((buildMotor != null) && (!_motorPageService.GetSweepMotor().STOP_MOVE_FLAG))
        {
            await _motorPageService.HomeMotorAndUpdateTextBox(buildMotor);
        }
        else
        {
            MagnetoLogger.Log("Build Motor is null or stop flag is up cannot home motor.", LogFactoryLogLevel.LogLevel.ERROR);
        }

        if ((powderMotor != null) && (!_motorPageService.GetSweepMotor().STOP_MOVE_FLAG))
        {
            await _motorPageService.HomeMotorAndUpdateTextBox(powderMotor);
        }
        else
        {
            MagnetoLogger.Log("Powder Motor is null or stop flag is up cannot home motor.", LogFactoryLogLevel.LogLevel.ERROR);
        }

        if ((sweepMotor != null) && (!_motorPageService.GetSweepMotor().STOP_MOVE_FLAG))
        {
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
        _motorPageService.SelectBuildMotor();
    }
    private void SelectPowderMotorButton_Click(object sender, RoutedEventArgs e)
    {
        _motorPageService.SelectPowderMotor();
    }
    private void SelectSweepMotorButton_Click(object sender, RoutedEventArgs e)
    {
        _motorPageService.SelectSweepMotor();
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
    private void StepSweepMotorLeftButton_Click(object sender, RoutedEventArgs e)
    {
        var motor = _motorPageService.GetSweepMotor();
        _motorPageService.HandleRelMove(motor, _motorPageService.GetSweepStepTextBox(), true, this.Content.XamlRoot);
    }
    private void StepSweepMotorRightButton_Click(object sender, RoutedEventArgs e)
    {
        var motor = _motorPageService.GetSweepMotor();
        _motorPageService.HandleRelMove(motor, _motorPageService.GetSweepStepTextBox(), false, this.Content.XamlRoot);
    }
    private async void HomeAllMotorsButton_Click(object sender, RoutedEventArgs e)
    {
        await HomeMotorsHelper();
    }
    private void StopAllMotorsButton_Click(object sender, RoutedEventArgs e)
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

    #region Print Layer Move Methods
    private void StopButton_Click(object sender, RoutedEventArgs e)
    {
        //KillAll(); // TODO: TEST; does the same as below, but has not always worked in methods
        // stop mark
        _waverunnerPageService.StopMark(this.Content.XamlRoot);
        // stop motors
        _motorPageService.StopSweepMotorAndUpdateTextBox();
        _motorPageService.StopBuildMotorAndUpdateTextBox();
        _motorPageService.StopPowderMotorAndUpdateTextBox();
    }
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
    #region Movement Helpers
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
        _motorPageService.ChangeSelectButtonsBackground(Colors.Red);
        LockCalibrationPanel();
    }
    private void EnableMotorsButton_Click(object sender, RoutedEventArgs e)
    {
        var buildMotor = _motorPageService.GetBuildMotor();
        var powderMotor = _motorPageService.GetPowderMotor();
        var sweepMotor = _motorPageService.GetSweepMotor();
        buildMotor.STOP_MOVE_FLAG = false;
        powderMotor.STOP_MOVE_FLAG = false;
        sweepMotor.STOP_MOVE_FLAG = false;
        UnlockCalibrationPanel();
        _motorPageService.ChangeSelectButtonsBackground(Colors.DarkGray);
    }
    #endregion

    #region Locks
    private void UnlockCalibrationPanel()
    {
        _motorPageService.UnlockCalibrationPanel();
        EnableMotorsButton.Content = "Lock Calibration";
    }
    private void LockCalibrationPanel()
    {
        _motorPageService.LockCalibrationPanel();
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
    private async void PrintLayersButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        int res;
        double thickness;
        double power;
        double scanSpeed;
        double hatchSpacing;
        double amplifier;
        Int64 slicesToMark;
        var startWithMark = StartWithMarkCheckbox.IsEnabled;
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
        (res, power) = ConvertTextBoxTextToDouble(PowerTextBox);
        if (res == 0)
        {
            var msg = $"Layer thickness text box input is invalid.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
            return;
        }
        (res, scanSpeed) = ConvertTextBoxTextToDouble(ScanSpeedTextBox);
        if (res == 0)
        {
            var msg = $"Layer thickness text box input is invalid.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
            return;
        }
        (res, hatchSpacing) = ConvertTextBoxTextToDouble(HatchSpacingTextBox);
        if (res == 0)
        {
            var msg = $"Layer thickness text box input is invalid.";
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
        // Print requested layers
        for (var i = 0; i < slicesToMark; i++)
        {
            await ViewModel.PrintLayer(startWithMark, thickness, power, scanSpeed, hatchSpacing, amplifier);
            PopulatePageText();
        }
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
        _waverunnerPageService.TestWaverunnerConnection(this.XamlRoot);
    }
    
    private void EnableBuildMotorButton_Click(object sender, RoutedEventArgs e)
    {
    }
    private void EnablePowderMotorButton_Click(object sender, RoutedEventArgs e)
    {
    }
    private void EnableSweepMotorButton_Click(object sender, RoutedEventArgs e)
    {
    }
}
