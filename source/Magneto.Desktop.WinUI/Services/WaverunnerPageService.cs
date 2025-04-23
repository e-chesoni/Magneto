using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using SAMLIGHT_CLIENT_CTRL_EXLib;
using Magneto.Desktop.WinUI.Helpers;
using ABI.System;
using static System.Net.Mime.MediaTypeNames;
using Microsoft.UI;
using Magneto.Desktop.WinUI.Popups;
using Magneto.Desktop.WinUI.Core;
using Magneto.Desktop.WinUI.Core.Contracts.Services;
using Magneto.Desktop.WinUI.Contracts.Services;
using Microsoft.VisualBasic;

namespace Magneto.Desktop.WinUI.Services;
public class WaverunnerPageService
{
    private readonly IWaverunnerService _waverunnerService;
    private readonly IFileService _fileService;
    /// <summary>
    /// WaveRunner client control interface
    /// </summary>
    private static readonly ScSamlightClientCtrlEx cci = new(); // TODO: Remove once service is set up
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

    #region UI Variables
    public TextBox JobDirectoryTextBox { get; set; }
    public TextBox JobFileNameTextBox { get; set; }
    public Button ToggleRedPointerButton { get; set; }
    public Button StartMarkButton { get; set; }
    public TextBlock? IsMarkingText { get; set; }
    #endregion

    public WaverunnerPageService(TextBox jobFileSearchDirectory, TextBox jobFileNameTextBox,
                                 Button toggleRedPointerButton, Button startMarkButton, TextBlock isMarkingText)
    {
        // Load services
        _waverunnerService = App.GetService<IWaverunnerService>();
        _fileService = App.GetService<IFileService>();

        // Assign UI Elements
        this.JobDirectoryTextBox = jobFileSearchDirectory;
        this.JobFileNameTextBox = jobFileNameTextBox;
        this.ToggleRedPointerButton = toggleRedPointerButton;
        this.StartMarkButton = startMarkButton;
        this.IsMarkingText = isMarkingText;

        // Set default job directory
        _defaultJobDirectory = @"C:\Scanner Application\Scanner Software\jobfiles";  // "@" symbol means treat "\" as "\" (not a space)
        _jobDirectory = _defaultJobDirectory;
        this.JobDirectoryTextBox.Text = _jobDirectory;

        // Set default job file
        _defaultJobName = "steel-3D-test-11-22-24.sjf";
        this.JobFileNameTextBox.Text = _defaultJobName;

        // ASSUMPTION: Red pointer is off when application starts
        // Have not found way to check red pointer status in SAMLight docs 
        // Initialize red pointer to off
        _redPointerEnabled = false;
    }

    public WaverunnerPageService(TextBox printDirectoryTextBox, Button startMarkButton)
    {
        // Load services
        _waverunnerService = App.GetService<IWaverunnerService>();
        _fileService = App.GetService<IFileService>();
        
        // Assign UI elements
        this.StartMarkButton = startMarkButton;
        _defaultJobDirectory = @"C:\Scanner Application\Scanner Software\jobfiles";
        _jobDirectory = _defaultJobDirectory;
        // ASSUMPTION: Red pointer is off when application starts
        // Have not found way to check red pointer status in SAMLight docs 
        // Initialize red pointer to off
        _redPointerEnabled = false;
    }

    public WaverunnerPageService()
    {
    
    }

    #region Connectivity Test Methods

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
    /*
    #region Helper Functions
    private ExecStatus FindJobDirectory()
    {
        // Log the target directory
        var msg = $"Target Directory: {_jobDirectory}";
        MagnetoLogger.Log(msg, Core.Contracts.Services.LogFactoryLogLevel.LogLevel.VERBOSE);

        // Check if the directory exists
        if (!Directory.Exists(_jobDirectory))
        {
            msg = "Directory does not exist. Cannot get job.";
            MagnetoLogger.Log(msg, Core.Contracts.Services.LogFactoryLogLevel.LogLevel.ERROR);
            return ExecStatus.Failure;
        }
        else
        {
            return ExecStatus.Success;
        }
    }
    private ExecStatus FindFile(string fileName, XamlRoot xamlRoot)
    {
        // Check if the file exists
        if (!File.Exists(fileName))
        {
            // TODO: Use Log & Display once it's extrapolated from TestPrintPage.xaml.cs
            var msg = $"Could not find: {fileName}";
            MagnetoLogger.Log(msg, Core.Contracts.Services.LogFactoryLogLevel.LogLevel.ERROR);
            _ = PopupInfo.ShowContentDialog(xamlRoot, "Warning", msg);
            return ExecStatus.Failure;
        }
        else
        {
            var msg = $"Found file: {fileName}";
            MagnetoLogger.Log(msg, Core.Contracts.Services.LogFactoryLogLevel.LogLevel.SUCCESS);
            return ExecStatus.Success;
        }
    }
    #endregion
    */
    #region File Path Methods

    // TODO: Implement file selector in view model instead  of all this...
    /*

    public ExecStatus UpdateDirectory()
    {
        _jobDirectory = JobDirectoryTextBox.Text;
        StartMarkButton.IsEnabled = false;
        return ExecStatus.Success;
    }
    public ExecStatus ValidateJobPath(XamlRoot xamlRoot)
    {
        MagnetoLogger.Log("Getting job...", Core.Contracts.Services.LogFactoryLogLevel.LogLevel.VERBOSE);
        var fullPathToJob = Path.Combine(_jobDirectory, JobFileNameTextBox.Text);

        // TODO: Use log & display for error messaging in future
        if (!Directory.Exists(_jobDirectory))
        {
            var msg = "Directory does not exist. Cannot get job.";
            MagnetoLogger.Log(msg, Core.Contracts.Services.LogFactoryLogLevel.LogLevel.ERROR);
            _ = PopupInfo.ShowContentDialog(xamlRoot, "Warning", msg);
            return ExecStatus.Failure;
        }

        if (!File.Exists(fullPathToJob))
        {
            var msg = $"File not found: {fullPathToJob}";
            MagnetoLogger.Log(msg, Core.Contracts.Services.LogFactoryLogLevel.LogLevel.SUCCESS);
            _ = PopupInfo.ShowContentDialog(xamlRoot, "Warning", msg);
            return ExecStatus.Failure;
        }

        return ExecStatus.Success;
    }
    public ExecStatus SetMarkJobInTestConfig(XamlRoot xamlRoot)
    {
        var fullFilePath = Path.Combine(_jobDirectory, JobFileNameTextBox.Text);

        if (_fileService.ValidateFilePath(fullFilePath) == 1)
        {
            var msg = $"Valid job path";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.SUCCESS);
            // Enable toggle pointer and start mark buttons
            _fullJobFilePath = fullFilePath;
            // TODO: Add check to make sure wave runner is open & running ("say hi check")
            // If it's not open, do not enable these buttons! Instead, display error pop up & log error
            StartMarkButton.IsEnabled = true;
            ToggleRedPointerButton.IsEnabled = true;
            return ExecStatus.Success;
        }
        else
        {
            var msg = $"File not found: {fullFilePath}";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
            _ = PopupInfo.ShowContentDialog(xamlRoot, "Warning", msg);
            // Make sure buttons are disabled; we can't mark what we can't find!
            StartMarkButton.IsEnabled = false;
            ToggleRedPointerButton.IsEnabled = false;
            return ExecStatus.Failure;
        }
    }
    */

    // TODO: Remove. Update Test Waverunner page accordingly
    /*
    public ExecStatus UseDefaultJob()
    {
        // Update job file name
        JobFileNameTextBox.Text = _defaultJobName;

        var msg = $"Setting job file to default job {_defaultJobName}";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);

        // Check if the directory exists
        if (_fileService.FindDirectory(_jobDirectory) == 0) // Returns -1 on fail; 0 on success
        {
            msg = $"Directory {_jobDirectory} does not exist. Cannot get job.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
            return ExecStatus.Failure;
        }
        else { return ExecStatus.Success; }
    }
    */
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

    public ExecStatus StartRedPointer(XamlRoot xamlRoot, string filePath)
    {
        string? msg;
        if (_waverunnerService.IsRunning() == 0)
        {
            msg = $"Could not mark file. Waverunner is not running.";
            _ = PopupInfo.ShowContentDialog(xamlRoot, "Warning", msg);
            StartMarkButton.IsEnabled = false;
            return ExecStatus.Failure;
        }

        _waverunnerService.StartRedPointer(filePath);

        // TODO: Replace once we figure out how to interact with error codes form SAM
        return ExecStatus.Success;
    }

    public int ToggleRedPointer(XamlRoot xamlroot, string filePath)
    {
        _redPointerEnabled = !_redPointerEnabled;

        if (_redPointerEnabled)
        {
            MagnetoLogger.Log("Starting Red Pointer", LogFactoryLogLevel.LogLevel.SUCCESS);
            StartRedPointer(xamlroot, filePath);
            ToggleRedPointerButton.Background = new SolidColorBrush(Colors.Red);
            StartMarkButton.IsEnabled = false; // Disable start mark button (can't be enabled at same time as red pointer -- TODO: follow up with docs/tests to validate)
            return (int)ExecStatus.Success;
        }
        else
        {
            MagnetoLogger.Log("Stopping Red Pointer", LogFactoryLogLevel.LogLevel.SUCCESS);
            StopRedPointer();
            ToggleRedPointerButton.Background = (SolidColorBrush)Microsoft.UI.Xaml.Application.Current.Resources["ButtonBackgroundThemeBrush"];
            // Re-enable StartMarkButton only if _fullJobFilePath is still valid
            StartMarkButton.IsEnabled = !string.IsNullOrEmpty(_fullJobFilePath) && File.Exists(_fullJobFilePath);
            return (int)ExecStatus.Success;
        }
    }

    public ExecStatus StopRedPointer()
    {
        _waverunnerService.StopRedPointer();
        // TODO: Replace once we figure out how to interact with error codes form SAM
        return ExecStatus.Success;
    }

    public async Task<ExecStatus> MarkEntityAsync(XamlRoot xamlRoot, string filePath)
    {
        string? msg;
        if (_waverunnerService.IsRunning() == 0)
        {
            msg = $"Could not mark file. Waverunner is not running.";
            _ = PopupInfo.ShowContentDialog(xamlRoot, "Warning", msg);
            StartMarkButton.IsEnabled = false;
            return ExecStatus.Failure;
        }
        await _waverunnerService.MarkEntityAsync(filePath);
        return ExecStatus.Success;
    }

    public ExecStatus StopMark(XamlRoot xamlRoot)
    {
        string? msg;
        if (_waverunnerService.IsRunning() == 0)
        {
            msg = "Could not stop mark. Waverunner is not running.";
            _ = PopupInfo.ShowContentDialog(xamlRoot, "Warning", msg);
            return ExecStatus.Failure;
        }
        _waverunnerService.StopMark();
        return ExecStatus.Success;
    }
    #endregion

    #region Logging Methods
    // TODO: integrate with log & display extrapolation from TestPrintPage.xaml.cs
    public void UpdateUIMarkStatusAndLogMessage(string uiMessage, LogFactoryLogLevel.LogLevel logLevel, string logMessage = null)
    {
        // Update UI with the message
        //UpdateUITextHelper.UpdateUIText(IsMarkingText, uiMessage);

        // Use the provided log level for logging
        MagnetoLogger.Log(logMessage ?? uiMessage, logLevel);
    }
    #endregion
}
