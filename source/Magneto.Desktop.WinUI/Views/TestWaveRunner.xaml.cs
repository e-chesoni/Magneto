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
using ABI.System;
using static System.Net.Mime.MediaTypeNames;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Magneto.Desktop.WinUI;
/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class TestWaveRunner : Page
{
    private static ScSamlightClientCtrlEx ctrlNew = new ScSamlightClientCtrlEx();
    private string m_text_is_marking = "";
    private string _defaultJobDirectory { get; set; }
    private string _jobDirectory { get; set; }
    private string _jobFilePath { get; set; }
    private string _defaultJobName { get; set; }
    
    public TestWaveRunner()
    {
        InitializeComponent();

        // Set Job Directory
        _defaultJobDirectory = @"C:\Scanner Application\Scanner Software\jobfiles";
        _jobDirectory = _defaultJobDirectory;
        JobFileSearchDirectory.Text = _jobDirectory;

        // Set Job File
        _defaultJobName = "100mm_Square.sjf";
        JobFileNameTextBox.Text = _defaultJobName;
    }

    #region Button Methods
    private void SayHelloButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // show hello world message box in samlight
            ctrlNew.ScExecCommand((int)ScComSAMLightClientCtrlExecCommandConstants.scComSAMLightClientCtrlExecCommandTest);
        }
        catch (System.Exception exception)
        {
            var msg = $"CCI Error! \n {Convert.ToString(exception)}";
            MagnetoLogger.Log(msg, Core.Contracts.Services.LogFactoryLogLevel.LogLevel.ERROR);
        }
    }

    private void GetLastMarkButton_Click(object sender, RoutedEventArgs e)
    {
        var msg = "Get last mark requested...";
        MagnetoLogger.Log(msg, Core.Contracts.Services.LogFactoryLogLevel.LogLevel.VERBOSE);

        if (ctrlNew.ScIsRunning() == 0)
        {
            msg = "SAMLight not found";
            MagnetoLogger.Log(msg, Core.Contracts.Services.LogFactoryLogLevel.LogLevel.ERROR);
            return;
        }

        var mark_time = 0.0;

        try
        {
            mark_time = ctrlNew.ScGetDoubleValue((int)ScComSAMLightClientCtrlValueTypes.scComSAMLightClientCtrlDoubleValueTypeLastMarkTime);
            var mark_time_string = string.Concat("Last mark time was: ", mark_time, " seconds");
            MagnetoLogger.Log(mark_time_string, Core.Contracts.Services.LogFactoryLogLevel.LogLevel.VERBOSE);
        }
        catch (System.Exception exception)
        {
            msg = $"Unable to get mark time \n {Convert.ToString(exception)}";
            MagnetoLogger.Log(msg, Core.Contracts.Services.LogFactoryLogLevel.LogLevel.ERROR);
        }
        //MessageBox.Show(mark_time_string, "Info", MessageBoxButtons.OK);
    }

    private void StartMarkButton_Click(object sender, RoutedEventArgs e)
    {
        // File exists, proceed with marking
        var msg = $"Starting mark for file: {_jobFilePath}";
        MagnetoLogger.Log(msg, Core.Contracts.Services.LogFactoryLogLevel.LogLevel.VERBOSE);
        _ = MarkEntityAsync(JobFileNameTextBox.Text);
    }

    private void StopMarkButton_Click(object sender, RoutedEventArgs e)
    {
        var msg = "";

        if (ctrlNew.ScIsRunning() == 0)
        {
            msg = "SAMLight not found";
            MagnetoLogger.Log(msg, Core.Contracts.Services.LogFactoryLogLevel.LogLevel.ERROR);
            return;
        }

        m_text_is_marking = "Stopping Mark";
        UpdateUIText(m_text_is_marking);

        msg = "SAMLight is stopping mark";
        MagnetoLogger.Log(msg, Core.Contracts.Services.LogFactoryLogLevel.LogLevel.WARN);

        ctrlNew.ScStopMarking();
    }

    private void UpdateDirectoryButton_Click(object sender, RoutedEventArgs e)
    {
        _jobDirectory = JobFileSearchDirectory.Text;
        StartMarkButton.IsEnabled = false;
    }

    private void GetJobButton_Click(object sender, RoutedEventArgs e)
    {
        var msg = "Getting job...";
        MagnetoLogger.Log(msg, Core.Contracts.Services.LogFactoryLogLevel.LogLevel.VERBOSE);

        // Check if the directory exists
        if (FindJobDirectory() < 0)
        {
            msg = "Directory does not exist. Cannot get job.";
            MagnetoLogger.Log(msg, Core.Contracts.Services.LogFactoryLogLevel.LogLevel.ERROR);
            return;
        }

        //PrintDirectoryFiles(targetDirectory);

        // Construct the full file path
        var fullFilePath = Path.Combine(_jobDirectory, JobFileNameTextBox.Text);

        // Check if the file exists
        if (FindFile(fullFilePath) == 0) // FindFile returns 0 if file does not exist
        {
            msg = $"File not found: {fullFilePath}";
            MagnetoLogger.Log(msg, Core.Contracts.Services.LogFactoryLogLevel.LogLevel.ERROR);
            return;
        }
        else // If file exists, set _jobFilePath to full path name, and enable start job button
        {
            _jobFilePath = fullFilePath;
            StartMarkButton.IsEnabled= true;
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
            var msg = $"Could not find: {fileName}";
            MagnetoLogger.Log(msg, Core.Contracts.Services.LogFactoryLogLevel.LogLevel.ERROR);
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

    #region Marking Methods

    private async Task<int> MarkEntityAsync(string entityNameToMark)
    {
        var msg = "";
        if (ctrlNew.ScIsRunning() == 0)
        {
            msg = "SAMLight not found";
            MagnetoLogger.Log(msg, Core.Contracts.Services.LogFactoryLogLevel.LogLevel.ERROR);
            m_text_is_marking = "Cannot Mark; WaveRunner is closed.";
            UpdateUIText(m_text_is_marking);
            StartMarkButton.IsEnabled = false;
            return 0;
        }

        m_text_is_marking = "Sending Objects!";
        UpdateUIText(m_text_is_marking); // Update UI method
        long markFlags = 0x0;
        ctrlNew.ScSetMarkFlags((int)markFlags);

        // TODO: Test actual marking

        // ACTUAL MARKING
        /*
        // Starts marking
        ctrlNew.ScMarkEntityByName(entityNameToMark, 0);
        m_text_is_marking = "Marking!";
        UpdateUIText(m_text_is_marking);

        msg = "SAMLight is Marking...";
        MagnetoLogger.Log(msg, Core.Contracts.Services.LogFactoryLogLevel.LogLevel.WARN);

        // Loop checks to see if laser is still marking
        await Task.Run(() =>
        {
            int i = 0;
            while (true)
            {
                i++;
                if (i % 10 == 0)
                {
                    if (ctrlNew.ScIsMarking() == 0)
                        break;
                }
            }
        });

        ctrlNew.ScStopMarking();

        // END OF ACTUAL MARKING
        */

        // TEST MARKING
        await Task.Delay(1000); // false wait for sending objects

        m_text_is_marking = "Marking!";
        UpdateUIText(m_text_is_marking); // Update UI method

        msg = "SAMLight is marking...";
        MagnetoLogger.Log(msg, Core.Contracts.Services.LogFactoryLogLevel.LogLevel.WARN);

        await Task.Delay(5000);
        // END OF TEST

        m_text_is_marking = "Done Marking";
        UpdateUIText(m_text_is_marking); // Update UI method

        msg = "SAMLight is done marking";
        MagnetoLogger.Log(msg, Core.Contracts.Services.LogFactoryLogLevel.LogLevel.SUCCESS);
        StartMarkButton.IsEnabled = false;

        return 1;
    }

    private void UpdateUIText(string text)
    {
        // Ensure the UI update is performed on the UI thread
        DispatcherQueue.TryEnqueue(() =>
        {
            IsMarkingText.Text = text;
        });
    }

    #endregion

}
