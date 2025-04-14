using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Magneto.Desktop.WinUI.Core.Contracts.Services;
using Magneto.Desktop.WinUI.Core.Models.Print;
using Magneto.Desktop.WinUI.Core.Models.Motors;
using static Magneto.Desktop.WinUI.Core.Models.Print.CommandQueueManager;
using Microsoft.UI.Xaml.Controls;
using Magneto.Desktop.WinUI.Helpers;
using Magneto.Desktop.WinUI.Popups;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;
using Magneto.Desktop.WinUI.Core;
using CommunityToolkit.WinUI.UI.Controls.TextToolbarSymbols;
using Magneto.Desktop.WinUI.Models.UIControl;
using Magneto.Desktop.WinUI.Core.Services;

namespace Magneto.Desktop.WinUI;
public class MotorPageService
{
    private readonly IMotorService _motorService;
    public PrintUIControlGroupHelper printUiControlGroupHelper { get; set; }

    public MotorPageService(CommandQueueManager am, PrintUIControlGroupHelper printCtlGrpHelper)
    {
        _motorService = App.GetService<IMotorService>();
        _motorService.HandleStartUp();
        printUiControlGroupHelper = new PrintUIControlGroupHelper(printCtlGrpHelper.calibrateMotorControlGroup, printCtlGrpHelper.printMotorControlGroup);
    }

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
        return printUiControlGroupHelper.calibrateMotorControlGroup.buildPositionTextBox;
    }
    public TextBox GetPowderPositionTextBox()
    {
        return printUiControlGroupHelper.calibrateMotorControlGroup.powderPositionTextBox;
    }
    public TextBox GetSweepPositionTextBox()
    {
        return printUiControlGroupHelper.calibrateMotorControlGroup.sweepPositionTextBox;
    }
    public TextBox GetBuildStepTextBox()
    {
        return printUiControlGroupHelper.calibrateMotorControlGroup.buildStepTextBox;
    }
    public TextBox GetPowderStepTextBox()
    {
        return printUiControlGroupHelper.calibrateMotorControlGroup.powderStepTextBox;
    }
    public TextBox GetSweepStepTextBox()
    {
        return printUiControlGroupHelper.calibrateMotorControlGroup.sweepStepTextBox;
    }
    public TextBox GetBuildAbsMoveTextBox()
    {
        return printUiControlGroupHelper.calibrateMotorControlGroup.buildAbsMoveTextBox;
    }
    public TextBox GetPowderAbsMoveTextBox()
    {
        return printUiControlGroupHelper.calibrateMotorControlGroup.powderAbsMoveTextBox;
    }
    public TextBox GetSweepAbsMoveTextBox()
    {
        return printUiControlGroupHelper.calibrateMotorControlGroup.sweepAbsMoveTextBox;
    }
    #endregion

    #region Motor Movement Methods
    public async Task<(int status, double targetPos)> MoveMotorAbs(StepperMotor motor, TextBox textBox)
    {
        if (textBox == null || !double.TryParse(textBox.Text, out var value))
        {
            var msg = $"invalid input in {motor.GetMotorName} text box.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
            return (0,0);
        }
        else
        {
            var targetPos = double.Parse(textBox.Text);
            await _motorService.MoveMotorAbs(motor, targetPos);
            return (1, targetPos);
        }
    }
    public async Task<(int status, double targetPos)> MoveMotorRel(StepperMotor motor, TextBox textBox, bool moveUp)
    {
        if (textBox == null || !double.TryParse(textBox.Text, out var value))
        {
            var msg = $"invalid input in {motor.GetMotorName} text box: {textBox.Text}";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
            return (0,0);
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

            var currentPos = await _motorService.GetMotorPositionAsync(motor);
            var targetPos = currentPos + distance;

            await _motorService.MoveMotorRel(motor, distance);
            return (1, targetPos);
        }
    }
    public async Task<int> WaitUntilAtTargetAsync(StepperMotor motor, double targetPos)
    {
        //await motor.WaitUntilAtTargetAsync(targetPos);
        await _motorService.WaitUntilAtTargetAsync(motor, targetPos);
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
        var motor = _motorService.GetBuildMotor();
        var msg = $"stopping {motor.GetMotorName()} motor";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.SUCCESS);
        await _motorService.StopMotorAndClearQueue(motor);
        await UpdateMotorPositionTextBox(motor);
    }
    public async void StopPowderMotorAndUpdateTextBox()
    {
        var motor = _motorService.GetPowderMotor();
        var msg = $"stopping {motor.GetMotorName()} motor";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.SUCCESS);
        await _motorService.StopMotorAndClearQueue(motor);
        await UpdateMotorPositionTextBox(motor);
    }
    public async void StopSweepMotorAndUpdateTextBox()
    {
        var motor = _motorService.GetSweepMotor();
        var msg = $"stopping {motor.GetMotorName()} motor";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.SUCCESS);
        await _motorService.StopMotorAndClearQueue(motor);
        await UpdateMotorPositionTextBox(motor);
    }

    public async void StopAllMotorsAndClearQueue()
    {
        await _motorService.StopMotorAndClearQueue(_motorService.GetBuildMotor());
        await _motorService.StopMotorAndClearQueue(_motorService.GetPowderMotor());
        await _motorService.StopMotorAndClearQueue(_motorService.GetSweepMotor());
    }
    public async Task<int> HomeMotorAndUpdateTextBox(StepperMotor motor)
    {
        printUiControlGroupHelper.SelectMotor(motor);
        await _motorService.HomeMotor(motor);
        await WaitUntilAtTargetAsync(motor, motor.GetHomePos());
        await UpdateMotorPositionTextBox(motor); // TODO: This should probably wait until the motor is done moving...
        return 1;
    }

    #endregion

    #region Movement Handlers

    public async void HandleGetPosition(StepperMotor motor, TextBox textBox)
    {
        var msg = "Get position button clicked...";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);
        if (motor != null)
        {
            if (motor != null)
            {
                var pos = await _motorService.GetMotorPositionAsync(motor);
                if (textBox != null) // Full error checking in UITextHelper
                {
                    UpdateUITextHelper.UpdateUIText(textBox, pos.ToString());
                }
                printUiControlGroupHelper.SelectMotor(motor);
            }
        }
        else
        {
            MagnetoLogger.Log("Motor is null, cannot get position.", LogFactoryLogLevel.LogLevel.ERROR);
        }
    }

    public void HandleAbsMove(StepperMotor motor, TextBox textBox, XamlRoot xamlRoot)
    {
        var msg = $"{motor.GetMotorName()} abs move button clicked.";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.SUCCESS);
        if (motor != null)
        {
            var moveIsAbs = true;
            var moveUp = true; // Does not matter what we put here; unused in absolute move
            MoveMotorAndUpdateUI(motor, textBox, moveIsAbs, moveUp, xamlRoot);
        }
        else
        {
            msg = $"Cannot execute relative move on motor. Motor is null.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
        }
    }

    public void HandleRelMove(StepperMotor motor, TextBox textBox, bool moveUp, XamlRoot xamlRoot)
    {
        var msg = $"{motor.GetMotorName()} rel move button clicked.";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.SUCCESS);
        if (motor != null)
        {
            var moveIsAbs = false;
            MoveMotorAndUpdateUI(motor, textBox, moveIsAbs, moveUp, xamlRoot);
        }
        else
        {
            msg = $"Cannot execute relative move on motor. Motor is null.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
        }
    }
    /*
    public void HandleHomeMotorAndUpdateTextBox(StepperMotor motor, TextBox positionTextBox)
    {
        MagnetoLogger.Log("Homing Motor.", LogFactoryLogLevel.LogLevel.VERBOSE);

        if (motor != null)
        {
            _ = HomeMotor(motor);
            printUiControlGroupHelper.SelectMotor(motor);
        }
        else
        {
            MagnetoLogger.Log($"Cannot home motor: motor value is null.", LogFactoryLogLevel.LogLevel.ERROR);
        }
    }
    public void HandleHomeMotor(StepperMotor motor)
    {
        MagnetoLogger.Log("Homing Motor.", LogFactoryLogLevel.LogLevel.VERBOSE);

        if (motor != null)
        {
            _ = HomeMotor(motor);
            printUiControlGroupHelper.SelectMotor(motor);
        }
        else
        {
            MagnetoLogger.Log($"Cannot home {motor.GetMotorName()} motor: motor value is null.", LogFactoryLogLevel.LogLevel.ERROR);
        }
    }
    */
    #endregion

    #region Move and Update UI Method
    public TextBox? GetMotorPositonTextBox(StepperMotor motor)
    {
        return motor.GetMotorName() switch
        {
            "build" => printUiControlGroupHelper.calibrateMotorControlGroup.buildPositionTextBox,
            "powder" => printUiControlGroupHelper.calibrateMotorControlGroup.powderPositionTextBox,
            "sweep" => printUiControlGroupHelper.calibrateMotorControlGroup.sweepPositionTextBox,
            _ => null
        };
    }

    /// <summary>
    /// Updates the text box associated with a given motor name with the motor's current position.
    /// </summary>
    /// <param name="motorName">Name of the motor whose position needs to be updated in the UI.</param>
    /// <param name="motor">The motor object whose position is to be retrieved and displayed.</param>
    public async Task<int> UpdateMotorPositionTextBox(StepperMotor motor)
    {
        MagnetoLogger.Log("Updating motor position text box.", LogFactoryLogLevel.LogLevel.SUCCESS);
        try
        {
            var position = await _motorService.GetMotorPositionAsync(motor);
            var textBox = GetMotorPositonTextBox(motor);
            if (textBox != null)
                textBox.Text = position.ToString();
            return 0;
        }
        catch (Exception ex)
        {
            MagnetoLogger.Log($"❌Failed to get motor position: {ex.Message}", LogFactoryLogLevel.LogLevel.ERROR);
            return -1;
        }
    }

    public async void MoveMotorAndUpdateUI(StepperMotor motor, TextBox textBox, bool moveIsAbs, bool increment, XamlRoot xamlRoot)
    {
        int res;
        double targetPos;
        if (motor == null)
        {
            _ = PopupInfo.ShowContentDialog(xamlRoot, "Error", "Failed to select motor. Motor is null.");
            return;
        }
        printUiControlGroupHelper.SelectMotor(motor);
        if (moveIsAbs)
        {
            (res, targetPos) = await MoveMotorAbs(motor, textBox);
        }
        else
        {
            (res, targetPos) = await MoveMotorRel(motor, textBox, increment);
        }
        if (res == 1)
        {
            // 🔒 Wait until motor reaches final position
            await WaitUntilAtTargetAsync(motor, targetPos); // TODO: go through _motorService
            await UpdateMotorPositionTextBox(motor); // TODO: TEST--may cause issues
        }
        else
        {
            _ = PopupInfo.ShowContentDialog(xamlRoot, "Error", "Failed to send command to motor.");
        }
    }
    #endregion
}

