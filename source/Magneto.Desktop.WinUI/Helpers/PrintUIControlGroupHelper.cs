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

    /// <summary>
    /// Selects the given StepperMotor as the current test motor, updates the UI to reflect this selection,
    /// and toggles the selection status. Clears the position text box and updates the background color of motor selection buttons.
    /// </summary>
    /// <param name="motor">The StepperMotor to be selected as the current test motor.</param>
    /// <param name="positionTextBox">The TextBox associated with the motor, to be cleared upon selection.</param>
    /// <param name="thisMotorSelected">A reference to a boolean flag indicating the selection status of this motor.</param>
    public void SelectButtonBackgroundGreen(StepperMotor motor)
    {
        // Update button backgrounds and selection flags
        calibrateMotorControlGroup.selectBuildButton.Background = new SolidColorBrush(motor.GetMotorName() == "build" ? Colors.Green : Colors.DimGray);
        calibrateMotorControlGroup.selectPowderButton.Background = new SolidColorBrush(motor.GetMotorName() == "powder" ? Colors.Green : Colors.DimGray);
        calibrateMotorControlGroup.selectSweepButton.Background = new SolidColorBrush(motor.GetMotorName() == "sweep" ? Colors.Green : Colors.DimGray);
    }
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

    /// <summary>
    /// Wrapper for motor build motor selection code
    /// </summary>
    public void SelectMotor(StepperMotor motor)
    {
        if (motor != null)
        {
            SelectButtonBackgroundGreen(motor);
        }
        else
        {
            var msg = $"{motor.GetMotorName()} motor is null.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
        }
    }

    public void SelectMotor(string motorName)
    {
        SelectButtonBackgroundGreen(motorName);
    }

    public void EnableUIControlGroup(UIControlGroup controlGrp)
    {
        foreach (var control in controlGrp.GetControlGroupEnuerable())
        {
            if (control is Control c && c != null)
            {
                c.IsEnabled = true;
            }
        }
        if (calibrateMotorControlGroup.enableMotorsButton != null)
        {
            //calibrateMotorControlGroup.enableMotorsButton.Content = "Lock Motors";
        }
    }

    public void DisableUIControlGroup(UIControlGroup controlGrp)
    {
        foreach (var control in controlGrp.GetControlGroupEnuerable())
        {
            if (control is Control c && c != null)
            {
                c.IsEnabled = false;
            }
        }
        if (calibrateMotorControlGroup.enableMotorsButton != null)
        {
            //calibrateMotorControlGroup.enableMotorsButton.Content = "Unlock Motors";
        }
    }

    #endregion

}
