using Microsoft.UI.Xaml.Controls;

namespace Magneto.Desktop.WinUI.Models.UIControl;
public class UIControlGroupWaverunner : IUIControlGroupWaverunner
{
    public TextBox printDirectoryInputTextBox;

    public TextBlock layerTextBlock;
    public TextBlock fileNameTextBlock;
    public TextBlock layerThicknessTextBlock;
    public TextBlock laserPowerTextBlock;
    public TextBlock scanSpeedTextBlock;
    public TextBlock hatchSpacingTextBlock;
    public TextBlock energyDensityTextBlock;
    public TextBlock slicesToMarkTextBlock;
    public TextBlock supplyAmplifierTextBlock;

    public TextBox layerTextBox;
    public TextBox fileNameTextBox;
    public TextBox layerThicknessTextBox;
    public TextBox laserPowerTextBox;
    public TextBox scanSpeedTextBox;
    public TextBox hatchSpacingTextBox;
    public TextBox energyDensityTextBox;
    public TextBox slicesToMarkTextBox;
    public TextBox supplyAmplifierTextBox;

    public CheckBox startWithMarkCheckBox;

    public Button printLayersButton;
    public Button pausePrintButton;
    public Button remarkLayerButton;

    public IEnumerable<object> settingsEnumerable;
    public IEnumerable<object> buttonsEnumerable;

    public UIControlGroupWaverunner(TextBox printDirectoryInputTextBx, TextBlock layerTextBlk, TextBlock fileNameTextBlk, TextBlock layerThicknessTextBlk, 
                               TextBlock laserPowerTextBlk, TextBlock scanSpeedTextBlk, TextBlock hatchSpacingTextBlk, 
                               TextBlock energyDensityTextBlk, TextBlock slicesToMarkTextBlk, TextBlock supplyAmplifierTextBlk,
                               TextBox layerTextBx, TextBox fileNameTextBx, TextBox layerThicknessTextBx, TextBox laserPowerTextBx, 
                               TextBox scanSpeedTextBx, TextBox hatchSpacingTextBx, TextBox energyDensityTextBx, TextBox slicesToMarkTextBx, 
                               TextBox supplyAmplifierTextBx, CheckBox startWithMarkCheckBx, Button PrintLayersBtn, Button PausePrintBtn, Button RemarkLayerBtn)
    {
        printDirectoryInputTextBox = printDirectoryInputTextBx;
        layerTextBlock = layerTextBlk;
        fileNameTextBlock = fileNameTextBlk;
        layerThicknessTextBlock = layerThicknessTextBlk;
        laserPowerTextBlock = laserPowerTextBlk;
        scanSpeedTextBlock = scanSpeedTextBlk;
        hatchSpacingTextBlock = hatchSpacingTextBlk;
        energyDensityTextBlock = energyDensityTextBlk;
        slicesToMarkTextBlock = slicesToMarkTextBlk;
        supplyAmplifierTextBlock = supplyAmplifierTextBlk;
        layerTextBox = layerTextBx;
        fileNameTextBox = fileNameTextBx;
        layerThicknessTextBox = layerThicknessTextBx;
        laserPowerTextBox = laserPowerTextBx;
        scanSpeedTextBox = scanSpeedTextBx;
        hatchSpacingTextBox = hatchSpacingTextBx;
        energyDensityTextBox = energyDensityTextBx;
        slicesToMarkTextBox = slicesToMarkTextBx;
        supplyAmplifierTextBox = supplyAmplifierTextBx;

        startWithMarkCheckBox = startWithMarkCheckBx;

        printLayersButton = PrintLayersBtn;
        pausePrintButton = PausePrintBtn;
        remarkLayerButton = RemarkLayerBtn;

        settingsEnumerable = new List<object>
        {
            layerTextBlock,
            fileNameTextBlock,
            layerThicknessTextBlock,
            laserPowerTextBlock,
            scanSpeedTextBlock,
            hatchSpacingTextBlock,
            energyDensityTextBlock,
            slicesToMarkTextBlock,
            supplyAmplifierTextBlock,
            layerTextBox,
            fileNameTextBox,
            layerThicknessTextBox,
            laserPowerTextBox,
            scanSpeedTextBox,
            hatchSpacingTextBox,
            energyDensityTextBox,
            slicesToMarkTextBox,
            supplyAmplifierTextBox,
            startWithMarkCheckBox
        };
        // NOTE: Never add pause/stop buttons to enumerables (they disable buttons)
        buttonsEnumerable = new List<object>
        {
            printLayersButton, remarkLayerButton,
        };
    }
    public IEnumerable<object> GetSettingsEnuerable() => settingsEnumerable;
    public IEnumerable<object> GetButtonGroupEnuerable() => buttonsEnumerable;
}
