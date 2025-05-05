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
using static Magneto.Desktop.WinUI.Core.Models.Print.ProgramsManager;
using static Magneto.Desktop.WinUI.Views.TestPrintPage;
using CommunityToolkit.WinUI.UI.Animations;
using CommunityToolkit.WinUI.UI.Controls.TextToolbarSymbols;
using Magneto.Desktop.WinUI.Models.UIControl;
using Magneto.Desktop.WinUI.Services;
using System.Threading.Tasks;

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
    private UIControlGroupMotors? _calibrateMotorUIControlGroup { get; set; }
    private static readonly string buildMotorName = "build";
    private static readonly string powderMotorName = "powder";
    private static readonly string sweepMotorName = "sweep";

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
        _calibrateMotorUIControlGroup = new UIControlGroupMotors(SelectBuildMotorButton, SelectPowderMotorButton, SelectSweepMotorButton,
                                                                BuildMotorCurrentPositionTextBox, PowderMotorCurrentPositionTextBox, SweepMotorCurrentPositionTextBox,
                                                                GetBuildMotorCurrentPositionButton, GetPowderMotorCurrentPositionButton, GetSweepMotorCurrentPositionButton,
                                                                BuildMotorAbsPositionTextBox, PowderMotorAbsPositionTextBox, SweepMotorAbsPositionTextBox,
                                                                MoveBuildToAbsPositionButton, MovePowderToAbsPositionButton, MoveSweepToAbsPositionButton,
                                                                BuildMotorStepTextBox, PowderMotorStepTextBox, SweepMotorStepTextBox,
                                                                StepBuildMotorUpButton, StepBuildMotorDownButton, StepPowderMotorUpButton, StepPowderMotorDownButton, StepSweepMotorLeftButton, StepSweepMotorRightButton,
                                                                StopBuildMotorButton, StopPowderMotorButton, StopSweepMotorButton,
                                                                HomeAllMotorsButton, EnableMotorsButton, StopMotorsButton);
        // initialize motor page service
        _motorPageService = new MotorPageService(new UIControlGroupWrapper(_calibrateMotorUIControlGroup));

        // populate motor positions on page load
        _motorPageService.HandleGetAllPositions();
    }

    #region Helpers
    private async Task HomeMotorsHelper()
    {
        string? msg;
        if (_motorPageService == null)
        {
            msg = "_motorPageService is null. Cannot home motors.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);
            return;
        }
        await _motorPageService.HomeMotorProgramAndUpdateUI(buildMotorName);
        await _motorPageService.HomeMotorProgramAndUpdateUI(powderMotorName);
        await _motorPageService.HomeMotorProgramAndUpdateUI(sweepMotorName);
    }
    private void StopMotorsHelper()
    {
        if (_motorPageService == null)
        {
            _ = PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", "Unable to stop motors.");
            return;
        }
        _motorPageService.StopAllMotors();
    }

    // Keep as a reference; still seeing some bugs when stopping motors with new method
    /*
    private void StopMotorsHelperOLD()
    {
        if (_motorPageService == null)
        {
            _ = PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", "Unable to stop motors.");
            return;
        }
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
    */
    #endregion

    #region Selectors
    private void SelectBuildMotorButton_Click(object sender, RoutedEventArgs e)
    {
        if (_motorPageService == null)
        {
            _ = PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", "Unable to select build motor.");
            return;
        }
        _motorPageService.SelectBuildMotor();
    }
    private void SelectPowderMotorButton_Click(object sender, RoutedEventArgs e)
    {
        if (_motorPageService == null)
        {
            _ = PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", "Unable to select powder motor.");
            return;
        }
        _motorPageService.SelectPowderMotor();
    }
    private void SelectSweepMotorButton_Click(object sender, RoutedEventArgs e)
    {
        if (_motorPageService == null)
        {
            _ = PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", "Unable to select sweep motor.");
            return;
        }
        _motorPageService.SelectSweepMotor();
    }
    #endregion

    #region Position Getters
    private async void GetBuildMotorCurrentPositionButton_Click(object sender, RoutedEventArgs e)
    {
        if (_motorPageService == null)
        {
            _ = PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", "Unable to get build motor position.");
            return;
        }
        await _motorPageService.HandleGetPosition(buildMotorName, _motorPageService.GetBuildPositionTextBox(), true);
    }
    private async void GetPowderMotorCurrentPositionButton_Click(object sender, RoutedEventArgs e)
    {
        if (_motorPageService == null)
        {
            _ = PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", "Unable to get powder motor position.");
            return;
        }
        await _motorPageService.HandleGetPosition(powderMotorName, _motorPageService.GetPowderPositionTextBox(), true);
    }
    private async void GetSweepMotorCurrentPositionButton_Click(object sender, RoutedEventArgs e)
    {
        if (_motorPageService == null)
        {
            _ = PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", "Unable to get sweep motor position");
            return;
        }
        await _motorPageService.HandleGetPosition(sweepMotorName, _motorPageService.GetSweepPositionTextBox(), true);
    }
    #endregion

    #region Absolute Movers
    private void MoveBuildToAbsPositionButton_Click(object sender, RoutedEventArgs e)
    {
        if (_motorPageService == null)
        {
            _ = PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", "Unable to move build motor.");
            return;
        }
        _motorPageService.HandleAbsMove(buildMotorName, _motorPageService.GetBuildAbsMoveTextBox(), this.Content.XamlRoot);
    }
    private void MovePowderToAbsPositionButton_Click(object sender, RoutedEventArgs e)
    {
        if (_motorPageService == null)
        {
            _ = PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", "Unable to move powder motor.");
            return;
        }
        _motorPageService.HandleAbsMove(powderMotorName, _motorPageService.GetPowderAbsMoveTextBox(), this.Content.XamlRoot);
    }
    private void MoveSweepToAbsPositionButton_Click(object sender, RoutedEventArgs e)
    {
        if (_motorPageService == null)
        {
            _ = PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", "Unable to move sweep motor.");
            return;
        }
        _motorPageService.HandleAbsMove(sweepMotorName, _motorPageService.GetSweepAbsMoveTextBox(), this.Content.XamlRoot);
    }
    #endregion

    #region Relative Movers
    private void StepBuildMotorUpButton_Click(object sender, RoutedEventArgs e)
    {
        if (_motorPageService == null)
        {
            _ = PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", "Unable to build motor up.");
            return;
        }
        _motorPageService.HandleRelMove(buildMotorName, _motorPageService.GetBuildStepTextBox(), true, this.Content.XamlRoot);
    }
    private void StepBuildMotorDownButton_Click(object sender, RoutedEventArgs e)
    {
        if (_motorPageService == null)
        {
            _ = PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", "Unable to move build motor down.");
            return;
        }
        _motorPageService.HandleRelMove(buildMotorName, _motorPageService.GetBuildStepTextBox(), false, this.Content.XamlRoot);
    }
    private void StepPowderMotorUpButton_Click(object sender, RoutedEventArgs e)
    {
        if (_motorPageService == null)
        {
            _ = PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", "Unable to move powder motor up.");
            return;
        }
        _motorPageService.HandleRelMove(powderMotorName, _motorPageService.GetPowderStepTextBox(), true, this.Content.XamlRoot);
    }
    private void StepPowderMotorDownButton_Click(object sender, RoutedEventArgs e)
    {
        if (_motorPageService == null)
        {
            _ = PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", "Unable to move powder motor down.");
            return;
        }
        _motorPageService.HandleRelMove(powderMotorName, _motorPageService.GetPowderStepTextBox(), false, this.Content.XamlRoot);
    }
    private void StepSweepMotorLeftButton_Click(object sender, RoutedEventArgs e)
    {
        if (_motorPageService == null)
        {
            _ = PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", "Unable to move sweep motor left.");
            return;
        }
        _motorPageService.HandleRelMove(sweepMotorName, _motorPageService.GetSweepStepTextBox(), true, this.Content.XamlRoot);
    }
    private void StepSweepMotorRightButton_Click(object sender, RoutedEventArgs e)
    {
        if (_motorPageService == null)
        {
            _ = PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", "Unable to move sweep motor right.");
            return;
        }
        _motorPageService.HandleRelMove(sweepMotorName, _motorPageService.GetSweepStepTextBox(), false, this.Content.XamlRoot);
    }
    #endregion

    #region Homing
    private async void HomeAllMotorsButton_Click(object sender, RoutedEventArgs e)
    {
        await HomeMotorsHelper();
    }
    #endregion

    #region Stoppers
    private void StopBuildMotorButton_Click(object sender, RoutedEventArgs e)
    {
        if (_motorPageService == null)
        {
            _ = PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", "Unable to stop build motor.");
            return;
        }
        _motorPageService.StopBuildMotorAndUpdateTextBox();
    }
    private void StopPowderMotorButton_Click(object sender, RoutedEventArgs e)
    {
        if (_motorPageService == null)
        {
            _ = PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", "Unable to stop powder motor.");
            return;
        }
        _motorPageService.StopPowderMotorAndUpdateTextBox();
    }
    private void StopSweepMotorButton_Click(object sender, RoutedEventArgs e)
    {
        if (_motorPageService == null)
        {
            _ = PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", "Unable to stop sweep motor.");
            return;
        }
        _motorPageService.StopSweepMotorAndUpdateTextBox();
    }
    private void StopMotorsButton_Click(object sender, RoutedEventArgs e)
    {
        StopMotorsHelper();
        LockCalibrationPanel();
    }
    #endregion

    #region Enablers
    // TODO: Implement enablers
    private void EnableBuildMotorButton_Click(object sender, RoutedEventArgs e)
    {
        if (_motorPageService == null)
        {
            _ = PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", "Cannot enable build motor.");
            return;
        }
        _motorPageService.EnableBuildMotor();
    }
    private void EnablePowderMotorButton_Click(object sender, RoutedEventArgs e)
    {
        if (_motorPageService == null)
        {
            _ = PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", "Cannot enable powder motor.");
            return;
        }
        _motorPageService.EnablePowderMotor();
    }
    private void EnableSweepMotorButton_Click(object sender, RoutedEventArgs e)
    {
        if (_motorPageService == null)
        {
            _ = PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", "Cannot enable sweep motor.");
            return;
        }
        _motorPageService.EnableSweepMotor();
    }
    private void EnableMotorsButton_Click(object sender, RoutedEventArgs e)
    {
        if (_motorPageService == null)
        {
            _ = PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", "Unable to enable motors.");
            return;
        }
        UnlockCalibrationPanel();
    }
    #endregion

    #region Locking
    private void UnlockCalibrationPanel()
    {
        if (_motorPageService == null)
        {
            _ = PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", "Cannot unlock calibration panel.");
            return;
        }
        _motorPageService.UnlockCalibrationPanel();
        //EnableMotorsButton.Content = "Lock Motors";
    }
    private void LockCalibrationPanel()
    {
        if (_motorPageService == null)
        {
            _ = PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", "Cannot lock calibration panel.");
            return;
        }
        _motorPageService.LockCalibrationPanel();
        //EnableMotorsButton.Content = "Unlock Motors";
    }
    #endregion
}
