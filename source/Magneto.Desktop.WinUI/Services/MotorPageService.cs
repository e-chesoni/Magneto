using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Magneto.Desktop.WinUI.Core.Contracts.Services;
using Magneto.Desktop.WinUI.Core.Models.Motors;
using Magneto.Desktop.WinUI.Popups;
using Magneto.Desktop.WinUI.Core;
using MongoDB.Driver;
using Microsoft.UI;
using Magneto.Desktop.WinUI.Models.UIControl;
using Magneto.Desktop.WinUI.Core.Services;
using Magneto.Desktop.WinUI.Core.Models.Print;
using static Magneto.Desktop.WinUI.Core.Models.Constants.MagnetoConstants;
using static Magneto.Desktop.WinUI.Core.Models.Print.RoutineStateMachine;

namespace Magneto.Desktop.WinUI;
public class MotorPageService
{
    private readonly IMotorService _motorService;
    public RoutineStateMachine _rsm;
    private UIControlGroupWrapper _uiControlGroupHelper { get; set; }
    public static readonly string buildMotorName =  "build";
    public static readonly string powderMotorName = "powder";
    public static readonly string sweepMotorName = "sweep";
    public MotorPageService(UIControlGroupWrapper printCtlGrpHelper, RoutineStateMachine rsm)
    {
        _motorService = App.GetService<IMotorService>();
        _motorService.HandleStartUp();
        _uiControlGroupHelper = printCtlGrpHelper;
        _rsm = rsm;
    }

    // NEW METHODS -- WHO DIS?
    public void StopAllMotorsClearProgramList() => _motorService.StopAllMotorsClearProgramList();
    public void ResumeProgram() => _motorService.ResumeProgram();
    public void PauseProgram() => _motorService.PauseProgram();
    public void ClearProgramList() => _motorService.ClearProgramList();
    public bool IsPrintPaused() => _rsm.status == RoutineStateMachineStatus.Paused;
    public Task<bool> IsProgramRunningAsync(string motorNameLower) => _motorService.IsProgramRunningAsync();

    // TODO: pause motors (i.e. stop but don't clear list)
    public void StopAllMotors() => _motorService.StopAllMotors();

    #region Locks
    public void UnlockCalibrationPanel()
    {
        _uiControlGroupHelper.EnableUIControlGroup(_uiControlGroupHelper.GetCalibrationControlGroup());
    }
    public void LockCalibrationPanel()
    {
        _uiControlGroupHelper.DisableUIControlGroup(_uiControlGroupHelper.GetCalibrationControlGroup());
    }
    #endregion

    #region Selectors
    public void SelectBuildMotor()
    {
        _uiControlGroupHelper.SelectMotor(buildMotorName);
    }
    public void SelectPowderMotor()
    {
        _uiControlGroupHelper.SelectMotor(powderMotorName);
    }
    public void SelectSweepMotor()
    {
        _uiControlGroupHelper.SelectMotor(sweepMotorName);
    }
    public void ChangeSelectButtonsBackground(Windows.UI.Color color)
    {
        _uiControlGroupHelper.ChangeMotorButtonsTo(color);
    }
    #endregion

    #region Positioning
    #region Position Getters
    public TextBox GetBuildPositionTextBox()
    {
        return _uiControlGroupHelper.calibrateMotorControlGroup.buildPositionTextBox;
    }
    public TextBox GetPowderPositionTextBox()
    {
        return _uiControlGroupHelper.calibrateMotorControlGroup.powderPositionTextBox;
    }
    public TextBox GetSweepPositionTextBox()
    {
        return _uiControlGroupHelper.calibrateMotorControlGroup.sweepPositionTextBox;
    }
    #endregion
    #region Position Handlers
    public async Task HandleGetPosition(string motorName, TextBox textBox, bool selectMotor)
    {
        var motorNameToLower = motorName.ToLower();
        int res;
        double pos;
        var msg = "Get position button clicked...";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);
        // get position of requested motor
        (res, pos) = await _motorService.GetMotorPositionAsync(motorNameToLower);
        // check request result
        if (res == 0)
        {
            msg = $"Unable to handle getting {motorNameToLower} motor position.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
            return;
        }
        // if get position was successful, validate text box
        if (textBox != null) // Full error checking in UITextHelper
        {
            UpdateUITextHelper.UpdateUIText(textBox, pos.ToString());
        }
        // if select motor is true, select the button too
        if (selectMotor)
        {
            _uiControlGroupHelper.SelectMotor(motorNameToLower);
        }
    }
    public async Task HandleGetAllPositionsAsync()
    {
        var msg = "Get position button clicked...";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);
        await HandleGetPosition(buildMotorName, GetBuildPositionTextBox(), false);
        await HandleGetPosition(powderMotorName, GetPowderPositionTextBox(), false);
        await HandleGetPosition(sweepMotorName, GetSweepPositionTextBox(), false);
    }
    #endregion
    #endregion

    #region Movement
    #region Main Movement Commands
    public async Task<(int status, double targetPos)> MoveMotorAbsoluteProgram(string motorName, TextBox textBoxToRead)
    {
        var motorNameLower = motorName.ToLower();
        if (textBoxToRead == null || !double.TryParse(textBoxToRead.Text, out _))
        {
            var msg = $"invalid input in {motorName} text box.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
            return (0, 0);
        }
        else
        {
            var targetPos = double.Parse(textBoxToRead.Text);
            var moveUp = targetPos > 0;
            await _motorService.MoveMotorAbsoluteProgram(motorNameLower, targetPos);
            return (1, targetPos);
        }
    }
    public async Task<(int status, double targetPos)> MoveMotorRelativeProgram(string motorName, TextBox textBoxToRead, bool moveUp)
    {
        var motorNameLower = motorName.ToLower();
        if (textBoxToRead == null || !double.TryParse(textBoxToRead.Text, out var _))
        {
            var msg = $"invalid input in {motorNameLower} text box.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
            return (0, 0);
        }
        else
        {
            // Convert distance to an absolute number to avoid confusing user
            var steps = Math.Abs(double.Parse(textBoxToRead.Text));
            // Update the text box with corrected distance
            textBoxToRead.Text = steps.ToString();
            // Calculate target distance
            var (res, currentPos) = await _motorService.GetMotorPositionAsync(motorNameLower);
            var distance = moveUp ? steps : -steps;
            var targetPos = currentPos + distance;
            // WARNING: pass abs distance anywhere moveUp variable is present
            await _motorService.MoveMotorRelativeProgram(motorNameLower, steps, moveUp);
            return (res, targetPos);
        }
    }
    #endregion

    #region Movement Handlers
    public void HandleAbsMove(string motorName, TextBox textBox, XamlRoot xamlRoot)
    {
        var motorNameLower = motorName.ToLower();
        var msg = $"{motorNameLower} abs move button clicked.";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.SUCCESS);
        var moveIsAbs = true;
        var moveUp = true;
        MoveMotorAndUpdateUISelector(motorName, textBox, moveIsAbs, moveUp, xamlRoot);
    }

    public void HandleRelMove(string motorName, TextBox textBox, bool moveUp, XamlRoot xamlRoot)
    {
        var motorNameLower = motorName.ToLower();
        var msg = $"{motorNameLower} rel move button clicked.";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.SUCCESS);
        var moveIsAbs = false;
        MoveMotorAndUpdateUISelector(motorName, textBox, moveIsAbs, moveUp, xamlRoot);
    }
    #endregion

    #region Stop Methods
    public void StopBuildMotorAndDisableControls()
    {
        var msg = $"stopping {buildMotorName} motor";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.SUCCESS);
        _motorService.StopMotorAndClearProgramList(buildMotorName); // UI is already updated when stop clicked...by magic i guess
        _uiControlGroupHelper.DisableMotorControls(_uiControlGroupHelper.GetCalibrationControlGroup(), buildMotorName);
    }
    public void StopPowderMotorAndDisableControls()
    {
        var msg = $"stopping {powderMotorName} motor";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.SUCCESS);
        _motorService.StopMotorAndClearProgramList(powderMotorName);
        _uiControlGroupHelper.DisableMotorControls(_uiControlGroupHelper.GetCalibrationControlGroup(), powderMotorName);
    }
    public void StopSweepMotorAndDisbleControls()
    {
        var msg = $"stopping {sweepMotorName} motor";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.SUCCESS);
        _motorService.StopMotorAndClearProgramList(sweepMotorName);
        _uiControlGroupHelper.DisableMotorControls(_uiControlGroupHelper.GetCalibrationControlGroup(), sweepMotorName);
    }
    #endregion
    #endregion

    #region Enablers
    public void EnableProgramRunning() => _motorService.EnableProgram();

    public void EnableBuildMotor()
    {
        EnableProgramRunning();
        _uiControlGroupHelper.EnableMotorControls(_uiControlGroupHelper.GetCalibrationControlGroup(), buildMotorName);
    }
    public void EnablePowderMotor()
    {
        EnableProgramRunning();
        _uiControlGroupHelper.EnableMotorControls(_uiControlGroupHelper.GetCalibrationControlGroup(), powderMotorName);
    }
    public void EnableSweepMotor()
    {
        EnableProgramRunning();
        _uiControlGroupHelper.EnableMotorControls(_uiControlGroupHelper.GetCalibrationControlGroup(), sweepMotorName);
    }
    public void EnableAllMotors()
    {
        EnableProgramRunning();
        _uiControlGroupHelper.EnableMotorControls(_uiControlGroupHelper.GetCalibrationControlGroup(), buildMotorName);
        _uiControlGroupHelper.EnableMotorControls(_uiControlGroupHelper.GetCalibrationControlGroup(), powderMotorName);
        _uiControlGroupHelper.EnableMotorControls(_uiControlGroupHelper.GetCalibrationControlGroup(), sweepMotorName);
    }
    #endregion

    #region UI
    #region Text Box Getters
    public TextBox GetBuildStepTextBox()
    {
        return _uiControlGroupHelper.calibrateMotorControlGroup.buildStepTextBox;
    }
    public TextBox GetPowderStepTextBox()
    {
        return _uiControlGroupHelper.calibrateMotorControlGroup.powderStepTextBox;
    }
    public TextBox GetSweepStepTextBox()
    {
        return _uiControlGroupHelper.calibrateMotorControlGroup.sweepStepTextBox;
    }
    public TextBox GetBuildAbsMoveTextBox()
    {
        return _uiControlGroupHelper.calibrateMotorControlGroup.buildAbsMoveTextBox;
    }
    public TextBox GetPowderAbsMoveTextBox()
    {
        return _uiControlGroupHelper.calibrateMotorControlGroup.powderAbsMoveTextBox;
    }
    public TextBox GetSweepAbsMoveTextBox()
    {
        return _uiControlGroupHelper.calibrateMotorControlGroup.sweepAbsMoveTextBox;
    }
    public TextBox? GetMotorPositonTextBox(string motorNameLower)
    {
        return motorNameLower switch
        {
            "build" => _uiControlGroupHelper.calibrateMotorControlGroup.buildPositionTextBox,
            "powder" => _uiControlGroupHelper.calibrateMotorControlGroup.powderPositionTextBox,
            "sweep" => _uiControlGroupHelper.calibrateMotorControlGroup.sweepPositionTextBox,
            _ => null
        };
    }
    #endregion

    #region Move and Update UI Methods
    public async Task<int> UpdateMotorPositionTextBox(string motorName)
    {
        var motorNameToLower = motorName.ToLower();
        int res;
        double pos;
        var msg = "Updating motor position text box.";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.SUCCESS);
        // get position of requested motor
        (res, pos) = await _motorService.GetMotorPositionAsync(motorNameToLower);
        // check request result
        if (res == 0)
        {
            msg = $"Unable to handle getting {motorNameToLower} motor position.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
            return 0;
        }

        var textBox = GetMotorPositonTextBox(motorNameToLower);
        if (textBox != null)
        {
            textBox.Text = pos.ToString();
        }
        return 0;
    }
    public async Task MoveMotorAndUpdateUI(string motorName, TextBox textBox, bool moveIsAbs, bool moveUp)
    {
        int res;
        double targetPos;
        if (moveIsAbs)
        {
            (res, targetPos) = await MoveMotorAbsoluteProgram(motorName, textBox);
        }
        else
        {
            //(res, targetPos) = await MoveMotorRel(motorName, textBox, moveUp);
            (res, targetPos) = await MoveMotorRelativeProgram(motorName, textBox, moveUp); // wait to get to position occurs in program processor in motor service
        }
        await UpdateMotorPositionTextBox(motorName);
    }
    public async void MoveMotorAndUpdateUISelector(string motorName, TextBox textBox, bool moveIsAbs, bool moveInPositiveDirection, XamlRoot xamlRoot)
    {
        switch (motorName)
        {
            case "build":
                await MoveMotorAndUpdateUI(buildMotorName, textBox, moveIsAbs, moveInPositiveDirection);
                break;
            case "powder":
                await MoveMotorAndUpdateUI(powderMotorName, textBox, moveIsAbs, moveInPositiveDirection);
                break;
            case "sweep":
                await MoveMotorAndUpdateUI(sweepMotorName, textBox, moveIsAbs, moveInPositiveDirection);
                break;
            default:
                _ = PopupInfo.ShowContentDialog(xamlRoot, "Error", "Invalid motor name given.");
                return;
        }
    }
    public async Task<int> HomeMotorProgramAndUpdateUI(string motorName)
    {
        _uiControlGroupHelper.SelectMotor(motorName);
        await _motorService.HomeMotorProgram(motorName); // wait takes place in process commands in motor service
        await UpdateMotorPositionTextBox(motorName); // TODO: This should probably wait until the motor is done moving...
        return 1;
    }

    public async Task HomeAllMotorsAsync()
    {
        await _motorService.HomeAllMotors();
    }
    #endregion
    #endregion
}

