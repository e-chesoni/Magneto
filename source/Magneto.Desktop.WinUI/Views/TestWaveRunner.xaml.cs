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

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Magneto.Desktop.WinUI;

public sealed partial class TestWaveRunner : Page
{

    private WaverunnerPageService _waverunnerPageService;

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
        _waverunnerPageService = new WaverunnerPageService(JobFileSearchDirectory, JobFileNameTextBox,
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
        _waverunnerPageService.GetLastMark();
    }

    private void UpdateDirectoryButton_Click(object sender, RoutedEventArgs e)
    {
        _waverunnerPageService.UpdateDirectory();
    }

    private void GetJobButton_Click(object sender, RoutedEventArgs e)
    {
        _waverunnerPageService.SetMarkJobInTestConfig(this.Content.XamlRoot);
    }

    private void UseDefaultJobButton_Click(object sender, RoutedEventArgs e)
    {
        _waverunnerPageService.UseDefaultJob();
    }


    // TODO: Deprecated? Remove it not needed
    private void StartRedPointerButton_Click(object sender, RoutedEventArgs e)
    {
        _waverunnerPageService.StartRedPointer();
    }

    private void ToggleRedPointerButton_Click(object sender, RoutedEventArgs e)
    {
        _waverunnerPageService.StartRedPointer();
    }

    private void StartMarkButton_Click(object sender, RoutedEventArgs e)
    {
        _ = _waverunnerPageService.MarkEntityAsync();
    }

    private void StopMarkButton_Click(object sender, RoutedEventArgs e)
    {
        _waverunnerPageService.StopMark();
    }

    #endregion

}
