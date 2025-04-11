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
using Magneto.Desktop.WinUI.Core.Services;
using Magneto.Desktop.WinUI.Helpers;
using ABI.System;
using static System.Net.Mime.MediaTypeNames;
using Microsoft.UI;
using Magneto.Desktop.WinUI.Popups;

namespace Magneto.Desktop.WinUI.Services;
public class WaverunnerPageService
{
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

    #region Enumerators

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

    public WaverunnerPageService(TextBox jobFileSearchDirectory, TextBox jobFileNameTextBox,
                                 Button toggleRedPointerButton, Button startMarkButton)
    {
        this.JobDirectoryTextBox = jobFileSearchDirectory;
        this.JobFileNameTextBox = jobFileNameTextBox;
        this.ToggleRedPointerButton = toggleRedPointerButton;
        this.StartMarkButton = startMarkButton;
        this.IsMarkingText = null;

        // Set default job directory
        _defaultJobDirectory = @"C:\Scanner Application\Scanner Software\jobfiles";
        _jobDirectory = _defaultJobDirectory;
        this.JobDirectoryTextBox.Text = _jobDirectory;

        // Set default job file
        _defaultJobName = "center_crosshair_OAT.sjf";
        this.JobFileNameTextBox.Text = _defaultJobName;

        // ASSUMPTION: Red pointer is off when application starts
        // Have not found way to check red pointer status in SAMLight docs 
        // Initialize red pointer to off
        _redPointerEnabled = false;
    }

    public WaverunnerPageService(TextBox printDirectoryTextBox, Button startMarkButton)
    {
        //this.JobFileSearchDirectory = jobFileSearchDirectory;
        //this.JobFileNameTextBox = jobFileNameTextBox;
        //this.ToggleRedPointerButton = toggleRedPointerButton;
        this.StartMarkButton = startMarkButton;
        //this.IsMarkingText = null;

        // Set default job directory
        _defaultJobDirectory = @"C:\Scanner Application\Scanner Software\jobfiles";
        _jobDirectory = _defaultJobDirectory;
        //this.JobDirectory.Text = _jobDirectory;

        // Set default job file
        //_defaultJobName = "center_crosshair_OAT.sjf";
        //this.JobFileNameTextBox.Text = _defaultJobName;

        // ASSUMPTION: Red pointer is off when application starts
        // Have not found way to check red pointer status in SAMLight docs 
        // Initialize red pointer to off
        _redPointerEnabled = false;
    }

    #region Setters
    public void SetDirectory(string directory)
    {
        //JobDirectoryTextBox = directory;
    }
    public void SetDefaultJobFileName(string defaultFileNameJob)
    {
        _defaultJobName = defaultFileNameJob;
        JobFileNameTextBox.Text = defaultFileNameJob;
    }
    #endregion

    #region Connectivity Test Methods

    /// <summary>
    /// Test magneto connection to waverunner
    /// </summary>
    /// <returns>return 0 if successful; -1 if failed</returns>
    public ExecStatus TestWaverunnerConnection(XamlRoot xamlRoot)
    {
        try
        {
            // Show hello world message box in SAMlight
            cci.ScExecCommand((int)ScComSAMLightClientCtrlExecCommandConstants.scComSAMLightClientCtrlExecCommandTest);
            return ExecStatus.Success;
        }
        catch (System.Exception exception)
        {
            // TODO: Use Log & Display once it's extrapolated from TestPrintPage.xaml.cs
            var logMsg = $"CCI Error! \n {Convert.ToString(exception)}";
            var displayMsg = "Unable to say hello to waverunner. Is the application open?";
            MagnetoLogger.Log(logMsg, Core.Contracts.Services.LogFactoryLogLevel.LogLevel.ERROR);
            _ = PopupInfo.ShowContentDialog(xamlRoot, "Warning", displayMsg);
            return ExecStatus.Failure;
        }
    }

    #endregion

    #region Helper Functions

    private ExecStatus PrintDirectoryFiles(string targetDirectory)
    {
        var msg = "";

        if (!Directory.Exists(targetDirectory))
        {
            msg = "Directory does not exist. Cannot print files.";
            MagnetoLogger.Log(msg, Core.Contracts.Services.LogFactoryLogLevel.LogLevel.ERROR);
            return ExecStatus.Failure;
        }

        // Get all file names in the directory
        var fileEntries = Directory.GetFiles(targetDirectory);
        foreach (var fileName in fileEntries)
        {
            msg = $"File: {fileName}";
            MagnetoLogger.Log(msg, Core.Contracts.Services.LogFactoryLogLevel.LogLevel.SUCCESS);
        }

        return ExecStatus.Success;
    }

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

    #region File Path Methods

    // TODO: put in a try/catch block in case update fails
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

        if (ValidateJobPath(xamlRoot) == ExecStatus.Success)
        {
            var msg = $"Valid job path";
            MagnetoLogger.Log(msg, Core.Contracts.Services.LogFactoryLogLevel.LogLevel.SUCCESS);
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
            MagnetoLogger.Log(msg, Core.Contracts.Services.LogFactoryLogLevel.LogLevel.ERROR);
            _ = PopupInfo.ShowContentDialog(xamlRoot, "Warning", msg);
            // Make sure buttons are disabled; we can't mark what we can't find!
            StartMarkButton.IsEnabled = false;
            ToggleRedPointerButton.IsEnabled = false;
            return ExecStatus.Failure;
        }
    }

    public string SetMarkJob(XamlRoot xamlRoot, string jobFileName)
    {
        var fullFilePath = Path.Combine(_jobDirectory, jobFileName);

        if (ValidateJobPath(xamlRoot) == ExecStatus.Success)
        {
            var msg = $"Valid job path";
            MagnetoLogger.Log(msg, Core.Contracts.Services.LogFactoryLogLevel.LogLevel.SUCCESS);
            // Enable toggle pointer and start mark buttons
            _fullJobFilePath = fullFilePath;
            // TODO: Add check to make sure wave runner is open & running ("say hi check")
            // If it's not open, do not enable these buttons! Instead, display error pop up & log error
            StartMarkButton.IsEnabled = true;
            return fullFilePath;
        }
        else
        {
            var msg = $"File not found: {fullFilePath}";
            MagnetoLogger.Log(msg, Core.Contracts.Services.LogFactoryLogLevel.LogLevel.ERROR);
            _ = PopupInfo.ShowContentDialog(xamlRoot, "Warning", msg);
            // Make sure buttons are disabled; we can't mark what we can't find!
            StartMarkButton.IsEnabled = false;
            return "";
        }
    }

    public ExecStatus UseDefaultJob()
    {
        // Update job file name
        JobFileNameTextBox.Text = _defaultJobName;

        var msg = $"Setting job file to default job {_defaultJobName}";
        MagnetoLogger.Log(msg, Core.Contracts.Services.LogFactoryLogLevel.LogLevel.VERBOSE);

        // Check if the directory exists
        if (FindJobDirectory() == ExecStatus.Failure) // Returns -1 on fail; 0 on success
        {
            msg = $"Directory {_jobDirectory} does not exist. Cannot get job.";
            MagnetoLogger.Log(msg, Core.Contracts.Services.LogFactoryLogLevel.LogLevel.ERROR);
            return ExecStatus.Failure;
        }
        else { return ExecStatus.Success; }
    }

    #endregion

    #region Marking Methods

    public double GetLastMark()
    {
        var msg = "Get last mark requested...";
        MagnetoLogger.Log(msg, Core.Contracts.Services.LogFactoryLogLevel.LogLevel.VERBOSE);

        var mark_time = 0.0; // placeholder for mark time

        if (cci.ScIsRunning() == 0)
        {
            msg = "SAMLight not found";
            MagnetoLogger.Log(msg, Core.Contracts.Services.LogFactoryLogLevel.LogLevel.ERROR);
            return -2.0;
        }

        try
        {
            mark_time = cci.ScGetDoubleValue((int)ScComSAMLightClientCtrlValueTypes.scComSAMLightClientCtrlDoubleValueTypeLastMarkTime);
            var mark_time_string = string.Concat("Last mark time was: ", mark_time, " seconds");
            MagnetoLogger.Log(mark_time_string, Core.Contracts.Services.LogFactoryLogLevel.LogLevel.VERBOSE);
            return mark_time;
        }
        catch (System.Exception exception)
        {
            msg = $"Unable to get mark time \n {Convert.ToString(exception)}";
            MagnetoLogger.Log(msg, Core.Contracts.Services.LogFactoryLogLevel.LogLevel.ERROR);
            return -1.0;
        }

        //MessageBox.Show(mark_time_string, "Info", MessageBoxButtons.OK);
    }

    public static ExecStatus SetRedPointerMode(RedPointerMode mode)
    {
        // Returns void
        cci.ScSetLongValue((int)ScComSAMLightClientCtrlValueTypes.scComSAMLightClientCtrlLongValueTypeRedpointerMode, (int)mode);

        // TODO: Replace once we figure out how to interact with error codes form SAM
        return ExecStatus.Success;
    }

    public ExecStatus StartRedPointer()
    {
        UpdateUIMarkStatusAndLogMessage("Starting red pointer", Core.Contracts.Services.LogFactoryLogLevel.LogLevel.SUCCESS);

        if (cci.ScIsRunning() == 0)
        {
            UpdateUIMarkStatusAndLogMessage("Cannot Mark; WaveRunner is closed.", Core.Contracts.Services.LogFactoryLogLevel.LogLevel.ERROR, "SAMLight not found");
            StartMarkButton.IsEnabled = false;
            return ExecStatus.Failure;
        }

        UpdateUIMarkStatusAndLogMessage("Sending Objects!", Core.Contracts.Services.LogFactoryLogLevel.LogLevel.SUCCESS); // Update UI with status

        // load demo job file
        cci.ScLoadJob(_fullJobFilePath, 1, 1, 0);

        // returns void
        cci.ScExecCommand((int)ScComSAMLightClientCtrlExecCommandConstants.scComSAMLightClientCtrlExecCommandRedPointerStart);

        // TODO: Replace once we figure out how to interact with error codes form SAM
        return ExecStatus.Success;
    }

    public int ToggleRedPointer()
    {
        _redPointerEnabled = !_redPointerEnabled;

        if (_redPointerEnabled)
        {
            MagnetoLogger.Log("Starting Red Pointer", Core.Contracts.Services.LogFactoryLogLevel.LogLevel.SUCCESS);
            StartRedPointer();
            ToggleRedPointerButton.Background = new SolidColorBrush(Colors.Red);
            StartMarkButton.IsEnabled = false; // Disable start mark button (can't be enabled at same time as red pointer -- TODO: follow up with docs/tests to validate)
            return (int)ExecStatus.Success;
        }
        else
        {
            MagnetoLogger.Log("Stopping Red Pointer", Core.Contracts.Services.LogFactoryLogLevel.LogLevel.SUCCESS);
            StopRedPointer();
            ToggleRedPointerButton.Background = (SolidColorBrush)Microsoft.UI.Xaml.Application.Current.Resources["ButtonBackgroundThemeBrush"];
            // Re-enable StartMarkButton only if _fullJobFilePath is still valid
            StartMarkButton.IsEnabled = !string.IsNullOrEmpty(_fullJobFilePath) && File.Exists(_fullJobFilePath);
            return (int)ExecStatus.Success;
        }
    }

    public ExecStatus StopRedPointer()
    {
        UpdateUIMarkStatusAndLogMessage("Stopping red pointer", Core.Contracts.Services.LogFactoryLogLevel.LogLevel.SUCCESS);

        // returns void
        cci.ScExecCommand((int)ScComSAMLightClientCtrlExecCommandConstants.scComSAMLightClientCtrlExecCommandRedPointerStop);

        // make sure laser does not mark when stopping red pointer
        cci.ScStopMarking();

        // TODO: Replace once we figure out how to interact with error codes form SAM
        return ExecStatus.Success;
    }

    public async Task<ExecStatus> MarkEntityAsync()
    {
        // File exists, proceed with marking
        var msg = $"Starting mark for file: {_fullJobFilePath}";
        MagnetoLogger.Log(msg, Core.Contracts.Services.LogFactoryLogLevel.LogLevel.VERBOSE);

        if (cci.ScIsRunning() == 0)
        {
            UpdateUIMarkStatusAndLogMessage("Cannot Mark; WaveRunner is closed.", Core.Contracts.Services.LogFactoryLogLevel.LogLevel.ERROR, "SAMLight not found");
            StartMarkButton.IsEnabled = false;
            return ExecStatus.Failure;
        }

        UpdateUIMarkStatusAndLogMessage("Sending Objects!", Core.Contracts.Services.LogFactoryLogLevel.LogLevel.SUCCESS); // Update UI with status

        // load demo job file
        cci.ScLoadJob(_fullJobFilePath, 1, 1, 0);

        msg = $"Loaded file at path: {_fullJobFilePath} for marking...";

        MagnetoLogger.Log(msg, Core.Contracts.Services.LogFactoryLogLevel.LogLevel.WARN);

        try
        {
            cci.ScMarkEntityByName("", 0); // 0 returns control to the user immediately; if you use 1, this becomes a blocking function
            UpdateUIMarkStatusAndLogMessage("Marking!", Core.Contracts.Services.LogFactoryLogLevel.LogLevel.WARN, "SAMLight is Marking...");

            // Wait for marking to complete
            while (cci.ScIsMarking() != 0)
            {
                await Task.Delay(100); // Use a delay to throttle the loop for checking marking status
            }

            cci.ScStopMarking();
            UpdateUIMarkStatusAndLogMessage("Done Marking", Core.Contracts.Services.LogFactoryLogLevel.LogLevel.SUCCESS, "SAMLight is done marking.");
            StartMarkButton.IsEnabled = true; // Allow retrying

            return ExecStatus.Success;
        }
        catch (System.Runtime.InteropServices.COMException comEx)
        {
            UpdateUIMarkStatusAndLogMessage($"COM Exception: {comEx.Message}", Core.Contracts.Services.LogFactoryLogLevel.LogLevel.ERROR);
            StartMarkButton.IsEnabled = true; // Allow retrying
            return ExecStatus.Failure;
        }
    }

    /// <summary>
    /// If the ScMarkEntityByName function was called with WaitForMarkEnd set to 0, this function can be used for checking whether the actual marking process is already finished or not. 
    /// </summary>
    /// <returns> The Function returns 1 if the scanner application is still marking. </returns>
    public int GetMarkStatus()
    {
        return cci.ScIsMarking();
    }

    public void MarkAndWait()
    {
        while (GetMarkStatus() != 0)
        {
            // wait
            Task.Delay(100).Wait();
        }

        _ = StopMark();
    }

    public ExecStatus StopMark()
    {
        var msg = "";

        if (cci.ScIsRunning() == 0)
        {
            msg = "SAMLight not found";
            MagnetoLogger.Log(msg, Core.Contracts.Services.LogFactoryLogLevel.LogLevel.ERROR);
            return ExecStatus.Failure;
        }

        UpdateUIMarkStatusAndLogMessage("Stopping Mark", Core.Contracts.Services.LogFactoryLogLevel.LogLevel.SUCCESS);

        msg = "SAMLight is stopping mark";
        MagnetoLogger.Log(msg, Core.Contracts.Services.LogFactoryLogLevel.LogLevel.WARN);

        cci.ScStopMarking();

        return ExecStatus.Success;
    }

    #endregion

    #region Logging Methods

    // TODO: integrate with log & display extrapolation from TestPrintPage.xaml.cs
    public void UpdateUIMarkStatusAndLogMessage(string uiMessage, Core.Contracts.Services.LogFactoryLogLevel.LogLevel logLevel, string logMessage = null)
    {
        // Update UI with the message
        //UpdateUITextHelper.UpdateUIText(IsMarkingText, uiMessage);

        // Use the provided log level for logging
        MagnetoLogger.Log(logMessage ?? uiMessage, logLevel);
    }

    #endregion
}
