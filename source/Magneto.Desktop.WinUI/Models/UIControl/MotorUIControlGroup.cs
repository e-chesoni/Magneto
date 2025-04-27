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
    public Button? enableMotorsButton { get; set; } = null;
    public Button? stopAllMotorsButton { get; set; } = null;

    public IEnumerable<object> controlEnumerable;

    // for calibrate button control group on print test page
    public MotorUIControlGroup(Button selectBuildBtn, Button selectPowderBtn, Button selectSweepBtn,
                               TextBox buildPosTB, TextBox powderPosTB, TextBox sweepPosTB,
                               Button getBuildPosBtn, Button getPowderPosBtn, Button getSweepPosBtn,
                               TextBox buildAbsMoveTB, TextBox powderAbsMoveTB, TextBox sweepAbsMoveTB,
                               TextBox buildStepTB, TextBox powderStepTB, TextBox sweepStepTB,
                               Button incrBuildBtn, Button decrBuildBtn, Button incrPowderBtn, Button decrPowderBtn, Button incrSweepBtn, Button decrSweepBtn,
                               Button stopBuildBtn, Button stopPowderBtn, Button stopSweepBtn,
                               Button homeAllBtn, Button enableMotorsBtn, Button stopAllBtn)
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

        buildAbsMoveTextBox = buildAbsMoveTB;
        powderAbsMoveTextBox = powderAbsMoveTB;
        sweepAbsMoveTextBox = sweepAbsMoveTB;

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
        enableMotorsButton = enableMotorsBtn;
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

