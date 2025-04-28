using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Magneto.Desktop.WinUI.Core;
using Magneto.Desktop.WinUI.Core.Contracts.Services;
using Magneto.Desktop.WinUI.Core.Models.Motors;
using Magneto.Desktop.WinUI.Models.UIControl;
using Microsoft.UI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using static Magneto.Desktop.WinUI.MotorPageService;

namespace Magneto.Desktop.WinUI.Helpers;
public class PrintUIControlGroupHelper
{
    public MotorUIControlGroup calibrateMotorControlGroup { get; set; }

    public PrintUIControlGroupHelper(MotorUIControlGroup calibrateMotorControlGroup)
    {
        this.calibrateMotorControlGroup = calibrateMotorControlGroup;
    }

    #region Select Motor Helper Methods
    public void SelectButtonBackgroundGreen(string motorName)
    {
        string motorNameToLower = motorName.ToLower();
        // Update button backgrounds and selection flags
        calibrateMotorControlGroup.selectBuildButton.Background = new SolidColorBrush(motorNameToLower == "build" ? Colors.Green : Colors.DimGray);
        calibrateMotorControlGroup.selectPowderButton.Background = new SolidColorBrush(motorNameToLower == "powder" ? Colors.Green : Colors.DimGray);
        calibrateMotorControlGroup.selectSweepButton.Background = new SolidColorBrush(motorNameToLower == "sweep" ? Colors.Green : Colors.DimGray);
    }
    public void ChangeSelectButtonsBackground(Windows.UI.Color color)
    {
        // Update button backgrounds and selection flags
        calibrateMotorControlGroup.selectBuildButton.Background = new SolidColorBrush(color);
        calibrateMotorControlGroup.selectPowderButton.Background = new SolidColorBrush(color);
        calibrateMotorControlGroup.selectSweepButton.Background = new SolidColorBrush(color);
    }
    #endregion

    #region Getters
    public UIControlGroup GetCalibrationControlGroup()
    {
        return calibrateMotorControlGroup; 
    }
    #endregion

    #region Select Motor Helper Methods
    public void SelectMotor(string motorName)
    {
        SelectButtonBackgroundGreen(motorName);
    }

    public void EnableUIControlGroup(UIControlGroup controlGrp)
    {
        EnableGroupHelper(controlGrp.GetControlGroupEnuerable());
    }

    public void DisableUIControlGroup(UIControlGroup controlGrp)
    {
        DisableGroupHelper(controlGrp.GetControlGroupEnuerable());
    }

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
    public void EnableMotorControls(UIControlGroup controlGrp, string motorNameLowerCase)
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

    public void DisableMotorControls(UIControlGroup controlGrp, string motorNameLowerCase)
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

    #endregion

}
