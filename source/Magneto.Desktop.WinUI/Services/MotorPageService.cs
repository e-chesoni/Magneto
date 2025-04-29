using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Magneto.Desktop.WinUI.Core.Contracts.Services;
using Magneto.Desktop.WinUI.Core.Models.Motors;
using Magneto.Desktop.WinUI.Popups;
using Magneto.Desktop.WinUI.Core;
using MongoDB.Driver;
using Microsoft.UI;
using Magneto.Desktop.WinUI.Models.UIControl;

namespace Magneto.Desktop.WinUI;
public class MotorPageService
{
    private readonly IMotorService _motorService;
    private UIControlGroupWrapper _uiControlGroupHelper { get; set; }
    public static readonly string buildMotorName =  "build";
    public static readonly string powderMotorName = "powder";
    public static readonly string sweepMotorName = "sweep";
    public MotorPageService(UIControlGroupWrapper printCtlGrpHelper)
    {
        _motorService = App.GetService<IMotorService>();
        _motorService.HandleStartUp();
        _uiControlGroupHelper = printCtlGrpHelper;
    }

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
        (res, pos) = await _motorService.HandleGetPositionAsync(motorNameToLower);
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
    public async void HandleGetAllPositions()
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
    public async Task<(int status, double targetPos)> MoveMotorAbs(string motorName, TextBox textBoxToRead)
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
            await _motorService.MoveMotorAbs(motorNameLower, targetPos);
            return (1, targetPos);
        }
    }
    public async Task<(int status, double targetPos)> MoveMotorRel(string motorName, TextBox textBoxToRead, bool moveUp)
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
            var dist = Math.Abs(double.Parse(textBoxToRead.Text));
            // Update the text box with corrected distance
            textBoxToRead.Text = dist.ToString();
            // Add sign to distance based on moveUp boolean
            var distance = moveUp ? dist : -dist;
            MagnetoLogger.Log($"Moving motor distance of {distance}", LogFactoryLogLevel.LogLevel.SUCCESS);
            var currentPos = await _motorService.GetMotorPositionAsync(motorNameLower);
            var targetPos = currentPos + distance;
            await _motorService.MoveMotorRel(motorNameLower, distance);
            return (1, targetPos);
        }
    }
    public async Task<(int status, double targetPos)> MoveMotorAbs(string motorName, double targetPos)
    {
        var motorNameLower = motorName.ToLower();
        await _motorService.MoveMotorAbs(motorNameLower, targetPos);
        return (1, targetPos);
    }
    public async Task<(int status, double targetPos)> MoveMotorRel(string motorName, double distance, bool moveUp)
    {
        var motorNameLower = motorName.ToLower();
        // Add sign to distance based on moveUp boolean
        distance = moveUp ? distance : -distance;
        MagnetoLogger.Log($"Moving motor distance of {distance}", LogFactoryLogLevel.LogLevel.SUCCESS);
        var currentPos = await _motorService.GetMotorPositionAsync(motorNameLower);
        var targetPos = currentPos + distance;
        await _motorService.MoveMotorRel(motorNameLower, distance);
        return (1, targetPos);
    }
    // TODO: remove old layer move after new one is vetted
    /*
    public async Task<int> LayerMoveOLD(double layerThickness)
    {
        var powderAmplifier = 2.5; // Quan requested we change this from 4-2.5 to conserve powder
        var lowerBuildForSweepDist = 2;

        if (_actuationManager != null)
        {
            // move build motor down for sweep
            await _actuationManager.AddCommand(GetControllerTypeHelper(buildMotor.GetMotorName()), buildMotor.GetAxis(), CommandType.RelativeMove, -lowerBuildForSweepDist);

            // home sweep motor
            await _actuationManager.AddCommand(GetControllerTypeHelper(sweepMotor.GetMotorName()), sweepMotor.GetAxis(), CommandType.AbsoluteMove, sweepMotor.GetHomePos());

            // move build motor back up to last mark height
            await _actuationManager.AddCommand(GetControllerTypeHelper(buildMotor.GetMotorName()), buildMotor.GetAxis(), CommandType.RelativeMove, lowerBuildForSweepDist);

            // move powder motor up by powder amp layer height (Prof. Tertuliano recommends powder motor moves 2-3x distance of build motor)
            await _actuationManager.AddCommand(GetControllerTypeHelper(powderMotor.GetMotorName()), powderMotor.GetAxis(), CommandType.RelativeMove, (powderAmplifier * layerThickness));

            // move build motor down by layer height
            await _actuationManager.AddCommand(GetControllerTypeHelper(buildMotor.GetMotorName()), buildMotor.GetAxis(), CommandType.RelativeMove, -layerThickness);

            // apply material to build plate
            await _actuationManager.AddCommand(GetControllerTypeHelper(sweepMotor.GetMotorName()), sweepMotor.GetAxis(), CommandType.AbsoluteMove, maxSweepPosition);

            // TEMPORARY SOLUTION: repeat last command to pad queue so we can use motors running check properly
            await _actuationManager.AddCommand(GetControllerTypeHelper(sweepMotor.GetMotorName()), sweepMotor.GetAxis(), CommandType.AbsoluteMove, maxSweepPosition); // TODO: change to wait for end command
        } else {
            var msg = $"Actuation manager is null.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
            return 0;
        }
        return 1;
    }
    */
    public async Task<int> ExecuteLayerMove(double layerThickness, double amplifier, XamlRoot xamlRoot)
    {
        MagnetoLogger.Log($"Executing layer move...", LogFactoryLogLevel.LogLevel.VERBOSE);
        var maxSweepPosition = _motorService.GetMaxSweepPosition();
        var clearance = 2;
        var thicknessTimesAmplifier = layerThickness * amplifier;

        // TODO: Finish updating execute layer move to use MoveMotorAndUpdateUI()
        // public async void MoveMotorAndUpdateUISelector(string motorName, double value, bool moveIsAbs, bool moveInPositiveDirection, XamlRoot xamlRoot)
        // all moves are relative
        var moveIsAbs = false;
        // 1. move build motor down for sweep
        //await _motorService.MoveMotorRel(buildMotor, -clearance);
        await MoveMotorAndUpdateUISelector(buildMotorName, clearance, moveIsAbs, false, xamlRoot); // false -> move down
        // 2. home sweep motor
        //await _motorService.HomeMotor(sweepMotor);
        await HomeMotorAndUpdateUI(sweepMotorName);
        // 3. move build motor back up to last mark height
        //await _motorService.MoveMotorRel(buildMotor, clearance);
        await MoveMotorAndUpdateUISelector(buildMotorName, clearance, moveIsAbs, true, xamlRoot); // true -> move up
        // 4. move powder motor up by amplifier distance (Oat recommends 2-3x distance of build motor)
        //await _motorService.MoveMotorRel(powderMotor, layerThickness);
        await MoveMotorAndUpdateUISelector(powderMotorName, thicknessTimesAmplifier, moveIsAbs, true, xamlRoot);
        // 5. move build motor down by layer height
        //await _motorService.MoveMotorRel(buildMotor, -layerThickness);
        await MoveMotorAndUpdateUISelector(buildMotorName, layerThickness, moveIsAbs, false, xamlRoot);
        // 6. apply material to build plate
        await MoveMotorAndUpdateUISelector(sweepMotorName, maxSweepPosition, true, true, xamlRoot); // absolute move in the positive direction
        // TEMPORARY SOLUTION: repeat last command to pad queue so we can use motors running check properly
        await MoveMotorAndUpdateUISelector(sweepMotorName, maxSweepPosition, true, true, xamlRoot);
        return 1; // TODO: implement failure return
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

    #region Stoppers
    public async void StopBuildMotorAndUpdateTextBox()
    {
        var msg = $"stopping {buildMotorName} motor";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.SUCCESS);
        await _motorService.StopMotorAndClearQueue(buildMotorName);
        await UpdateMotorPositionTextBox(buildMotorName);
        _uiControlGroupHelper.DisableMotorControls(_uiControlGroupHelper.GetCalibrationControlGroup(), buildMotorName);
    }
    public async void StopPowderMotorAndUpdateTextBox()
    {
        var msg = $"stopping {powderMotorName} motor";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.SUCCESS);
        await _motorService.StopMotorAndClearQueue(powderMotorName);
        await UpdateMotorPositionTextBox(powderMotorName);
        _uiControlGroupHelper.DisableMotorControls(_uiControlGroupHelper.GetCalibrationControlGroup(), powderMotorName);
    }
    public async void StopSweepMotorAndUpdateTextBox()
    {
        var msg = $"stopping {sweepMotorName} motor";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.SUCCESS);
        await _motorService.StopMotorAndClearQueue(sweepMotorName);
        await UpdateMotorPositionTextBox(sweepMotorName);
        _uiControlGroupHelper.DisableMotorControls(_uiControlGroupHelper.GetCalibrationControlGroup(), sweepMotorName);
    }
    // Keep as a reference; still seeing bugs when stop buttons are clicked
    /*
    public void StopMotorsWithFlag()
    {
        var buildConfig = MagnetoConfig.GetMotorByName("build");
        var sweepConfig = MagnetoConfig.GetMotorByName("sweep");
        MagnetoLogger.Log("✉️Writing to COM to stop", LogFactoryLogLevel.LogLevel.WARN);
        MagnetoSerialConsole.SerialWrite(buildConfig.COMPort, "1STP"); // build motor is on axis 1
        MagnetoSerialConsole.SerialWrite(buildConfig.COMPort, "2STP");
        MagnetoSerialConsole.SerialWrite(sweepConfig.COMPort, "1STP"); // sweep motor is on axis 1
        GetBuildMotor().STOP_MOVE_FLAG = true;
        GetPowderMotor().STOP_MOVE_FLAG = true;
        GetSweepMotor().STOP_MOVE_FLAG = true;
    }
    */
    #endregion

    #region Wait for move to complete
    public async Task<int> WaitUntilMotorHomedAsync(string motorName)
    {
        //await motor.WaitUntilAtTargetAsync(targetPos);
        await _motorService.WaitUntilMotorHomedAsync(motorName);
        return 1;
    }
    #endregion
    #endregion

    #region Enablers
    public void EnableBuildMotor()
    {
        _motorService.EnableBuildMotor();
        _uiControlGroupHelper.EnableMotorControls(_uiControlGroupHelper.GetCalibrationControlGroup(), buildMotorName);
    }
    public void EnablePowderMotor()
    {
        _motorService.EnablePowderMotor();
        _uiControlGroupHelper.EnableMotorControls(_uiControlGroupHelper.GetCalibrationControlGroup(), powderMotorName);
    }
    public void EnableSweepMotor()
    {
        _motorService.EnableBuildMotor();
        _uiControlGroupHelper.EnableMotorControls(_uiControlGroupHelper.GetCalibrationControlGroup(), sweepMotorName);
    }
    public void EnableMotors()
    {
        _motorService.EnableMotors();
    }
    #endregion

    #region Checkers
    public bool MotorsRunning()
    {
        // if queue is not empty, motors are running
        if (_motorService.MotorsRunning())
        {
            return true;
        }
        else
        {
            return false;
        }
    }
    public bool CheckMotorStopFlag(string motorName)
    {
        return _motorService.CheckMotorStopFlag(motorName.ToLower());
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
        (res, pos) = await _motorService.HandleGetPositionAsync(motorNameToLower);
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
    public async Task MoveMotorAndUpdateUI(string motorName, double value, bool moveIsAbs, bool moveUp)
    {
        int res;
        double targetPos;
        if (moveIsAbs)
        {
            (res, targetPos) = await MoveMotorAbs(motorName, value);
        }
        else
        {
            (res, targetPos) = await MoveMotorRel(motorName, value, moveUp);
        }
        if (res == 1)
        {
            MagnetoLogger.Log($"Waiting for {motorName} motor to move {targetPos}", LogFactoryLogLevel.LogLevel.ERROR);
            await _motorService.WaitUntilBuildReachesTargetAsync(targetPos);
            await UpdateMotorPositionTextBox(motorName);
        }
    }
    public async Task MoveMotorAndUpdateUI(string motorName, TextBox textBox, bool moveIsAbs, bool moveUp)
    {
        int res;
        double targetPos;
        if (moveIsAbs)
        {
            (res, targetPos) = await MoveMotorAbs(motorName, textBox);
        }
        else
        {
            (res, targetPos) = await MoveMotorRel(motorName, textBox, moveUp);
        }
        if (res == 1)
        {
            MagnetoLogger.Log($"Waiting for build motor to move {targetPos}", LogFactoryLogLevel.LogLevel.ERROR);
            await _motorService.WaitUntilBuildReachesTargetAsync(targetPos);
            await UpdateMotorPositionTextBox(motorName);
        }
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
    
    // TODO: not sure if this should be task of void...
    public async Task MoveMotorAndUpdateUISelector(string motorName, double value, bool moveIsAbs, bool moveInPositiveDirection, XamlRoot xamlRoot)
    {
        switch (motorName)
        {
            case "build":
                await MoveMotorAndUpdateUI(buildMotorName, value, moveIsAbs, moveInPositiveDirection);
                break;
            case "powder":
                await MoveMotorAndUpdateUI(powderMotorName, value, moveIsAbs, moveInPositiveDirection);
                break;
            case "sweep":
                await MoveMotorAndUpdateUI(sweepMotorName, value, moveIsAbs, moveInPositiveDirection);
                break;
            default:
                _ = PopupInfo.ShowContentDialog(xamlRoot, "Error", "Invalid motor name given.");
                return;
        }
    }
    public async Task<int> HomeMotorAndUpdateUI(string motorName)
    {
        _uiControlGroupHelper.SelectMotor(motorName);
        await _motorService.HomeMotor(motorName);
        await WaitUntilMotorHomedAsync(motorName);
        await UpdateMotorPositionTextBox(motorName); // TODO: This should probably wait until the motor is done moving...
        return 1;
    }
    #endregion
    #endregion
}

