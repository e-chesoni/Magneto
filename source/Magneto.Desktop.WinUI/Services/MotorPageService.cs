using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Magneto.Desktop.WinUI.Behaviors;
using Magneto.Desktop.WinUI.ViewModels;
using Magneto.Desktop.WinUI.Core.Models;
using Magneto.Desktop.WinUI.Core.Contracts.Services;
using Magneto.Desktop.WinUI.Core.Models.Motor;
using Magneto.Desktop.WinUI.Core.Services;
using Magneto.Desktop.WinUI.Popups;
using Magneto.Desktop.WinUI.Core.Models.BuildModels;
using Magneto.Desktop.WinUI.Core;
using System.IO.Ports;
using Magneto.Desktop.WinUI.Helpers;
using Microsoft.UI;
using static Magneto.Desktop.WinUI.Core.Models.BuildModels.BuildManager;

namespace Magneto.Desktop.WinUI.Core.Contracts.Services.Motor;
public class MotorPageService
{
    private MissionControl? _missionControl;
    private BuildManager? _bm;
    private StepperMotor? _powderMotor;
    private StepperMotor? _buildMotor;
    private StepperMotor? _sweepMotor;

    public MotorPageService(MissionControl missionControl)
    {
        _missionControl = missionControl;
    }

    /// <summary>
    /// Sets up test motors for powder, build, and sweep operations by retrieving configurations 
    /// and initializing the respective StepperMotor objects. Logs success or error for each motor setup.
    /// Assumes motor order in configuration corresponds to powder, build, and sweep.
    /// </summary>
    private async void SetUpTestMotors()
    {
        if (_missionControl == null)
        {
            MagnetoLogger.Log("MissionControl is null. Unable to set up motors.", LogFactoryLogLevel.LogLevel.ERROR);
            // TODO: Remove -> actual page (service does not have content)
            //await PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", "MissionControl is not Connected.");
            return;
        }

        SetUpMotor("powder", _missionControl.GetPowderMotor(), out _powderMotor);
        SetUpMotor("build", _missionControl.GetBuildMotor(), out _buildMotor);
        SetUpMotor("sweep", _missionControl.GetSweepMotor(), out _sweepMotor);

        _bm = _missionControl.GetBuildManger();

        //GetMotorPositions(); // TOOD: Fix--all positions are 0 on page load even if they're not...
    }

    /// <summary>
    /// Gets motor from mission control and assigns each motor to a private variable for easy access in the TestPrintPage class
    /// </summary>
    /// <param name="motorName">Motor name</param>
    /// <param name="motor">The actual stepper motor</param>
    /// <param name="motorField">Variable to assign motor to</param>
    private void SetUpMotor(string motorName, StepperMotor motor, out StepperMotor motorField)
    {
        if (motor != null)
        {
            motorField = motor;
            var msg = $"Found motor in config with name {motor.GetMotorName()}. Setting this to {motorName} motor in test.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);
        }
        else
        {
            motorField = null;
            MagnetoLogger.Log($"Unable to find {motorName} motor", LogFactoryLogLevel.LogLevel.ERROR);
        }
    }

    #region Getters

    public StepperMotor? GetBuildMotor() => _buildMotor;
    public StepperMotor? GetPowderMotor() => _powderMotor;
    public StepperMotor? GetSweepMotor() => _sweepMotor;
    public BuildManager? GetBuildManager() => _bm;

    /// <summary>
    /// Helper to get controller type given motor name
    /// </summary>
    /// <param name="motorName">Name of the motor for which to return the controller type</param>
    /// <returns>Controller type</returns>
    private ControllerType GetControllerTypeHelper(string motorName)
    {
        switch (motorName)
        {
            case "sweep":
                return ControllerType.SWEEP;
            default: return ControllerType.BUILD;
        }
    }

    /// <summary>
    /// Helper to get motor axis
    /// </summary>
    /// <param name="motorName">Name of the motor for which to return the axis</param>
    /// <returns>Motor axis if request is successful; -1 if request failed</returns>
    private int GetMotorAxisHelper(string motorName)
    {
        if (_bm != null)
        {
            switch (motorName)
            {
                case "build":
                    return _bm.GetBuildMotor().GetAxis();
                case "powder":
                    return _bm.GetPowderMotor().GetAxis();
                case "sweep":
                    return _bm.GetSweepMotor().GetAxis();
                default: return _bm.GetPowderMotor().GetAxis();
            }
        }
        else
        {
            var msg = "Unable to get motor axis.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
            return -1;
        }
    }

    /// <summary>
    /// Helper to get motor name, controller type, and motor axis given a motor
    /// </summary>
    /// <param name="motor"></param>
    /// <returns>Tuple containing motor name, controller type, and motor axis</returns>
    public (string motorName, ControllerType controllerType, int motorAxis) GetMotorDetailsHelper(StepperMotor motor)
    {
        // Get the name of the current motor
        var motorName = motor.GetMotorName();

        // Get the controller type using a helper method
        var controllerType = GetControllerTypeHelper(motorName);

        // Get the motor axis using a helper method
        var motorAxis = GetMotorAxisHelper(motorName);

        return (motorName, controllerType, motorAxis);
    }

    #endregion

    public async Task GetMotorPositions()
    {
        if (_buildMotor != null) await GetPositionHelper(_buildMotor);
        if (_powderMotor != null) await GetPositionHelper(_powderMotor);
        if (_sweepMotor != null) await GetPositionHelper(_sweepMotor);
    }

    private async Task GetPositionHelper(StepperMotor motor)
    {
        if (MagnetoSerialConsole.OpenSerialPort(motor.GetPortName()))
        {
            var pos = await motor.GetPosAsync();
            MagnetoLogger.Log($"Motor {motor.GetMotorName()} position: {pos}");
        }
        else
        {
            MagnetoLogger.Log("Failed to open port.", LogFactoryLogLevel.LogLevel.ERROR);
        }
    }

    public async Task HomeMotor(StepperMotor motor)
    {
        if (_bm != null)
        {
            var motorDetails = GetMotorDetailsHelper(motor);
            await _bm.AddCommand(motorDetails.controllerType, motorDetails.motorAxis, CommandType.AbsoluteMove, 0);
            // TODO: Fix type error
            //await GetMotorPositionAfterMove(motorDetails);
        }
    }

    private async Task GetMotorPositionAfterMove((ControllerType controllerType, int motorAxis) motorDetails)
    {
        try
        {
            var position = await _bm.AddCommand(motorDetails.controllerType, motorDetails.motorAxis, CommandType.PositionQuery, 0);
            MagnetoLogger.Log($"Motor position: {position}");
        }
        catch (Exception ex)
        {
            MagnetoLogger.Log($"Failed to get motor position: {ex.Message}", LogFactoryLogLevel.LogLevel.ERROR);
        }
    }


}
