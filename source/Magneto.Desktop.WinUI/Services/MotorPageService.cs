using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Magneto.Desktop.WinUI.Core.Contracts.Services;
using Magneto.Desktop.WinUI.Core.Models.Motors;
using Magneto.Desktop.WinUI.Helpers;
using Magneto.Desktop.WinUI.Popups;
using Magneto.Desktop.WinUI.Core;
using MongoDB.Driver;

namespace Magneto.Desktop.WinUI;
public class MotorPageService
{
    private readonly IMotorService _motorService;
    private PrintUIControlGroupHelper _printUiControlGroupHelper { get; set; }
    public static readonly string buildMotorName =  "build";
    public static readonly string powderMotorName = "powder";
    public static readonly string sweepMotorName = "sweep";
    public MotorPageService(PrintUIControlGroupHelper printCtlGrpHelper)
    {
        _motorService = App.GetService<IMotorService>();
        _motorService.HandleStartUp();
        _printUiControlGroupHelper = new PrintUIControlGroupHelper(printCtlGrpHelper.calibrateMotorControlGroup);
    }

    #region Locks
    public void UnlockCalibrationPanel()
    {
        _printUiControlGroupHelper.EnableUIControlGroup(_printUiControlGroupHelper.GetCalibrationControlGroup());
    }
    public void LockCalibrationPanel()
    {
        _printUiControlGroupHelper.DisableUIControlGroup(_printUiControlGroupHelper.GetCalibrationControlGroup());
    }
    #endregion

    public bool CheckMotorStopFlag(string motorName)
    {
        return _motorService.CheckMotorStopFlag(motorName.ToLower());
    }
    public void EnableBuildMotor() =>_motorService.EnableBuildMotor();
    public void EnablePowderMotor() => _motorService.EnablePowderMotor();
    public void EnableSweepMotor() => _motorService.EnableBuildMotor();
    public void EnableMotors() =>_motorService.EnableMotors();

    #region Getters
    public StepperMotor GetBuildMotor()
    {
        return _motorService.GetBuildMotor();
    }
    public StepperMotor GetPowderMotor()
    {
        return _motorService.GetPowderMotor();
    }
    public StepperMotor GetSweepMotor()
    {
        return _motorService.GetSweepMotor();
    }
    public TextBox GetBuildPositionTextBox()
    {
        return _printUiControlGroupHelper.calibrateMotorControlGroup.buildPositionTextBox;
    }
    public TextBox GetPowderPositionTextBox()
    {
        return _printUiControlGroupHelper.calibrateMotorControlGroup.powderPositionTextBox;
    }
    public TextBox GetSweepPositionTextBox()
    {
        return _printUiControlGroupHelper.calibrateMotorControlGroup.sweepPositionTextBox;
    }
    public TextBox GetBuildStepTextBox()
    {
        return _printUiControlGroupHelper.calibrateMotorControlGroup.buildStepTextBox;
    }
    public TextBox GetPowderStepTextBox()
    {
        return _printUiControlGroupHelper.calibrateMotorControlGroup.powderStepTextBox;
    }
    public TextBox GetSweepStepTextBox()
    {
        return _printUiControlGroupHelper.calibrateMotorControlGroup.sweepStepTextBox;
    }
    public TextBox GetBuildAbsMoveTextBox()
    {
        return _printUiControlGroupHelper.calibrateMotorControlGroup.buildAbsMoveTextBox;
    }
    public TextBox GetPowderAbsMoveTextBox()
    {
        return _printUiControlGroupHelper.calibrateMotorControlGroup.powderAbsMoveTextBox;
    }
    public TextBox GetSweepAbsMoveTextBox()
    {
        return _printUiControlGroupHelper.calibrateMotorControlGroup.sweepAbsMoveTextBox;
    }
    #endregion

    #region Selectors
    public void SelectBuildMotor()
    {
        _printUiControlGroupHelper.SelectMotor(GetBuildMotor());
    }
    public void SelectPowderMotor()
    {
        _printUiControlGroupHelper.SelectMotor(GetPowderMotor());
    }
    public void SelectSweepMotor()
    {
        _printUiControlGroupHelper.SelectMotor(GetSweepMotor());
    }
    public void ChangeSelectButtonsBackground(Windows.UI.Color color)
    {
        _printUiControlGroupHelper.ChangeSelectButtonsBackground(color);
    }
    #endregion

    #region Motor Movement Methods
    public async Task<(int status, double targetPos)> MoveMotorAbs(string motorName, TextBox textBox)
    {
        var motorNameLower = motorName.ToLower();
        if (textBox == null || !double.TryParse(textBox.Text, out _))
        {
            var msg = $"invalid input in {motorName} text box.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
            return (0, 0);
        }
        else
        {
            var targetPos = double.Parse(textBox.Text);
            await _motorService.MoveMotorAbs(motorNameLower, targetPos);
            return (1, targetPos);
        }
    }
    public async Task<(int status, double targetPos)> MoveMotorRel(string motorName, TextBox textBox, bool moveUp)
    {
        var motorNameLower = motorName.ToLower();
        if (textBox == null || !double.TryParse(textBox.Text, out var _))
        {
            var msg = $"invalid input in {motorNameLower} text box.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
            return (0, 0);
        }
        else
        {
            // Convert distance to an absolute number to avoid confusing user
            var dist = Math.Abs(double.Parse(textBox.Text));
            // Update the text box with corrected distance
            textBox.Text = dist.ToString();
            // Add sign to distance based on moveUp boolean
            var distance = moveUp ? dist : -dist;
            MagnetoLogger.Log($"Moving motor distance of {distance}", LogFactoryLogLevel.LogLevel.SUCCESS);
            var currentPos = await _motorService.GetMotorPositionAsync(motorNameLower);
            var targetPos = currentPos + distance;
            await _motorService.MoveMotorRel(motorNameLower, distance);
            return (1, targetPos);
        }
    }
    public async Task<int> WaitUntilMotorHomedAsync(string motorName)
    {
        //await motor.WaitUntilAtTargetAsync(targetPos);
        await _motorService.WaitUntilMotorHomedAsync(motorName);
        return 1;
    }
    /// <summary>
    /// Homes sweep motor (moves right to min position 0), moves powder motor up 2x layer height, moves build motor down by layer height, then sweeps material onto build plate
    /// (moves sweep motor left to max sweep position)
    /// </summary>
    /// <param name="layerThickness"></param>
    /// <returns></returns>
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
    public async Task<int> LayerMove(double layerThickness, double amplifier)
    {
        var buildMotor = _motorService.GetBuildMotor();
        var powderMotor = _motorService.GetPowderMotor();
        var sweepMotor = _motorService.GetSweepMotor();
        var maxSweepPosition = _motorService.GetMaxSweepPosition();
        var clearance = 2;
        // 1. move build motor down for sweep
        await _motorService.MoveMotorRel(buildMotor, -clearance);
        // 2. home sweep motor
        await _motorService.HomeMotor(sweepMotor);
        // 3. move build motor back up to last mark height
        await _motorService.MoveMotorRel(buildMotor, clearance);
        // 4. move powder motor up by powder amp layer height (Prof. Tertuliano recommends powder motor moves 2-3x distance of build motor)
        await _motorService.MoveMotorRel(powderMotor, layerThickness);
        // 5. move build motor down by layer height
        await _motorService.MoveMotorRel(buildMotor, -layerThickness);
        // 6. apply material to build plate
        await _motorService.MoveMotorRel(sweepMotor, maxSweepPosition);
        // TEMPORARY SOLUTION: repeat last command to pad queue so we can use motors running check properly
        await _motorService.MoveMotorRel(sweepMotor, maxSweepPosition);
        return 1; // TODO: implement failure return
    }
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
    public async void StopBuildMotorAndUpdateTextBox()
    {
        var msg = $"stopping {buildMotorName} motor";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.SUCCESS);
        await _motorService.StopMotorAndClearQueue(buildMotorName);
        await UpdateMotorPositionTextBox(buildMotorName);
    }
    public async void StopPowderMotorAndUpdateTextBox()
    {
        var msg = $"stopping {powderMotorName} motor";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.SUCCESS);
        await _motorService.StopMotorAndClearQueue(powderMotorName);
        await UpdateMotorPositionTextBox(powderMotorName);
    }
    public async void StopSweepMotorAndUpdateTextBox()
    {
        var msg = $"stopping {sweepMotorName} motor";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.SUCCESS);
        await _motorService.StopMotorAndClearQueue(sweepMotorName);
        await UpdateMotorPositionTextBox(sweepMotorName);
    }
    // Keep as a reference; still seeing bugs when stop buttons are clicked
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
    public async Task<int> HomeMotorAndUpdateTextBox(string motorName)
    {
        _printUiControlGroupHelper.SelectMotor(motorName);
        await _motorService.HomeMotor(motorName);
        await WaitUntilMotorHomedAsync(motorName);
        await UpdateMotorPositionTextBox(motorName); // TODO: This should probably wait until the motor is done moving...
        return 1;
    }

    #endregion

    #region Movement Handlers
    public async void HandleGetAllPositions()
    {
        var msg = "Get position button clicked...";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);
        await HandleGetPosition(buildMotorName, GetBuildPositionTextBox(), false);
        await HandleGetPosition(powderMotorName, GetPowderPositionTextBox(), false);
        await HandleGetPosition(sweepMotorName, GetSweepPositionTextBox(), false);
    }
    public async Task HandleGetPosition(string motorName, TextBox textBox, bool selectMotor)
    {
        var msg = "Get position button clicked...";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);
        var motorNameToLower = motorName.ToLower();
        double? pos;
        // TODO: Move switch to motor service
        switch (motorNameToLower)
        {
            case "build":
                pos = await _motorService.GetBuildMotorPositionAsync(); 
                break;
            case "powder":
                pos = await _motorService.GetPowderMotorPositionAsync();
                break;
            case "sweep":
                pos = await _motorService.GetSweepMotorPositionAsync();
                break;
            default:
                msg = $"Invalid motor name. Could not get {motorNameToLower} motor position.";
                MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
                return;
        }
        if (textBox != null) // Full error checking in UITextHelper
        {
            UpdateUITextHelper.UpdateUIText(textBox, pos.ToString());
        }
        if (selectMotor)
        {
            _printUiControlGroupHelper.SelectMotor(motorNameToLower);
        }
    }
    public void HandleAbsMove(string motorName, TextBox textBox, XamlRoot xamlRoot)
    {
        var motorNameLower = motorName.ToLower();
        var msg = $"{motorNameLower} abs move button clicked.";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.SUCCESS);
        var moveIsAbs = true;
        var moveUp = true; // Does not matter what we put here; unused in absolute move
        MoveMotorAndUpdateUI(motorName, textBox, moveIsAbs, moveUp, xamlRoot);
    }

    public void HandleRelMove(string motorName, TextBox textBox, bool moveUp, XamlRoot xamlRoot)
    {
        var motorNameLower = motorName.ToLower();
        var msg = $"{motorNameLower} rel move button clicked.";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.SUCCESS);
        var moveIsAbs = false;
        MoveMotorAndUpdateUI(motorName, textBox, moveIsAbs, moveUp, xamlRoot);
    }
    #endregion

    #region Move and Update UI Method
    public TextBox? GetMotorPositonTextBox(string motorNameLower)
    {
        return motorNameLower switch
        {
            "build" => _printUiControlGroupHelper.calibrateMotorControlGroup.buildPositionTextBox,
            "powder" => _printUiControlGroupHelper.calibrateMotorControlGroup.powderPositionTextBox,
            "sweep" => _printUiControlGroupHelper.calibrateMotorControlGroup.sweepPositionTextBox,
            _ => null
        };
    }
    public async Task<int> UpdateMotorPositionTextBox(string motorName)
    {
        var motorNameLower = motorName.ToLower();
        double position;
        MagnetoLogger.Log("Updating motor position text box.", LogFactoryLogLevel.LogLevel.SUCCESS);

        try
        {
            // TODO: move switch to motor service
            switch (motorNameLower)
            {
                case "build":
                    position = await _motorService.GetBuildMotorPositionAsync();
                    break;
                case "powder":
                    position = await _motorService.GetPowderMotorPositionAsync();
                    break;
                case "sweep":
                    position = await _motorService.GetSweepMotorPositionAsync();
                    break;
                default:
                    MagnetoLogger.Log($"❌Invalid motor name: {motorName}", LogFactoryLogLevel.LogLevel.ERROR);
                    return -1;
            }

            var textBox = GetMotorPositonTextBox(motorNameLower);
            if (textBox != null)
            {
                textBox.Text = position.ToString();
            }
            return 0;
        }
        catch (Exception ex)
        {
            MagnetoLogger.Log($"❌Failed to get motor position: {ex.Message}", LogFactoryLogLevel.LogLevel.ERROR);
            return -1;
        }
    }
    public async void MoveMotorAndUpdateUI(string motorName, TextBox textBox, bool moveIsAbs, bool increment, XamlRoot xamlRoot)
    {
        switch (motorName)
        {
            case "build":
                await MoveBuildMotorAndUpdateUI(textBox, moveIsAbs, increment, xamlRoot);
                break;
            case "powder":
                await MovePowderMotorAndUpdateUI(textBox, moveIsAbs, increment, xamlRoot);
                break;
            case "sweep":
                await MoveSweepMotorAndUpdateUI(textBox, moveIsAbs, increment, xamlRoot);
                break;
            default:
                _ = PopupInfo.ShowContentDialog(xamlRoot, "Error", "Invalid motor name given.");
                return;
        }
    }
    public async Task MoveBuildMotorAndUpdateUI(TextBox textBox, bool moveIsAbs, bool increment, XamlRoot xamlRoot)
    {
        int res;
        double targetPos;
        if (moveIsAbs)
        {
            (res, targetPos) = await MoveMotorAbs(buildMotorName, textBox);
        }
        else
        {
            (res, targetPos) = await MoveMotorRel(buildMotorName, textBox, increment);
        }
        if (res == 1)
        {
            MagnetoLogger.Log($"Waiting for build motor to move {targetPos}", LogFactoryLogLevel.LogLevel.ERROR);
            await _motorService.WaitUntilBuildReachesTargetAsync(targetPos);
            await UpdateMotorPositionTextBox(buildMotorName);
        }
    }
    public async Task MovePowderMotorAndUpdateUI(TextBox textBox, bool moveIsAbs, bool increment, XamlRoot xamlRoot)
    {
        int res;
        double targetPos;
        if (moveIsAbs)
        {
            (res, targetPos) = await MoveMotorAbs(powderMotorName, textBox);
        }
        else
        {
            (res, targetPos) = await MoveMotorRel(powderMotorName, textBox, increment);
        }
        if (res == 1)
        {
            MagnetoLogger.Log($"Waiting for build motor to move {targetPos}", LogFactoryLogLevel.LogLevel.ERROR);
            await _motorService.WaitUntilPowderReachesTargetAsync(targetPos);
            await UpdateMotorPositionTextBox(powderMotorName);
        }
    }
    public async Task MoveSweepMotorAndUpdateUI(TextBox textBox, bool moveIsAbs, bool increment, XamlRoot xamlRoot)
    {
        int res;
        double targetPos;
        if (moveIsAbs)
        {
            (res, targetPos) = await MoveMotorAbs(sweepMotorName, textBox);
        }
        else
        {
            (res, targetPos) = await MoveMotorRel(sweepMotorName, textBox, increment);
        }
        if (res == 1)
        {
            MagnetoLogger.Log($"Waiting for build motor to move {targetPos}", LogFactoryLogLevel.LogLevel.ERROR);
            await _motorService.WaitUntilSweepReachesTargetAsync(targetPos);
            await UpdateMotorPositionTextBox(sweepMotorName);
        }
    }
    #endregion
}

