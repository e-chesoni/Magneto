using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Magneto.Desktop.WinUI.Core.Contracts.Services;
using Magneto.Desktop.WinUI.Core.Models.Motor;
using Magneto.Desktop.WinUI.Core.Services;
using Microsoft.UI;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using static Magneto.Desktop.WinUI.MotorPageService;

namespace Magneto.Desktop.WinUI.Helpers;
public class MotorSelectHelper
{
    public struct ButtonGroup
    {
        public Button selectBuildButton { get; set; }
        public Button selectPowderButton { get; set; }
        public Button selectSweepButton { get; set; }

        public ButtonGroup(Button selectBuildBtn, Button selectPowderBtn, Button selectSweepBtn)
        {
            selectBuildButton = selectBuildBtn;
            selectPowderButton = selectPowderBtn;
            selectSweepButton = selectSweepBtn;
        }
    }

    public ButtonGroup calibrationSelectMotorBtnGrp { get; set;}

    public ButtonGroup printSelectMotorBtnGrp { get; set;}

    public MotorSelectHelper(ButtonGroup calibrationSelectMotorBtnGrp)
    {
        this.calibrationSelectMotorBtnGrp = calibrationSelectMotorBtnGrp;
    }

    public MotorSelectHelper(ButtonGroup calibrationSelectMotorBtnGrp, ButtonGroup printSelectMotorBtnGrp)
    {
        this.calibrationSelectMotorBtnGrp = calibrationSelectMotorBtnGrp;
        this.printSelectMotorBtnGrp = printSelectMotorBtnGrp;
    }

    #region Select Motor Helper Methods

    /// <summary>
    /// Selects the given StepperMotor as the current test motor, updates the UI to reflect this selection,
    /// and toggles the selection status. Clears the position text box and updates the background color of motor selection buttons.
    /// </summary>
    /// <param name="motor">The StepperMotor to be selected as the current test motor.</param>
    /// <param name="positionTextBox">The TextBox associated with the motor, to be cleared upon selection.</param>
    /// <param name="thisMotorSelected">A reference to a boolean flag indicating the selection status of this motor.</param>
    public void SelectMotorUIHelper(StepperMotor motor, ButtonGroup buttonGrp)
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
            SelectMotorUIHelper(motor, calibrationSelectMotorBtnGrp);
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
            SelectMotorUIHelper(motor, printSelectMotorBtnGrp);

        }
        else
        {
            var msg = "Build Motor is null.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
        }
    }

    #endregion

}
