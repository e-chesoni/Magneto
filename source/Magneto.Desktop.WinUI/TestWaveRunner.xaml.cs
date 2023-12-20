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

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Magneto.Desktop.WinUI;
/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class TestWaveRunner : Page
{
    static ScSamlightClientCtrlEx ctrlNew = new ScSamlightClientCtrlEx();

    public TestWaveRunner()
    {
        this.InitializeComponent();
    }

    private void SayHelloButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // show hello world message box in samlight
            ctrlNew.ScExecCommand((int)ScComSAMLightClientCtrlExecCommandConstants.scComSAMLightClientCtrlExecCommandTest);
        }
        catch (System.Exception exception)
        {
            var msg = "CCI Error! \n" + Convert.ToString(exception);
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
            msg = "Unable to get mark time \n" + Convert.ToString(exception);
            MagnetoLogger.Log(msg, Core.Contracts.Services.LogFactoryLogLevel.LogLevel.ERROR);
        }
        //MessageBox.Show(mark_time_string, "Info", MessageBoxButtons.OK);
    }

    private void StartMarkButton_Click(object sender, RoutedEventArgs e)
    {

    }

    private void StopMarkButton_Click(object sender, RoutedEventArgs e)
    {

    }

    private void GetJobButton_Click(object sender, RoutedEventArgs e)
    {

    }
}
