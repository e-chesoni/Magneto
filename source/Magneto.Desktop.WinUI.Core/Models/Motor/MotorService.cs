using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Magneto.Desktop.WinUI.Core.Contracts.Services;
using Magneto.Desktop.WinUI.Core.Models.BuildModels;
using Magneto.Desktop.WinUI.Core.Services;
using static Magneto.Desktop.WinUI.Core.Models.BuildModels.ActuationManager;

namespace Magneto.Desktop.WinUI.Core.Models.Motor;
public class MotorService
{
    private ActuationManager? _actuationManager;
    private StepperMotor? _powderMotor;
    private StepperMotor? _buildMotor;
    private StepperMotor? _sweepMotor;
    private bool _movingMotorToTarget = false;

    /// <summary>
    /// Initializes the dictionary mapping motor names to their corresponding StepperMotor objects.
    /// This map facilitates the retrieval of motor objects based on their names.
    /// </summary>
    private Dictionary<string, StepperMotor?>? _motorTextMap;


    public MotorService(ActuationManager am)
    {
        // Set up event handers to communicate with motor controller ports
        ConfigurePortEventHandlers();

        // Initialize motor set up for test page
        InitMotors(am);

        // Initialize motor map to simplify coordinated calls below
        // Make sure this happens AFTER motor setup
        InitializeMotorMap();
    }

    #region Initial Setup

    private void ConfigurePortEventHandlers()
    {
        var msg = "Requesting port access for motor service.";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.DEBUG);
        MagnetoSerialConsole.LogAvailablePorts();

        // Get motor configurations
        var buildMotorConfig = MagnetoConfig.GetMotorByName("build");
        var sweepMotorConfig = MagnetoConfig.GetMotorByName("sweep");

        // Get motor ports, ensuring that the motor configurations are not null
        var buildPort = buildMotorConfig?.COMPort;
        var sweepPort = sweepMotorConfig?.COMPort;

        // Register event handlers on page
        foreach (SerialPort port in MagnetoSerialConsole.GetAvailablePorts())
        {
            if (port.PortName.Equals(buildPort, StringComparison.OrdinalIgnoreCase))
            {
                MagnetoSerialConsole.AddEventHandler(port);
                msg = $"Requesting addition of event handler for port {port.PortName}";
                MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);
            }
            else if (port.PortName.Equals(sweepPort, StringComparison.OrdinalIgnoreCase))
            {
                MagnetoSerialConsole.AddEventHandler(port);
                msg = $"Requesting addition of event handler for port {port.PortName}";
                MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);
            }
        }
    }

    private async void InitMotors(ActuationManager am)
    {
        // Set up each motor individually using the passed-in parameters
        HandleMotorInit("powder", am.GetPowderMotor(), out _powderMotor);
        HandleMotorInit("build", am.GetBuildMotor(), out _buildMotor);
        HandleMotorInit("sweep", am.GetSweepMotor(), out _sweepMotor);

        // Since there's no _missionControl, you'll need to figure out how to get the BuildManager
        // if that's still necessary in this context.
        _actuationManager = am;

        // Optionally, get the positions of the motors after setting them up
        // GetMotorPositions(); // TODO: Fix this if needed
    }

    private void HandleMotorInit(string motorName, StepperMotor motor, out StepperMotor motorField)
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

    private void InitializeMotorMap()
    {
        _motorTextMap = new Dictionary<string, StepperMotor?>
            {
                { "build", _buildMotor },
                { "powder", _powderMotor },
                { "sweep", _sweepMotor }
            };
    }

    #endregion

    #region Getters

    public ActuationManager GetActuationManager()
    {
        return _actuationManager;
    }

    #endregion

}

