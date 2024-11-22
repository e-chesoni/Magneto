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
public class PrintUIControlGroupHelper
{
    public struct MotorUIControlGroup
    {
        public Button? selectBuildButton { get; set; } = null;
        public Button? selectPowderButton { get; set; } = null;
        public Button? selectSweepButton { get; set; } = null; 

        public TextBox? buildPositionTextBox { get; set; } = null;
        public TextBox? powderPositionTextBox { get; set; } = null;
        public TextBox? sweepPositionTextBox { get; set; } = null;

        public Button? getBuildPositionButton { get; set; } = null;
        public Button? getPowderPositionButton { get; set; } = null;
        public Button? getSweepPositionButton { get; set; } = null;

        public TextBox? buildStepTextBox { get; set; } = null;
        public TextBox? powderStepTextBox { get; set; } = null;
        public TextBox? sweepStepTextBox { get; set; } = null;

        public Button? incrBuildButton { get; set; } = null;
        public Button? decrBuildButton { get; set; } = null;
        public Button? incrPowderButton { get; set; } = null;
        public Button? decrPowderButton { get; set; } = null;
        public Button? incrSweepButton { get; set; } = null;
        public Button? decrSweepButton { get; set; } = null;

        public Button? stopBuildMotorButton { get; set; } = null;
        public Button? stopPowderMotorButton { get; set; } = null;
        public Button? stopSweepMotorButton { get; set; } = null;

        public TextBox? buildAbsMoveTextBox { get; set; } = null;
        public TextBox? powderAbsMoveTextBox { get; set; } = null;
        public TextBox? sweepAbsMoveTextBox { get; set; } = null;

        public Button? homeAllMotorsButton { get; set; } = null;
        public Button? stopAllMotorsButton { get; set; } = null;

        public IEnumerable<object> controlEnumerable;

        public MotorUIControlGroup(Button selectBuildBtn, Button selectPowderBtn, Button selectSweepBtn)
        {
            selectBuildButton = selectBuildBtn;
            selectPowderButton = selectPowderBtn;
            selectSweepButton = selectSweepBtn;

            controlEnumerable = new List<object>
            {
                selectBuildButton,
                selectPowderButton,
                selectSweepButton
            };
        }

        public MotorUIControlGroup(Button selectBuildBtn, Button selectPowderBtn, Button selectSweepBtn,
                                   TextBox buildPosTB, TextBox powderPosTB, TextBox sweepPosTB,
                                   TextBox buildStepTB, TextBox powderStepTB, TextBox sweepStepTB,
                                   TextBox buildAbsMoveTB, TextBox powderAbsMoveTB, TextBox sweepAbsMoveTB)
        {
            selectBuildButton = selectBuildBtn;
            selectPowderButton = selectPowderBtn;
            selectSweepButton = selectSweepBtn;

            buildPositionTextBox = buildPosTB;
            powderPositionTextBox = powderPosTB;
            sweepPositionTextBox = sweepPosTB;

            buildStepTextBox = buildStepTB;
            powderStepTextBox = powderStepTB;
            sweepStepTextBox = sweepStepTB;

            // TODO: add abs move text boxes here and above
            buildAbsMoveTextBox = buildAbsMoveTB;
            powderAbsMoveTextBox = powderAbsMoveTB;
            sweepAbsMoveTextBox = sweepAbsMoveTB;
        }

        public MotorUIControlGroup(Button selectBuildBtn, Button selectPowderBtn, Button selectSweepBtn,
                            TextBox buildPosTB, TextBox powderPosTB, TextBox sweepPosTB,
                            Button getBuildPosBtn, Button getPowderPosBtn, Button getSweepPosBtn,
                            TextBox buildStepTB, TextBox powderStepTB, TextBox sweepStepTB,
                            Button incrBuildBtn, Button decrBuildBtn, Button incrPowderBtn, Button decrPowderBtn, Button incrSweepBtn, Button decrSweepBtn,
                            Button stopBuildBtn, Button stopPowderBtn, Button stopSweepBtn,
                            Button homeAllBtn, Button stopAllBtn)
        {
            selectBuildButton = selectBuildBtn;
            selectPowderButton = selectPowderBtn;
            selectSweepButton = selectSweepBtn;

            buildPositionTextBox = buildPosTB;
            powderPositionTextBox = powderPosTB;
            sweepPositionTextBox = sweepPosTB;

            getBuildPositionButton = getBuildPosBtn;
            getPowderPositionButton = getPowderPosBtn;
            getSweepPositionButton = getSweepPosBtn;

            buildStepTextBox = buildStepTB;
            powderStepTextBox = powderStepTB;
            sweepStepTextBox = sweepStepTB;

            incrBuildButton = incrBuildBtn;
            decrBuildButton = decrBuildBtn;
            incrPowderButton = incrPowderBtn;
            decrPowderButton = decrPowderBtn;
            incrSweepButton = incrSweepBtn;
            decrSweepButton = decrSweepBtn;

            stopBuildMotorButton = stopBuildBtn;
            stopPowderMotorButton = stopPowderBtn;
            stopSweepMotorButton = stopSweepBtn;

            homeAllMotorsButton = homeAllBtn;
            stopAllMotorsButton = stopAllBtn;

            controlEnumerable = new List<object>
            {
                selectBuildButton, selectPowderButton, selectSweepButton,
                buildPositionTextBox, powderPositionTextBox, sweepPositionTextBox,
                getBuildPositionButton, getPowderPositionButton, getSweepPositionButton,
                buildStepTextBox, powderStepTextBox, sweepStepTextBox,
                incrBuildButton, decrBuildButton, incrPowderButton, decrPowderButton, incrSweepButton, decrSweepButton,
                stopBuildMotorButton, stopPowderMotorButton, stopSweepMotorButton,
                homeAllMotorsButton // NOTE: NEVER add e-stop to list (list can disable all buttons and e-stop should never be disabled)
            };
        }
    }

    public struct PrintSettingsUIControlGroup
    {
        public IEnumerable<object> controlEnumerable;
        public PrintSettingsUIControlGroup(IEnumerable<object> controls)
        {
            controlEnumerable = controls;
        }
    }


    public MotorUIControlGroup calibrateMotorControlGroup { get; set; }

    public MotorUIControlGroup printMotorControlGroup { get; set; }

    public PrintSettingsUIControlGroup settingsControlGroup{ get; set; }

    public PrintSettingsUIControlGroup layerControlGroup { get; set; }

    public PrintUIControlGroupHelper(MotorUIControlGroup calibrationSelectMotorBtnGrp)
    {
        this.calibrateMotorControlGroup = calibrationSelectMotorBtnGrp;
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

    public void EnableUIControlGroup(MotorUIControlGroup controlGrp)
    {
        foreach (var control in controlGrp.controlEnumerable)
        {
            if (control is Control c && c != null)
            {
                c.IsEnabled = true;
            }
        }
    }

    public void DisableUIControlGroup(MotorUIControlGroup controlGrp)
    {
        foreach (var control in controlGrp.controlEnumerable)
        {
            if (control is Control c && c != null)
            {
                c.IsEnabled = false;
            }
        }
    }

    #endregion

}
