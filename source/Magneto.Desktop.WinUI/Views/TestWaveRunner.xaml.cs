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
using static Magneto.Desktop.WinUI.Services.WaverunnerPageService;
using System.Diagnostics;
using Windows.Storage.Pickers;
using WinRT.Interop;
using Magneto.Desktop.WinUI.ViewModels;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Magneto.Desktop.WinUI;

public sealed partial class TestWaveRunner : Page
{
    private WaverunnerPageService _waverunnerPageService;
    public TestWaverunnerViewModel ViewModel
    {
        get;
    }

    #region Constructor
    public TestWaveRunner()
    {
        InitializeComponent();
        InitWaverunnerPageService();
    }
    #endregion

    #region Initial Setup
    private void InitWaverunnerPageService()
    {
        _waverunnerPageService = new WaverunnerPageService(JobPathTextBox,
                                                           ToggleRedPointerButton, StartMarkButton, IsMarkingText);
    }
    #endregion

    #region Button Methods

    private void SayHelloButton_Click(object sender, RoutedEventArgs e)
    {
        _waverunnerPageService.TestWaverunnerConnection(this.Content.XamlRoot);
    }

    private void GetLastMarkButton_Click(object sender, RoutedEventArgs e)
    {
        _waverunnerPageService.GetLastMark(this.Content.XamlRoot);
    }

    private void ToggleRedPointerButton_Click(object sender, RoutedEventArgs e)
    {
        _waverunnerPageService.ToggleRedPointer(this.Content.XamlRoot, JobPathTextBox.Text);
    }

    private void StartMarkButton_Click(object sender, RoutedEventArgs e)
    {
        _ = _waverunnerPageService.MarkEntityAsync(this.Content.XamlRoot, JobPathTextBox.Text);
    }

    private void StopMarkButton_Click(object sender, RoutedEventArgs e)
    {
        _waverunnerPageService.StopMark(this.Content.XamlRoot);
    }

    // WARNING: I believe this only works in xaml.cs...verify this later
    private async void BrowseButton_Click(object sender, RoutedEventArgs e)
    {
        string? msg;
        var filePicker = new FileOpenPicker();
        filePicker.SuggestedStartLocation = PickerLocationId.Desktop;
        filePicker.FileTypeFilter.Add("*");

        var hwnd = WindowNative.GetWindowHandle(App.MainWindow);
        InitializeWithWindow.Initialize(filePicker, hwnd);

        var file = await filePicker.PickSingleFileAsync();
        if (file != null)
        {
            if (!file.Name.EndsWith(".sjf", StringComparison.OrdinalIgnoreCase))
            {
                Debug.WriteLine("Selected file is not a .sjf file.");
                var dialog = new ContentDialog
                {
                    Title = "Invalid File Type",
                    Content = "Please select a valid .sjf job file.",
                    CloseButtonText = "OK",
                    XamlRoot = this.Content.XamlRoot
                };
                await dialog.ShowAsync();
                return;
            }
            JobPathTextBox.Text = file.Path;
            ToggleRedPointerButton.IsEnabled = true;
            StartMarkButton.IsEnabled = true;
            StopMarkButton.IsEnabled = true;
        }
        else 
        {
            msg = $"Could not retrieve empty file.";
            _ = PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", msg);
        }
    }
    #endregion
}
