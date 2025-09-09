using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;
using Magneto.Desktop.WinUI.Popups;
using Magneto.Desktop.WinUI.Core;
using Magneto.Desktop.WinUI.Core.Contracts.Services;
using Magneto.Desktop.WinUI.Contracts.Services;
using Magneto.Desktop.WinUI.Models.UIControl;

namespace Magneto.Desktop.WinUI.Services;
public class WaverunnerPageService
{
    private readonly IWaverunnerService _waverunnerService;
    private readonly IFileService _fileService;

    private bool _redPointerEnabled { get; set; }

    #region UI Variables
    public Button? ToggleRedPointerButton { get; set; }
    public Button StartMarkButton { get; set; }
    public TextBlock? IsMarkingText { get; set; }
    private UIControlGroupWrapper _uiControlGroupWrapper { get; set; }

    public UIControlGroupWaverunner? _waverunnerUiControlGroup { get; set; }
    #endregion

    #region Status Enumerators
    /// <summary>
    /// Waverunner Execution statuses
    /// </summary>
    public enum ExecStatus
    {
        Success = 0,
        Failure = -1,
    }
    #endregion

    public WaverunnerPageService(Button toggleRedPointerButton, Button startMarkButton, TextBlock isMarkingText)
    {
        // Load services
        _waverunnerService = App.GetService<IWaverunnerService>();
        _fileService = App.GetService<IFileService>();

        // Assign UI Elements
        this.ToggleRedPointerButton = toggleRedPointerButton;
        this.StartMarkButton = startMarkButton;
        this.IsMarkingText = isMarkingText;

        // ASSUMPTION: Red pointer is off when application starts
        // Have not found way to check red pointer status in SAMLight docs 
        // Initialize red pointer to off
        _redPointerEnabled = false;
    }

    public WaverunnerPageService(UIControlGroupWrapper uiControlGroupWrapper)
    {
        _waverunnerService = App.GetService<IWaverunnerService>();
        _fileService = App.GetService<IFileService>();
        _uiControlGroupWrapper = uiControlGroupWrapper;
        _waverunnerUiControlGroup = uiControlGroupWrapper.waverunnerControlGroup
                                     ?? throw new ArgumentNullException(nameof(uiControlGroupWrapper.waverunnerControlGroup), "Print Control Group must not be null.");

        this.StartMarkButton = _waverunnerUiControlGroup.playButton;
    }

    #region Connectivity Test Methods
    public bool WaverunnerRunning() => _waverunnerService.IsRunning();
    /// <summary>
    /// Test magneto connection to waverunner
    /// </summary>
    /// <returns>return 0 if successful; -1 if failed</returns>
    public ExecStatus TestWaverunnerConnection(XamlRoot xamlRoot)
    {
        if (_waverunnerService.TestConnection() == 1)
        {
            return ExecStatus.Success;
        }
        else
        {
            var displayMsg = "Unable to say hello to waverunner. Is the application open?";
            _ = PopupInfo.ShowContentDialog(xamlRoot, "Warning", displayMsg);
            return ExecStatus.Failure;
        }
    }
    #endregion

    #region Lock and Unlock Button Methods
    public void UnlockLayerMoveButtons() => _uiControlGroupWrapper.EnableLayerMoveButtons();
    public void LockLayerMoveButtons() => _uiControlGroupWrapper.DisableLayerMoveButtons();
    public void UnlockLayerMoveSettings() => _uiControlGroupWrapper.EnableWaverunnerSettingsControls();
    public void LockLayerMoveSettings() => _uiControlGroupWrapper.DisableWaverunnerSettingsControls();
    public void UnlockMarkOnlyControls() => _uiControlGroupWrapper.EnableMarkOnlyControls();
    public void LockMarkOnlyControls() => _uiControlGroupWrapper.DisableMarkOnlyControls();
    public void UnlockMarkButton() => _uiControlGroupWrapper.EnableMarkButton();
    public void LockMarkButton() => _uiControlGroupWrapper.DisableMarkButton();
    public void UnlockMarkOnlyCheckBox() => _uiControlGroupWrapper.EnableMarkOnlyCheckBox();
    public void LockMarkOnlyCheckBox() => _uiControlGroupWrapper.DisableMarkOnlyCheckBox();
    #endregion

    #region Pen Methods
    public double GetDefaultLaserPower() => _waverunnerService.GetDefaultLaserPower();
    public double GetDefaultMarkSpeed() => _waverunnerService.GetDefaultMarkSpeed();
    public double GetDefaultHatchSpacing() => _waverunnerService.GetDefaultHatchSpacing();
    public double GetDefaultSupplyAmplifier() => _waverunnerService.GetDefaultSupplyAmplifier();
    public double GetEnergyDensity(double thickness, double power, double scanSpeed, double hatchSpacing) => _waverunnerService.CalculateEnergyDensity(thickness, power, scanSpeed, hatchSpacing);
    public double GetMarkSpeed() => _waverunnerService.GetMarkSpeed();
    public double GetLaserPower() => _waverunnerService.GetLaserPower();
    #endregion

    #region Marking Methods
    public (int res, double martTime) GetLastMark(XamlRoot xamlRoot)
    {
        int res;
        double markTime;
        string? msg;
        (res, markTime) = _waverunnerService.GetLastMark();
        if (res == 0)
        {
            msg = $"Could not get last mark time";
            _ = PopupInfo.ShowContentDialog(xamlRoot, "Warning", msg);
        }
        return (res, markTime);
    }

    private ExecStatus StartRedPointer(XamlRoot xamlRoot, string filePath)
    {
        if (_waverunnerService.IsRunning())
        {
            StartMarkButton.IsEnabled = false;
            return ExecStatus.Failure;
        }

        _waverunnerService.StartRedPointer(filePath);

        // TODO: Replace once we figure out how to interact with error codes form SAM
        return ExecStatus.Success;
    }
    private ExecStatus StopRedPointer()
    {
        _waverunnerService.StopRedPointer();
        // TODO: Replace once we figure out how to interact with error codes form SAM
        return ExecStatus.Success;
    }
    public int ToggleRedPointer(XamlRoot xamlRoot, string filePath)
    {
        string? msg;
        if (_waverunnerService.IsRunning())
        {
            msg = $"Cannot toggle red pointer. Waverunner is not running.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
            _ = PopupInfo.ShowContentDialog(xamlRoot, "Error", msg);
            return 0;
        }
        if (ToggleRedPointerButton == null)
        {
            msg = $"Waverunner page service could not find toggle red pointer button.";
            MagnetoLogger.Log("ToggleRedPointerButton is null", LogFactoryLogLevel.LogLevel.ERROR);
            _ = PopupInfo.ShowContentDialog(xamlRoot, "Error", msg);
            return 0;
        }
        _redPointerEnabled = !_redPointerEnabled;
        if (_redPointerEnabled)
        {
            MagnetoLogger.Log("Starting Red Pointer", LogFactoryLogLevel.LogLevel.SUCCESS);
            StartRedPointer(xamlRoot, filePath);
            ToggleRedPointerButton.Background = new SolidColorBrush(Colors.Red);
            StartMarkButton.IsEnabled = false; // Disable start mark button (can't be enabled at same time as red pointer -- TODO: follow up with docs/tests to validate)
            if (IsMarkingText != null)
            {
                IsMarkingText.Text = "Red pointer on.";
            }
            return (int)ExecStatus.Success;
        }
        else
        {
            MagnetoLogger.Log("Stopping Red Pointer", LogFactoryLogLevel.LogLevel.SUCCESS);
            StopRedPointer();
            ToggleRedPointerButton.Background = (SolidColorBrush)Microsoft.UI.Xaml.Application.Current.Resources["ButtonBackgroundThemeBrush"];
            // TODO: Test if filePath is still valid
            StartMarkButton.IsEnabled = !string.IsNullOrEmpty(filePath) && File.Exists(filePath);
            if (IsMarkingText != null)
            {
                IsMarkingText.Text = "Red pointer off.";
            }
            return (int)ExecStatus.Success;
        }
    }

    public async Task<ExecStatus> MarkEntityAsync(XamlRoot xamlRoot, string filePath)
    {
        string? msg;
        if (_waverunnerService.IsRunning())
        {
            msg = $"Cannot mark file. Waverunner is not running.";
            _ = PopupInfo.ShowContentDialog(xamlRoot, "Error", msg); //TODO: TEST. Think this will work; "Error" may need to be "Warning"
            return ExecStatus.Failure;
        }
        if (_fileService.ValidateFilePath(filePath) == 0) 
        {
            msg = $"Could not mark file. Invalid file path.";
            _ = PopupInfo.ShowContentDialog(xamlRoot, "Error", msg);
        }
        // update text
        if (IsMarkingText != null)
        {
            IsMarkingText.Text = "Marking!";
        }
        // then start marking
        await _waverunnerService.MarkEntityAsync(filePath);
        return ExecStatus.Success;
    }

    public ExecStatus StopMark(XamlRoot xamlRoot)
    {
        string? msg;
        if (_waverunnerService.IsRunning())
        {
            msg = "Could not stop mark. Waverunner is not running.";
            _ = PopupInfo.ShowContentDialog(xamlRoot, "Error", msg);
            return ExecStatus.Failure;
        }
        // stop mark immediately
        _waverunnerService.StopMark();
        // then update text
        if (IsMarkingText != null)
        {
            IsMarkingText.Text = "Not marking.";
        }
        return ExecStatus.Success;
    }
    #endregion

    #region Slice Methods
    public bool WaverunnerIn3DMode()
    {
        return _waverunnerService.InSAM3DMode();
    }
    // TODO: Call me, beep me, if you wanna test me
    public async Task SliceSTL(XamlRoot xamlRoot, string stlPath, double sliceThickness, string inputEntityName = "ImportedEntity")
    {
        
        _waverunnerService.ImportStlFile(inputEntityName, stlPath);

        var slicedEntityName = _waverunnerService.GenerateSlicedEntity(inputEntityName, sliceThickness);
        if (slicedEntityName == null) return;

        // TODO: move later ? maybe belongs here b/c of entity
        _waverunnerService.SetHatchDistance(slicedEntityName, 0.12); // TODO: remove magic number
        _waverunnerService.EnableAndSetHatchStyle(slicedEntityName);

        var yes = await PopupInfo.ShowYesNoDialog(
            xamlRoot,
            "Export slices?",
            "Would you like to save the sliced layers to a directory?"
        );

        if (!yes) return;

        // create a sub-folder for slices
        var stlDir = Path.GetDirectoryName(stlPath) ?? "";
        var stlBase = Path.GetFileNameWithoutExtension(stlPath) ?? "slices";
        var outputDir = Path.Combine(stlDir, $"{stlBase}_slices");

        Directory.CreateDirectory(outputDir); // safe no-op if it exists

        _waverunnerService.ExportAndSaveSlices(slicedEntityName, outputDir);

        await PopupInfo.ShowYesNoDialog(
            xamlRoot,
            "Export Directory",
            $"Slices have been exported to: {outputDir}"
        );
    }
    #endregion
}
