// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

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
using Magneto.Desktop.WinUI.Services;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Magneto.Desktop.WinUI;
/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class TestWaveRunner : Page
{

    private WaverunnerPageService _waverunnerPageService;
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
    private string _defaultJobName
    {
        get; set;
    }

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

    #region Constructor

    public TestWaveRunner()
    {
        InitializeComponent();
        InitWaverunnerPageService();

        // Set default job directory
        _defaultJobDirectory = @"C:\Scanner Application\Scanner Software\jobfiles";
        _jobDirectory = _defaultJobDirectory;
        JobFileSearchDirectory.Text = _jobDirectory;

        // Set default job file
        _defaultJobName = "center_crosshair_OAT.sjf";
        JobFileNameTextBox.Text = _defaultJobName;

        // ASSUMPTION: Red pointer is off when application starts
        // Have not found way to check red pointer status in SAMLight docs 
        // Initialize red pointer to off
        _redPointerEnabled = false;
    }

    #endregion

    #region Initial Setup

    private void InitWaverunnerPageService()
    {
        _waverunnerPageService = new WaverunnerPageService(JobFileSearchDirectory, JobFileNameTextBox,
                                                            StartMarkButton, IsMarkingText);
    }

    #endregion


    #region Helper Functions

    private void PrintDirectoryFiles(string targetDirectory)
    {
        var msg = "";

        if (!Directory.Exists(targetDirectory))
        {
            msg = "Directory does not exist. Cannot print files.";
            MagnetoLogger.Log(msg, Core.Contracts.Services.LogFactoryLogLevel.LogLevel.ERROR);
            return;
        }

        // Get all file names in the directory
        var fileEntries = Directory.GetFiles(targetDirectory);
        foreach (var fileName in fileEntries)
        {
            msg = $"File: {fileName}";
            MagnetoLogger.Log(msg, Core.Contracts.Services.LogFactoryLogLevel.LogLevel.SUCCESS);
        }
    }

    private int FindJobDirectory()
    {
        // Log the target directory
        var msg = $"Target Directory: {_jobDirectory}";
        MagnetoLogger.Log(msg, Core.Contracts.Services.LogFactoryLogLevel.LogLevel.VERBOSE);

        // Check if the directory exists
        if (!Directory.Exists(_jobDirectory))
        {
            msg = "Directory does not exist. Cannot get job.";
            MagnetoLogger.Log(msg, Core.Contracts.Services.LogFactoryLogLevel.LogLevel.ERROR);
            return 0;
        }
        else
        {
            return 1;
        }
    }

    private int FindFile(string fileName)
    {
        // Check if the file exists
        if (!File.Exists(fileName))
        {
            // TODO: Use Log & Display once it's extrapolated from TestPrintPage.xaml.cs
            var msg = $"Could not find: {fileName}";
            MagnetoLogger.Log(msg, Core.Contracts.Services.LogFactoryLogLevel.LogLevel.ERROR);
            _ = PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Warning", msg);
            return 0;
        }
        else
        {
            var msg = $"Found file: {fileName}";
            MagnetoLogger.Log(msg, Core.Contracts.Services.LogFactoryLogLevel.LogLevel.SUCCESS);
            return 1;
        }
    }

    #endregion

    #region Button Methods

    private void SayHelloButton_Click(object sender, RoutedEventArgs e)
    {
        _waverunnerPageService.TestWaverunnerConnection(this.Content.XamlRoot);
    }

    private void GetLastMarkButton_Click(object sender, RoutedEventArgs e)
    {
        _waverunnerPageService.GetLastMark();
    }

    private void UpdateDirectoryButton_Click(object sender, RoutedEventArgs e)
    {
        _waverunnerPageService.UpdateDirectory();
    }

    private ExecStatus ValidateJob(string fullPathToJob)
    {
        return (ExecStatus)_waverunnerPageService.ValidateJobPath(fullPathToJob, this.Content.XamlRoot);
    }

    private void GetJobButton_Click(object sender, RoutedEventArgs e)
    {
        var fullFilePath = Path.Combine(_jobDirectory, JobFileNameTextBox.Text);

        if (ValidateJob(fullFilePath) == ExecStatus.Success)
        {
            // Enable toggle pointer and start mark buttons
            _fullJobFilePath = fullFilePath;
            // TODO: Add check to make sure wave runner is open & running ("say hi check")
            // If it's not open, do not enable these buttons! Instead, display error pop up & log error
            StartMarkButton.IsEnabled = true;
            ToggleRedPointerButton.IsEnabled = true;
        }
        else
        {
            // Make sure buttons are disabled; we can't mark what we can't find!
            StartMarkButton.IsEnabled = false;
            ToggleRedPointerButton.IsEnabled = false;
        }
    }

    private void UseDefaultJobButton_Click(object sender, RoutedEventArgs e)
    {
        // Update job file name
        JobFileNameTextBox.Text = _defaultJobName;

        var msg = $"Setting job file to default job {_defaultJobName}";
        MagnetoLogger.Log(msg, Core.Contracts.Services.LogFactoryLogLevel.LogLevel.VERBOSE);

        // Check if the directory exists
        if (FindJobDirectory() == 0) // Returns 0 on fail; 1 on success
        {
            msg = "Directory does not exist. Cannot get job.";
            MagnetoLogger.Log(msg, Core.Contracts.Services.LogFactoryLogLevel.LogLevel.ERROR);
            return;
        }
    }


    // TODO: Deprecated? Remove it not needed
    private void StartRedPointerButton_Click(object sender, RoutedEventArgs e)
    {
        // File exists, proceed with marking
        var msg = $"Starting Red Pointer";
        MagnetoLogger.Log(msg, Core.Contracts.Services.LogFactoryLogLevel.LogLevel.SUCCESS);
        _waverunnerPageService.StartRedPointer();
    }

    private void ToggleRedPointerButton_Click(object sender, RoutedEventArgs e)
    {
        _redPointerEnabled = !_redPointerEnabled;

        if (_redPointerEnabled)
        {
            MagnetoLogger.Log("Starting Red Pointer", Core.Contracts.Services.LogFactoryLogLevel.LogLevel.SUCCESS);
            _waverunnerPageService.StartRedPointer();
            ToggleRedPointerButton.Background = new SolidColorBrush(Colors.Red);
            StartMarkButton.IsEnabled = false; // Disable start mark button (can't be enabled at same time as red pointer -- TODO: follow up with docs/tests to validate)
        }
        else
        {
            MagnetoLogger.Log("Stopping Red Pointer", Core.Contracts.Services.LogFactoryLogLevel.LogLevel.SUCCESS);
            _waverunnerPageService.StopRedPointer();
            ToggleRedPointerButton.Background = (SolidColorBrush)Microsoft.UI.Xaml.Application.Current.Resources["ButtonBackgroundThemeBrush"];
            // Re-enable StartMarkButton only if _fullJobFilePath is still valid
            StartMarkButton.IsEnabled = !string.IsNullOrEmpty(_fullJobFilePath) && File.Exists(_fullJobFilePath);
        }
    }

    private void StartMarkButton_Click(object sender, RoutedEventArgs e)
    {
        // File exists, proceed with marking
        var msg = $"Starting mark for file: {_fullJobFilePath}";
        MagnetoLogger.Log(msg, Core.Contracts.Services.LogFactoryLogLevel.LogLevel.VERBOSE);
        _ = _waverunnerPageService.MarkEntityAsync();
    }

    private void StopMarkButton_Click(object sender, RoutedEventArgs e)
    {
        var msg = "";

        if (cci.ScIsRunning() == 0)
        {
            msg = "SAMLight not found";
            MagnetoLogger.Log(msg, Core.Contracts.Services.LogFactoryLogLevel.LogLevel.ERROR);
            return;
        }

        _waverunnerPageService.LogMessage("Stopping Mark", Core.Contracts.Services.LogFactoryLogLevel.LogLevel.SUCCESS);

        msg = "SAMLight is stopping mark";
        MagnetoLogger.Log(msg, Core.Contracts.Services.LogFactoryLogLevel.LogLevel.WARN);

        cci.ScStopMarking();
    }

    #endregion

}
