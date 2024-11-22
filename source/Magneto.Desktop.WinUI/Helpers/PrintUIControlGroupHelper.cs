using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Magneto.Desktop.WinUI.Core.Contracts.Services;
using Magneto.Desktop.WinUI.Core.Models.Motor;
using Magneto.Desktop.WinUI.Core.Services;
using Magneto.Desktop.WinUI.Models.UIControl;
using Microsoft.UI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using static Magneto.Desktop.WinUI.MotorPageService;

namespace Magneto.Desktop.WinUI.Helpers;
public class PrintUIControlGroupHelper
{
    public MotorUIControlGroup calibrateMotorControlGroup { get; set; }

    public MotorUIControlGroup printMotorControlGroup { get; set; }

    public PrintSettingsUIControlGroup settingsControlGroup{ get; set; }

    public PrintSettingsUIControlGroup layerControlGroup { get; set; }

    public PrintUIControlGroupHelper()
    {

    }

    public PrintUIControlGroupHelper(MotorUIControlGroup calibrateMotorControlGroup)
    {
        this.calibrateMotorControlGroup = calibrateMotorControlGroup;
    }

    public PrintUIControlGroupHelper(MotorUIControlGroup calibrateMotorControlGroup, MotorUIControlGroup printMotorControlGroup)
    {
        this.calibrateMotorControlGroup = calibrateMotorControlGroup;
        this.printMotorControlGroup = printMotorControlGroup;
    }

    public PrintUIControlGroupHelper(MotorUIControlGroup calibrateMotorControlGroup, MotorUIControlGroup printMotorControlGroup, PrintSettingsUIControlGroup settingsControlGroup, PrintSettingsUIControlGroup layerControlGroup)
    {
        this.calibrateMotorControlGroup = calibrateMotorControlGroup;
        this.printMotorControlGroup = printMotorControlGroup;
        this.settingsControlGroup = settingsControlGroup;
        this.layerControlGroup = layerControlGroup;
    }

    #region Select Motor Helper Methods

    /// <summary>
    /// Selects the given StepperMotor as the current test motor, updates the UI to reflect this selection,
    /// and toggles the selection status. Clears the position text box and updates the background color of motor selection buttons.
    /// </summary>
    /// <param name="motor">The StepperMotor to be selected as the current test motor.</param>
    /// <param name="positionTextBox">The TextBox associated with the motor, to be cleared upon selection.</param>
    /// <param name="thisMotorSelected">A reference to a boolean flag indicating the selection status of this motor.</param>
    public void SelectMotorUIHelper(StepperMotor motor, MotorUIControlGroup buttonGrp)
    {
        // Update button backgrounds and selection flags
        buttonGrp.selectBuildButton.Background = new SolidColorBrush(motor.GetMotorName() == "build" ? Colors.Green : Colors.DimGray);
        buttonGrp.selectPowderButton.Background = new SolidColorBrush(motor.GetMotorName() == "powder" ? Colors.Green : Colors.DimGray);
        buttonGrp.selectSweepButton.Background = new SolidColorBrush(motor.GetMotorName() == "sweep" ? Colors.Green : Colors.DimGray);
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
            SelectMotorUIHelper(motor, calibrateMotorControlGroup);
        }
        else
        {
            var msg = $"{motor.GetMotorName()} motor is null.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
        }
    }


    public void SelectMotorInPrint(StepperMotor motor)
    {
        if (motor != null)
        {
            SelectMotorUIHelper(motor, printMotorControlGroup);

        }
        else
        {
            var msg = "Build Motor is null.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
        }
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
    }

    #endregion

}
