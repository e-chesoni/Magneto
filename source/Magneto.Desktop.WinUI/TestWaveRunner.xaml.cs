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
        catch (Exception exception)
        {
            var msg = "CCI Error! \n" + Convert.ToString(exception);
            MagnetoLogger.Log(msg, Core.Contracts.Services.LogFactoryLogLevel.LogLevel.ERROR);
        }
    }
}
