// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Magneto.Desktop.WinUI.Behaviors;
using Magneto.Desktop.WinUI.ViewModels;
using Magneto.Desktop.WinUI.Core.Models;
using Magneto.Desktop.WinUI.Core.Contracts.Services;
using Magneto.Desktop.WinUI.Core.Models.Motors;
using Magneto.Desktop.WinUI.Popups;
using Magneto.Desktop.WinUI.Core.Models.Print;
using Magneto.Desktop.WinUI.Core;
using System.IO.Ports;
using Magneto.Desktop.WinUI.Helpers;
using Microsoft.UI;
using static Magneto.Desktop.WinUI.Core.Models.Print.CommandQueueManager;
using static Magneto.Desktop.WinUI.Views.TestPrintPage;
using CommunityToolkit.WinUI.UI.Animations;
using CommunityToolkit.WinUI.UI.Controls.TextToolbarSymbols;
using Magneto.Desktop.WinUI.Models.UIControl;
using Magneto.Desktop.WinUI.Services;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Magneto.Desktop.WinUI;

/// <summary>
/// An empty page that can be used on its own or navigated to within a Frame.
/// </summary>
public sealed partial class TestMotorsPage : Page
{
    private MissionControl? _missionControl { get; set; }
    public TestMotorsViewModel ViewModel { get; }
    private MotorPageService? _motorPageService;
    private MotorUIControlGroup? _calibrateMotorUIControlGroup { get; set; }

    public TestMotorsPage()
    {
        ViewModel = App.GetService<TestMotorsViewModel>();
        _missionControl = App.GetService<MissionControl>();
        InitializeComponent();
        // set up flags
        //KILL_OPERATION = false;
    }

    /// <summary>
    /// Handle page startup tasks
    /// </summary>
    /// <param name="e"></param>
    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        InitPageServices();
    }
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
        // create control group helper to pass to motor page service (coordinates button color changes etc.)
        //_printControlGroupHelper = new PrintUIControlGroupHelper(_calibrateMotorUIControlGroup);

        // initialize motor page service
        _motorPageService = new MotorPageService(new PrintUIControlGroupHelper(_calibrateMotorUIControlGroup));

        // populate motor positions on page load
        _motorPageService.HandleGetAllPositions();
    }

    #region Selectors
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
    #endregion

    #region Get Position
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
    #endregion

    #region Movement Helpers
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
    private void StopMotorsHelper()
    {
        // TODO: Does not work when moved to page service (only one motor stops)...no idea why...
        var buildConfig = MagnetoConfig.GetMotorByName("build");
        var sweepConfig = MagnetoConfig.GetMotorByName("sweep");
        var buildMotor = _motorPageService.GetBuildMotor();
        var powderMotor = _motorPageService.GetPowderMotor();
        var sweepMotor = _motorPageService.GetSweepMotor();
        MagnetoLogger.Log("Writing to COM to stop", LogFactoryLogLevel.LogLevel.WARN);
        MagnetoSerialConsole.SerialWrite(buildConfig.COMPort, "1STP"); // build motor is on axis 1
        MagnetoSerialConsole.SerialWrite(buildConfig.COMPort, "2STP");
        MagnetoSerialConsole.SerialWrite(sweepConfig.COMPort, "1STP"); // sweep motor is on axis 1
        buildMotor.STOP_MOVE_FLAG = true;
        powderMotor.STOP_MOVE_FLAG = true;
        sweepMotor.STOP_MOVE_FLAG = true;
        _motorPageService.ChangeSelectButtonsBackground(Colors.Red);
    }
    private void EnableMotorsButton_Click(object sender, RoutedEventArgs e)
    {
        var buildMotor = _motorPageService.GetBuildMotor();
        var powderMotor = _motorPageService.GetPowderMotor();
        var sweepMotor = _motorPageService.GetSweepMotor();
        buildMotor.STOP_MOVE_FLAG = false;
        powderMotor.STOP_MOVE_FLAG = false;
        sweepMotor.STOP_MOVE_FLAG = false;
        _motorPageService.ChangeSelectButtonsBackground(Colors.DarkGray);
    }
    #endregion

    #region Motor Movement
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
    private async void HomeBuildMotorButton_Click(object sender, RoutedEventArgs e)
    {
        var buildMotor = _motorPageService.GetBuildMotor();
        if ((buildMotor != null) && (!_motorPageService.GetSweepMotor().STOP_MOVE_FLAG))
        {
            //_motorPageService.HandleHomeMotorAndUpdateTextBox(buildMotor, buildTextBox);
            await _motorPageService.HomeMotorAndUpdateTextBox(buildMotor);
        }
        else
        {
            MagnetoLogger.Log("Build Motor is null or stop flag is up cannot home motor.", LogFactoryLogLevel.LogLevel.ERROR);
        }
    }

    private async void HomePowderMotorButton_Click(object sender, RoutedEventArgs e)
    {
        var powderMotor = _motorPageService.GetPowderMotor();
        if ((powderMotor != null) && (!_motorPageService.GetSweepMotor().STOP_MOVE_FLAG))
        {
            //_motorPageService.HandleHomeMotorAndUpdateTextBox(powderMotor, powderTextBox);
            await _motorPageService.HomeMotorAndUpdateTextBox(powderMotor);
        }
        else
        {
            MagnetoLogger.Log("Powder Motor is null or stop flag is up cannot home motor.", LogFactoryLogLevel.LogLevel.ERROR);
        }
    }
    private async void HomeSweepMotorButton_Click(object sender, RoutedEventArgs e)
    {
        var sweepMotor = _motorPageService.GetSweepMotor();
        if ((sweepMotor != null) && (!_motorPageService.GetSweepMotor().STOP_MOVE_FLAG))
        {
            //_motorPageService.HandleHomeMotorAndUpdateTextBox(sweepMotor, sweepTextBox);
            await _motorPageService.HomeMotorAndUpdateTextBox(sweepMotor);
        }
        else
        {
            MagnetoLogger.Log("Sweep Motor is null or stop flag is up cannot home motor.", LogFactoryLogLevel.LogLevel.ERROR);
        }
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
    // TODO: implement enable buttons
    private void EnableBuildMotorButton_Click(object sender, RoutedEventArgs e)
    {
    }
    private void EnablePowderMotorButton_Click(object sender, RoutedEventArgs e)
    {
    }
    private void EnableSweepMotorButton_Click(object sender, RoutedEventArgs e)
    {
    }
    private async void HomeAllMotorsButton_Click(object sender, RoutedEventArgs e)
    {
        await HomeMotorsHelper();
    }
    private void StopAllMotorsButton_Click(object sender, RoutedEventArgs e)
    {
        StopMotorsHelper();
    }
    #endregion
}
