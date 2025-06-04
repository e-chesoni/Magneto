using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Magneto.Desktop.WinUI.Core;
using Magneto.Desktop.WinUI.Core.Contracts.Services;
using Magneto.Desktop.WinUI.Core.Models.Motors;
using Microsoft.UI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using static Magneto.Desktop.WinUI.MotorPageService;

namespace Magneto.Desktop.WinUI.Models.UIControl;
public class UIControlGroupWrapper
{
    public UIControlGroupMotors? calibrateMotorControlGroup { get; set; }
    public UIControlGroupWaverunner? waverunnerControlGroup { get; set; }

    public UIControlGroupWrapper(UIControlGroupMotors calibrateMotorControlGroup)
    {
        this.calibrateMotorControlGroup = calibrateMotorControlGroup;
    }

    public UIControlGroupWrapper(UIControlGroupWaverunner printControlGroup)
    {
        this.waverunnerControlGroup = printControlGroup;
    }

    public UIControlGroupWrapper(UIControlGroupMotors calibrateMotorControlGroup, UIControlGroupWaverunner printControlGroup)
    {
        this.calibrateMotorControlGroup = calibrateMotorControlGroup;
        this.waverunnerControlGroup = printControlGroup;
    }

    #region Select Motor Helper Methods
    public void ChangeSelectedMotorButtonGreen(string selectedMotorName)
    {
        var motorNameToLower = selectedMotorName.ToLower();
        if (calibrateMotorControlGroup == null)
        {
            var msg = "❌ Calibrate control group is null.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
            return;
        }
        if (calibrateMotorControlGroup.selectBuildButton == null || calibrateMotorControlGroup.selectPowderButton == null || calibrateMotorControlGroup.selectSweepButton == null)
        {
            var msg = "❌ One of the select buttons is null.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
            return;
        }
        // Update button backgrounds and selection flags
        calibrateMotorControlGroup.selectBuildButton.Background = new SolidColorBrush(motorNameToLower == "build" ? Colors.Green : Colors.DimGray);
        calibrateMotorControlGroup.selectPowderButton.Background = new SolidColorBrush(motorNameToLower == "powder" ? Colors.Green : Colors.DimGray);
        calibrateMotorControlGroup.selectSweepButton.Background = new SolidColorBrush(motorNameToLower == "sweep" ? Colors.Green : Colors.DimGray);
    }
    public void ChangeMotorButtonsTo(Windows.UI.Color color)
    {
        if (calibrateMotorControlGroup == null)
        {
            var msg = "❌ Calibrate control group is null.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
            return;
        }
        if (calibrateMotorControlGroup.selectBuildButton == null || calibrateMotorControlGroup.selectPowderButton == null || calibrateMotorControlGroup.selectSweepButton == null)
        {
            var msg = "❌ One of the select buttons is null.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
            return;
        }
        calibrateMotorControlGroup.selectBuildButton.Background = new SolidColorBrush(color);
        calibrateMotorControlGroup.selectPowderButton.Background = new SolidColorBrush(color);
        calibrateMotorControlGroup.selectSweepButton.Background = new SolidColorBrush(color);
    }
    #endregion
    
    #region Getters
    public IUIControlGroupMotors GetCalibrationControlGroup() => calibrateMotorControlGroup;
    public IUIControlGroupWaverunner GetWaverunnerControlGroup() => waverunnerControlGroup;
    #endregion

    #region Select Motor Helper Methods
    public void SelectMotor(string motorName) => ChangeSelectedMotorButtonGreen(motorName);

    public void EnableUIControlGroup(IUIControlGroupMotors controlGrp) => EnableGroupHelper(controlGrp.GetControlGroupEnuerable());

    public void DisableUIControlGroup(IUIControlGroupMotors controlGrp) => DisableGroupHelper(controlGrp.GetControlGroupEnuerable());

    private void DisableGroupHelper(IEnumerable<object> controls)
    {
        foreach (var control in controls)
        {
            if (control is Control c && c != null)
            {
                c.IsEnabled = false;
            }
        }
    }
    private void EnableGroupHelper(IEnumerable<object> controls)
    {
        foreach (var control in controls)
        {
            if (control is Control c && c != null)
            {
                c.IsEnabled = true;
            }
        }
    }
    public void EnableMotorControls(IUIControlGroupMotors controlGrp, string motorNameLowerCase)
    {
        string msg;
        switch (motorNameLowerCase)
        {
            case "build":
                EnableGroupHelper(controlGrp.GetBuildControlGroupEnuerable());
                break;
            case "powder":
                EnableGroupHelper(controlGrp.GetPowderControlGroupEnuerable());
                break;
            case "sweep":
                EnableGroupHelper(controlGrp.GetSweepControlGroupEnuerable());
                break;
            default:
                msg = $"Unable to enable {motorNameLowerCase} controls. Invalid motor name given: {motorNameLowerCase}";
                MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
                break;
        }
    }

    public void DisableMotorControls(IUIControlGroupMotors controlGrp, string motorNameLowerCase)
    {
        string msg;
        switch (motorNameLowerCase)
        {
            case "build":
                DisableGroupHelper(controlGrp.GetBuildControlGroupEnuerable());
                break;
            case "powder":
                DisableGroupHelper(controlGrp.GetPowderControlGroupEnuerable());
                break;
            case "sweep":
                DisableGroupHelper(controlGrp.GetSweepControlGroupEnuerable());
                break;
            default:
                msg = $"Unable to disable {motorNameLowerCase} controls. Invalid motor name given: {motorNameLowerCase}";
                MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
                break;
        }
    }
    public void EnableWaverunnerSettingsControls() => EnableGroupHelper(waverunnerControlGroup.GetSettingsEnuerable());
    public void DisableWaverunnerSettingsControls() => DisableGroupHelper(waverunnerControlGroup.GetSettingsEnuerable());
    public void EnableLayerMoveButtons() => EnableGroupHelper(waverunnerControlGroup.GetLayerMoveEnumerable());
    public void DisableLayerMoveButtons() => DisableGroupHelper(waverunnerControlGroup.GetLayerMoveEnumerable());
    public void EnableMarkOnlyControls() => EnableGroupHelper(waverunnerControlGroup.GetMarkOnlyEnumerable());
    public void DisableMarkOnlyControls() => DisableGroupHelper(waverunnerControlGroup.GetMarkOnlyEnumerable());
    public void EnableMarkButton() => waverunnerControlGroup.GetMarkButton().IsEnabled = true;
    public void DisableMarkButton() => waverunnerControlGroup.GetMarkButton().IsEnabled = false;
    public void EnableMarkOnlyCheckBox() => waverunnerControlGroup.GetMarkOnlyCheckBox().IsEnabled = true;
    public void DisableMarkOnlyCheckBox() => waverunnerControlGroup.GetMarkOnlyCheckBox().IsEnabled = false;

    #endregion

}
