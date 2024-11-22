using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Controls;

namespace Magneto.Desktop.WinUI.Models.UIControl;
public class MotorUIControlGroup : UIControlGroup
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

    // For motor test page control buttons
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

        controlEnumerable = new List<object>
            {
                selectBuildButton,
                selectPowderButton,
                selectSweepButton
            };
    }

    // for in print control buttons on print test page
    public MotorUIControlGroup(Button selectBuildBtn, Button selectPowderBtn, Button selectSweepBtn,
                        TextBox buildPosTB, TextBox powderPosTB, TextBox sweepPosTB,
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
                buildStepTextBox, powderStepTextBox, sweepStepTextBox,
                incrBuildButton, decrBuildButton, incrPowderButton, decrPowderButton, incrSweepButton, decrSweepButton,
                stopBuildMotorButton, stopPowderMotorButton, stopSweepMotorButton,
                homeAllMotorsButton // NOTE: NEVER add e-stop to list (list can disable all buttons and e-stop should never be disabled)
            };
    }

    // for calibrate button control group on print test page
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

    IEnumerable<object> UIControlGroup.GetControlGroupEnuerable()
    {
        return controlEnumerable;
    }
}

