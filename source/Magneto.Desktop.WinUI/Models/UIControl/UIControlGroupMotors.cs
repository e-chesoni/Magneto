using Microsoft.UI.Xaml.Controls;

namespace Magneto.Desktop.WinUI.Models.UIControl;
public class UIControlGroupMotors : IUIControlGroupMotors
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

    public TextBox? buildAbsMoveTextBox { get; set; } = null;
    public TextBox? powderAbsMoveTextBox { get; set; } = null;
    public TextBox? sweepAbsMoveTextBox { get; set; } = null;

    public Button? buildAbsMoveButton { get; set; } = null;
    public Button? powderAbsMoveButton { get; set; } = null;
    public Button? sweepAbsMoveButton { get; set; } = null;

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

    public Button? homeAllMotorsButton { get; set; } = null;
    public Button? enableMotorsButton { get; set; } = null;
    public Button? stopAllMotorsButton { get; set; } = null;

    public IEnumerable<object> controlEnumerable;
    public IEnumerable<object> buildEnumerable;
    public IEnumerable<object> powderEnumerable;
    public IEnumerable<object> sweepEnumerable;

    // for calibrate button control group on print test page
    public UIControlGroupMotors(Button selectBuildBtn, Button selectPowderBtn, Button selectSweepBtn,
                               TextBox buildPosTB, TextBox powderPosTB, TextBox sweepPosTB,
                               Button getBuildPosBtn, Button getPowderPosBtn, Button getSweepPosBtn,
                               TextBox buildAbsMoveTB, TextBox powderAbsMoveTB, TextBox sweepAbsMoveTB,
                               Button buildAbsMoveBtn, Button powderAbsMoveBtn, Button sweepAbsMoveBtn,
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

        buildAbsMoveButton = buildAbsMoveBtn;
        powderAbsMoveButton = powderAbsMoveBtn;
        sweepAbsMoveButton = sweepAbsMoveBtn;

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

        // NOTE: NEVER add e-stop to enumerable lists (used to disable buttons and e-stop should never be disabled)
        controlEnumerable = new List<object>
        {
            selectBuildButton, selectPowderButton, selectSweepButton,
            buildPositionTextBox, powderPositionTextBox, sweepPositionTextBox,
            getBuildPositionButton, getPowderPositionButton, getSweepPositionButton,
            buildAbsMoveTextBox, powderAbsMoveTextBox, sweepAbsMoveTextBox,
            buildAbsMoveButton, powderAbsMoveButton, sweepAbsMoveButton,
            buildStepTextBox, powderStepTextBox, sweepStepTextBox,
            incrBuildButton, decrBuildButton, incrPowderButton, decrPowderButton, incrSweepButton, decrSweepButton,
            homeAllMotorsButton
        };
        buildEnumerable = new List<object>
        {
            selectBuildButton, buildPositionTextBox, getBuildPositionButton, buildAbsMoveButton, buildAbsMoveTextBox,
            buildStepTextBox, incrBuildButton, decrBuildButton, stopBuildMotorButton,
            homeAllMotorsButton
        };
        powderEnumerable = new List<object>
        {
            selectPowderButton, powderPositionTextBox, getPowderPositionButton, powderAbsMoveButton, powderAbsMoveTextBox,
            powderStepTextBox, incrPowderButton, decrPowderButton, stopPowderMotorButton,
            homeAllMotorsButton
        };
        sweepEnumerable = new List<object>
        {
            selectSweepButton, sweepPositionTextBox, getSweepPositionButton, sweepAbsMoveButton, sweepAbsMoveTextBox,
            sweepStepTextBox, incrSweepButton, decrSweepButton, stopSweepMotorButton,
            homeAllMotorsButton
        };
    }
    IEnumerable<object> IUIControlGroupMotors.GetControlGroupEnuerable() => controlEnumerable;
    IEnumerable<object> IUIControlGroupMotors.GetBuildControlGroupEnuerable() => buildEnumerable;
    IEnumerable<object> IUIControlGroupMotors.GetPowderControlGroupEnuerable() => powderEnumerable;
    IEnumerable<object> IUIControlGroupMotors.GetSweepControlGroupEnuerable() => sweepEnumerable;
}

