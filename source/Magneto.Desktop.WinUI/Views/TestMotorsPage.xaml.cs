// Copyright (c) Microsoft Corporation and Contributors.
// Licensed under the MIT License.

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
using static Magneto.Desktop.WinUI.Core.Models.BuildModels.ActuationManager;
using static Magneto.Desktop.WinUI.Views.TestPrintPage;
using Magneto.Desktop.WinUI.Core.Contracts.Services.Motor;
using CommunityToolkit.WinUI.UI.Animations;
using CommunityToolkit.WinUI.UI.Controls.TextToolbarSymbols;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Magneto.Desktop.WinUI
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class TestMotorsPage : Page
    {

        #region Private Variables

        private StepperMotor? _powderMotor;

        private StepperMotor? _buildMotor;

        private StepperMotor? _sweepMotor;

        private StepperMotor? _currTestMotor;

        private ActuationManager? _am;

        private bool _powderMotorSelected = false;

        private bool _buildMotorSelected = false;

        private bool _sweepMotorSelected = false;

        private bool _movingMotorToTarget = false;

        #endregion

        /// <summary>
        /// Store "global" mission control on this page
        /// </summary>
        public MissionControl? MissionControl
        {
            get; set;
        }

        /// <summary>
        /// Page view model
        /// </summary>
        public TestMotorsViewModel ViewModel
        {
            get;
        }

        public TestMotorsPage()
        {
            ViewModel = App.GetService<TestMotorsViewModel>();
            this.InitializeComponent();

            var msg = "Landed on Test Print Page";
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

        #region Test Page Setup

        /// <summary>
        /// Sets up test motors for powder, build, and sweep operations by retrieving configurations 
        /// and initializing the respective StepperMotor objects. Logs success or error for each motor setup.
        /// Assumes motor order in configuration corresponds to powder, build, and sweep.
        /// </summary>
        private async void SetUpTestMotors()
        {
            if (MissionControl == null)
            {
                MagnetoLogger.Log("MissionControl is null. Unable to set up motors.", LogFactoryLogLevel.LogLevel.ERROR);
                await PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", "MissionControl is not Connected.");
                return;
            }

            SetUpMotor("powder", MissionControl.GetPowderMotor(), out _powderMotor);
            SetUpMotor("build", MissionControl.GetBuildMotor(), out _buildMotor);
            SetUpMotor("sweep", MissionControl.GetSweepMotor(), out _sweepMotor);

            _am = MissionControl.GetBuildManger();

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

        #endregion


        #region Navigation Methods

        /// <summary>
        /// Handle page startup tasks
        /// </summary>
        /// <param name="e"></param>
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            // Get mission control (passed over when navigating from previous page)
            base.OnNavigatedTo(e);

            // Set mission control after navigating to new page
            MissionControl = (MissionControl)e.Parameter;

            // Initialize motor set up for test page
            SetUpTestMotors();

            // Initialize motor map to simplify coordinated calls below
            // Make sure this happens AFTER motor setup
            InitializeMotorMap();

            // Get motor positions
            //TODO: FIX ME -- mixes up motor positions
            //GetMotorPositions();

            var msg = string.Format("TestPrintPage::OnNavigatedTo -- {0}", MissionControl.FriendlyMessage);
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.DEBUG);
        }

        #endregion


        #region Dictionary Methods

        /// <summary>
        /// Initializes the dictionary mapping motor names to their corresponding StepperMotor objects.
        /// This map facilitates the retrieval of motor objects based on their names.
        /// </summary>
        private Dictionary<string, StepperMotor?>? _motorToPosTextMap;

        private void InitializeMotorMap()
        {
            _motorToPosTextMap = new Dictionary<string, StepperMotor?>
            {
                { "build", _buildMotor },
                { "powder", _powderMotor },
                { "sweep", _sweepMotor }
            };
        }

        #endregion


        #region Selection Helper Methods

        /// <summary>
        /// Selects the given StepperMotor as the current test motor, updates the UI to reflect this selection,
        /// and toggles the selection status. Clears the position text box and updates the background color of motor selection buttons.
        /// </summary>
        /// <param name="motor">The StepperMotor to be selected as the current test motor.</param>
        /// <param name="positionTextBox">The TextBox associated with the motor, to be cleared upon selection.</param>
        /// <param name="thisMotorSelected">A reference to a boolean flag indicating the selection status of this motor.</param>
        private void SelectMotorHelper(StepperMotor motor, TextBox positionTextBox, ref bool thisMotorSelected)
        {
            _currTestMotor = motor;
            var msg = $"Setting current motor to {_currTestMotor.GetMotorName()} motor";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);

            // Update button backgrounds and selection flags
            SelectPowderMotorButton.Background = new SolidColorBrush(_powderMotor == motor ? Colors.Green : Colors.DimGray);
            _powderMotorSelected = _powderMotor == motor;

            SelectBuildMotorButton.Background = new SolidColorBrush(_buildMotor == motor ? Colors.Green : Colors.DimGray);
            _buildMotorSelected = _buildMotor == motor;

            SelectSweepMotorButton.Background = new SolidColorBrush(_sweepMotor == motor ? Colors.Green : Colors.DimGray);
            _sweepMotorSelected = _sweepMotor == motor;

            // Update the selection flag for this motor
            thisMotorSelected = !thisMotorSelected;
        }

        /// <summary>
        /// Wrapper for motor build motor selection code
        /// </summary>
        private void SelectBuildMotor()
        {
            if (_buildMotor != null)
            {
                SelectMotorHelper(_buildMotor, BuildPositionTextBox, ref _buildMotorSelected);
            }
            else
            {
                var msg = "Build Motor is null.";
                MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
            }
        }

        /// <summary>
        /// Wrapper for motor powder motor selection code
        /// </summary>
        private void SelectPowderMotor()
        {
            if (_powderMotor != null)
            {
                SelectMotorHelper(_powderMotor, PowderPositionTextBox, ref _powderMotorSelected);
            }
            else
            {
                var msg = "Powder Motor is null.";
                MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
            }
        }

        /// <summary>
        /// Wrapper for motor sweep motor selection code
        /// </summary>
        private void SelectSweepMotor()
        {
            if (_sweepMotor != null)
            {
                SelectMotorHelper(_sweepMotor, SweepPositionTextBox, ref _sweepMotorSelected);
            }
            else
            {
                var msg = "Sweep Motor is null.";
                MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
            }
        }

        #endregion


        #region Motor Detail Helper Methods

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
        /// Helper to get motor name, controller type, and motor axis given a motor
        /// </summary>
        /// <param name="motor"></param>
        /// <returns>Tuple containing motor name, controller type, and motor axis</returns>
        public (string motorName, ControllerType controllerType, int motorAxis) GetMotorDetailsHelper(StepperMotor motor)
        {
            return (motor.GetMotorName(), GetControllerTypeHelper(motor.GetMotorName()), motor.GetAxis());
        }

        #endregion


        #region Select Motor Button Methods

        /// <summary>
        /// Event handler for clicking the 'Select Build Motor' button.
        /// Selects the build motor as the current test motor and updates the UI accordingly.
        /// Logs an error if the build motor is null.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">Event data for the click event.</param>
        private void SelectBuildMotorButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            SelectBuildMotor();
        }

        /// <summary>
        /// Event handler for clicking the 'Select Powder Motor' button.
        /// Selects the powder motor as the current test motor and updates the UI accordingly.
        /// Logs an error if the powder motor is null.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">Event data for the click event.</param>
        private void SelectPowderMotorButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            SelectPowderMotor();
        }

        /// <summary>
        /// Event handler for clicking the 'Select Sweep Motor' button.
        /// Selects the sweep motor as the current test motor and updates the UI accordingly.
        /// Logs an error if the sweep motor is null.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">Event data for the click event.</param>
        private void SelectSweepMotorButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            SelectSweepMotor();
        }

        #endregion


        #region Motor Movement Methods

        /// <summary>
        /// Get motor details for a given motor to add movement request to queue
        /// </summary>
        /// <param name="motor">Motor to get details about</param>
        /// <returns>Tuple containing validity of request and motor details (if successful)</returns>
        private (bool isValid, MotorDetails? motorDetails) PrepareMotorOperation(StepperMotor motor)
        {
            if (motor == null)
            {
                MagnetoLogger.Log("Invalid motor request or motor is null.", LogFactoryLogLevel.LogLevel.ERROR);
                _ = PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", "No motor selected.");
                return (false, null);
            }

            if (!MagnetoSerialConsole.OpenSerialPort(motor.GetPortName()))
            {
                MagnetoLogger.Log("Failed to open port.", LogFactoryLogLevel.LogLevel.ERROR);
                _ = PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", "Failed to open serial port.");
                return (false, null);
            }

            // Get motor name from motor
            var motorName = motor.GetMotorName();

            // Get motor details based on motor name
            ControllerType controllerType = GetControllerTypeHelper(motorName);
            var motorAxis = motor.GetAxis();

            return (true, new MotorDetails(motorName, controllerType, motorAxis));
        }

        /// <summary>
        /// Move motors by adding move command to move motors to build manager
        /// </summary>
        /// <param name="motor">Currently selected motor</param>
        /// <param name="isAbsolute">Indicates whether to move motor to absolute position or move motor relative to current position</param>
        /// <param name="value">Position to move to/distance to move</param>
        /// <returns></returns>
        private async Task ExecuteMovementCommand(StepperMotor motor, bool isAbsolute, double value)
        {
            var msg = "";

            if (MissionControl == null)
            {
                msg = "Failed to access mission control.";
                await PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", $"Mission control offline.");
                MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
                return;
            }
            else
            {
                // Get details for this motor
                var operationResult = PrepareMotorOperation(motor);

                if (!operationResult.isValid || operationResult.motorDetails == null)
                {
                    msg = "Motor preparation failed or motor details not available.";
                    MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);

                    // TODO: Handle the error appropriately

                    return;
                }
                else
                {
                    // Update movement boolean
                    _movingMotorToTarget = true;

                    // Ternary used to get control type
                    CommandType commandType = isAbsolute ? CommandType.AbsoluteMove : CommandType.RelativeMove;

                    // Get controller type from motor details
                    ControllerType controllerType = operationResult.motorDetails.Value.ControllerType;

                    // Get motor axis from motor details
                    var motorAxis = operationResult.motorDetails.Value.MotorAxis;

                    // Ask for position to update text box below
                    _ = await _am.AddCommand(controllerType, motorAxis, commandType, value);

                    try
                    {
                        // Call AddCommand with CommandType.PositionQuery to get the motor's position
                        var position = await _am.AddCommand(controllerType, motorAxis, CommandType.PositionQuery, 0);

                        MagnetoLogger.Log($"Position of motor on axis {motorAxis} is {position}", LogFactoryLogLevel.LogLevel.SUCCESS);
                    }
                    catch (Exception ex)
                    {
                        MagnetoLogger.Log($"Failed to get motor position: {ex.Message}", LogFactoryLogLevel.LogLevel.ERROR);
                    }

                    

                    // Update movement boolean
                    _movingMotorToTarget = false;
                }
            }
        }

        // Move motor and execute move are separate to remove text box reading from execution of move

        private async void MoveMotor(StepperMotor motor, TextBox textBox, bool moveIsAbs)
        {
            // Exit if no motor is selected
            if (motor == null)
            {
                MagnetoLogger.Log("Invalid motor request or Current Test Motor is null.", LogFactoryLogLevel.LogLevel.ERROR);
                await PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", $"No motor selected.");
                return;
            }

            if (double.TryParse(textBox.Text, out _))
            {
                var distance = moveIsAbs ? double.Parse(textBox.Text) : double.Parse(textBox.Text);
                await ExecuteMovementCommand(motor, moveIsAbs, distance);
                // Update text box
                UpdateMotorPositionTextBox(motor);
            }
            else
            {
                await PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", $"\"{textBox.Text}\" is not a valid position. Please make sure you entered a number in the text box.");
                return;
            }
        }

        /// <summary>
        /// Move motor by incremental value obtained from increment text box
        /// </summary>
        /// <param name="motor">Currently selected motor</param>
        /// <param name="increment">Indicates whether move is incremental (positive direction/up) or decremental (down) (true = increment)</param>
        private async void IncrementMotor(StepperMotor motor, TextBox textBox, bool increment)
        {
            if (textBox == null || !double.TryParse(textBox.Text, out var dist))
            {
                _ = PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", "Invalid input in increment text box.");
                return;
            }

            // Update text box value to absolute value so user knows
            textBox.Text = Math.Abs(dist).ToString();

            // Execute move
            await ExecuteMovementCommand(motor, false, increment ? dist : -dist);

            // Update text box
            UpdateMotorPositionTextBox(motor);
        }

        #endregion


        #region Increment/Decrement Button Methods

        private void IncrBuild_Click(object sender, RoutedEventArgs e)
        {
            var inrement = true;
            if (_buildMotor != null)
            {
                IncrementMotor(_buildMotor, IncrBuildPositionTextBox, inrement);
            }
            SelectBuildMotor();
        }

        private void DecrBuild_Click(object sender, RoutedEventArgs e)
        {
            var inrement = false;
            if (_buildMotor != null)
            {
                IncrementMotor(_buildMotor, IncrBuildPositionTextBox, inrement);
            }
            SelectBuildMotor();
        }

        private void IncrPowder_Click(object sender, RoutedEventArgs e)
        {
            var inrement = true;
            if (_powderMotor != null)
            {
                IncrementMotor(_powderMotor, IncrPowderPositionTextBox, inrement);
            }
            SelectPowderMotor();
        }

        private void DecrPowder_Click(object sender, RoutedEventArgs e)
        {
            var inrement = false;
            if (_powderMotor != null)
            {
                IncrementMotor(_powderMotor, IncrPowderPositionTextBox, inrement);
            }
            SelectPowderMotor();
        }

        private void IncrSweep_Click(object sender, RoutedEventArgs e)
        {
            var inrement = true;
            if (_sweepMotor != null)
            {
                IncrementMotor(_sweepMotor, IncrSweepPositionTextBox, inrement);
            }
            SelectSweepMotor();
        }

        private void DecrSweep_Click(object sender, RoutedEventArgs e)
        {
            var inrement = false;
            if (_sweepMotor != null)
            {
                IncrementMotor(_sweepMotor, IncrSweepPositionTextBox, inrement);
            }
            SelectSweepMotor();
        }

        #endregion


        #region Homing Button Methods

        /// <summary>
        /// Homes the specified StepperMotor if the current test motor is not null and the serial port can be opened.
        /// Logs the action and any errors encountered during the process, such as failure to open the serial port
        /// or if the current test motor is null.
        /// </summary>
        /// <param name="motor">The StepperMotor to be homed.</param>
        private async Task HomeMotorHelperAsync(StepperMotor motor)
        {
            var msg = "Using helper to home motors...";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);

            if (_am != null)
            {

                var motorDetails = GetMotorDetailsHelper(motor);

                _ = _am.AddCommand(motorDetails.controllerType, motorDetails.motorAxis, CommandType.AbsoluteMove, motor.GetHomePos());

                // Call try catch block to send command to get position to motor
                // (Required to update text box)
                try
                {
                    // Call AddCommand with CommandType.PositionQuery to get the motor's position
                    double position = await _am.AddCommand(motorDetails.controllerType, motorDetails.motorAxis, CommandType.PositionQuery, 0);
                    MagnetoLogger.Log($"Position of motor on axis {motorDetails.motorAxis} is {position}", LogFactoryLogLevel.LogLevel.SUCCESS);
                }
                catch (Exception ex)
                {
                    MagnetoLogger.Log($"Failed to get motor position: {ex.Message}", LogFactoryLogLevel.LogLevel.ERROR);
                }
                UpdateMotorPositionTextBox(motor);
            }
            else
            {
                msg = "ActuationManager is null.";
                MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
                await PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", "Internal error. Try reloading the page.");
            }
        }

        // TODO: FIX ME -- currently you can only home one motor at a time. Trying to home all motors only homes one motor
        /// <summary>
        /// Event handler for the 'Home All Motors' button click.
        /// Initiates the homing process for all motors (build, powder, and sweep).
        /// Checks each motor for null before attempting to home and logs an error if any motor is null.
        /// This ensures safe operation and provides clear feedback in case a motor object is missing.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void HomeAllMotorsButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            var msg = "Homing all motors";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);

            if (_buildMotor != null)
            {
                _ = HomeMotorHelperAsync(_buildMotor);
            }
            else
            {
                MagnetoLogger.Log("Build Motor is null, cannot home motor.", LogFactoryLogLevel.LogLevel.ERROR);
            }

            if (_powderMotor != null)
            {
                _ = HomeMotorHelperAsync(_powderMotor);
            }
            else
            {
                MagnetoLogger.Log("Powder Motor is null, cannot home motor.", LogFactoryLogLevel.LogLevel.ERROR);
            }

            if (_sweepMotor != null)
            {
                _ = HomeMotorHelperAsync(_sweepMotor);
            }
            else
            {
                MagnetoLogger.Log("Sweep Motor is null, cannot home motor.", LogFactoryLogLevel.LogLevel.ERROR);
            }
        }

        #endregion


        #region Get Position Button Methods

        /// <summary>
        /// Event handler for the 'Get Build Position' button click.
        /// Retrieves and displays the position of the build motor.
        /// Checks if the build motor is not null before attempting to get its position.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">Event data for the click event.</param>
        private async void GetBuildPositionButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            var msg = "GetBuildPositionButton_Click Clicked...";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);

            if (_buildMotor != null)
            {
                if (_buildMotor != null)
                {
                    var motorDetails = GetMotorDetailsHelper(_buildMotor);
                    //double pos = await _am.AddCommand(motorDetails.controllerType, motorDetails.motorAxis, CommandType.PositionQuery, 0);
                    var pos = await _buildMotor.GetPosAsync(); // TODO: figure out why AddCommand returns 0...
                    if (BuildPositionTextBox != null) // Full error checking in UITextHelper
                    {
                        UpdateUITextHelper.UpdateUIText(BuildPositionTextBox, pos.ToString());
                    }
                    SelectBuildMotor();
                }
            }
            else
            {
                MagnetoLogger.Log("Build Motor is null, cannot get position.", LogFactoryLogLevel.LogLevel.ERROR);
            }
        }

        /// <summary>
        /// Event handler for the 'Get Powder Position' button click.
        /// Retrieves and displays the position of the powder motor.
        /// Checks if the powder motor is not null before attempting to get its position.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">Event data for the click event.</param>
        private async void GetPowderPositionButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            var msg = "GetPowderPositionButton_Click Clicked...";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);

            if (_powderMotor != null)
            {
                var motorDetails = GetMotorDetailsHelper(_powderMotor);
                //var pos = await _am.AddCommand(motorDetails.controllerType, motorDetails.motorAxis, CommandType.PositionQuery, 0);
                var pos = await _powderMotor.GetPosAsync(); // TODO: figure out why AddCommand returns 0...
                if (PowderPositionTextBox != null) // Full error checking in UITextHelper
                {
                    UpdateUITextHelper.UpdateUIText(PowderPositionTextBox, pos.ToString());
                }
                SelectPowderMotor();
            }
            else
            {
                MagnetoLogger.Log("Powder Motor is null, cannot get position.", LogFactoryLogLevel.LogLevel.ERROR);
            }
        }

        /// <summary>
        /// Event handler for the 'Get Sweep Position' button click.
        /// Retrieves and displays the position of the sweep motor.
        /// Checks if the sweep motor is not null before attempting to get its position.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">Event data for the click event.</param>
        private async void GetSweepPositionButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
        {
            var msg = "GetSweepPositionButton_Click Clicked...";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);

            if (_sweepMotor != null)
            {
                if (_sweepMotor != null)
                {
                    var motorDetails = GetMotorDetailsHelper(_sweepMotor);
                    //var pos = await _am.AddCommand(motorDetails.controllerType, motorDetails.motorAxis, CommandType.PositionQuery, 0);
                    var pos = await _sweepMotor.GetPosAsync(); // TODO: figure out why AddCommand returns 0...
                    if (SweepPositionTextBox != null) // Full error checking in UITextHelper
                    {
                        UpdateUITextHelper.UpdateUIText(SweepPositionTextBox, pos.ToString());
                    }
                    SelectSweepMotor();
                }
            }
            else
            {
                MagnetoLogger.Log("Sweep Motor is null, cannot get position.", LogFactoryLogLevel.LogLevel.ERROR);
            }
        }

        #endregion


        #region UI Update Methods

        /// <summary>
        /// Retrieves the corresponding TextBox control for a given motor name.
        /// </summary>
        /// <param name="motorName">The name of the motor for which the corresponding TextBox is needed.</param>
        /// <returns>The corresponding TextBox if found, otherwise null.</returns>
        private TextBox? GetCorrespondingTextBox(string motorName)
        {
            return motorName switch
            {
                "build" => BuildPositionTextBox,
                "powder" => PowderPositionTextBox,
                "sweep" => SweepPositionTextBox,
                _ => null
            };
        }

        /// <summary>
        /// Updates the text box associated with a given motor name with the motor's current position.
        /// </summary>
        /// <param name="motorName">Name of the motor whose position needs to be updated in the UI.</param>
        /// <param name="motor">The motor object whose position is to be retrieved and displayed.</param>
        private void UpdateMotorPositionTextBox(StepperMotor motor)
        {
            MagnetoLogger.Log("Updating motor position text box.", LogFactoryLogLevel.LogLevel.SUCCESS);
            var motorName = motor.GetMotorName();
            var textBox = GetCorrespondingTextBox(motorName);
            if (textBox != null)
                textBox.Text = motor.GetCurrentPos().ToString();
        }

        /// <summary>
        /// Updates the background color of the motor control button based on the current state of motor movement.
        /// Sets the color to green if the motor is moving, and to dim gray once the movement is completed.
        /// </summary>
        /// <param name="isAbsolute">Determines which button's background to update, based on whether the movement is absolute or relative.</param>
        /*
        private void UpdateButtonBackground(bool isAbsolute)
        {
            var button = isAbsolute ? MoveMotorAbsButton : MoveMotorRelativeButton;
            button.Background = _movingMotorToTarget ? new SolidColorBrush(Colors.Green) : new SolidColorBrush(Colors.DimGray);
        }
        */
        #endregion

        private void AbsMoveBuildButton_Click(object sender, RoutedEventArgs e)
        {
            var msg = $"Build abs move button clicked.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.SUCCESS);
            if (_buildMotor != null)
            {
                MoveMotor(_buildMotor, BuildAbsMoveTextBox, true);
            }
            else
            {
                msg = "Cannot move build motor. Motor is null.";
                MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
            }
        }

        private void AbsMovePowderButton_Click(object sender, RoutedEventArgs e)
        {
            var msg = $"Powder abs move button clicked.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.SUCCESS);
            if (_powderMotor != null)
            {
                MoveMotor(_powderMotor, PowderAbsMoveTextBox, true);
            }
            else
            {
                msg = "Cannot move powder motor. Motor is null.";
                MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
            }
        }

        private void AbsMoveSweepButton_Click(object sender, RoutedEventArgs e)
        {
            var msg = $"Sweep abs move button clicked.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.SUCCESS);
            if (_sweepMotor != null)
            {
                MoveMotor(_sweepMotor, SweepAbsMoveTextBox, true);
            }
            else
            {
                msg = "Cannot move sweep motor. Motor is null.";
                MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
            }
        }

        private void HomeBuildMotorButton_Click(object sender, RoutedEventArgs e)
        {
            MagnetoLogger.Log("Homing Motor.", LogFactoryLogLevel.LogLevel.VERBOSE);

            if (_buildMotor != null)
            {
                _ = HomeMotorHelperAsync(_buildMotor);
            }
            else
            {
                MagnetoLogger.Log("Cannot home build motor: motor value is null.", LogFactoryLogLevel.LogLevel.ERROR);
            }
        }

        private void HomePowderMotorButton_Click(object sender, RoutedEventArgs e)
        {
            if (_powderMotor != null)
            {
                _ = HomeMotorHelperAsync(_powderMotor);
            }
            else
            {
                MagnetoLogger.Log("Cannot home powder motor: motor value is null.", LogFactoryLogLevel.LogLevel.ERROR);
            }
        }

        private void HomeSweepMotorButton_Click(object sender, RoutedEventArgs e)
        {
            if (_sweepMotor != null)
            {
                _ = HomeMotorHelperAsync(_sweepMotor);
            }
            else
            {
                MagnetoLogger.Log("Cannot home sweep motor: motor value is null.", LogFactoryLogLevel.LogLevel.ERROR);
            }
        }
    }
}
