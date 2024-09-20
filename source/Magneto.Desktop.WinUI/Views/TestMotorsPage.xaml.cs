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

        /// <summary>
        /// Initializes the dictionary mapping motor names to their corresponding StepperMotor objects.
        /// This map facilitates the retrieval of motor objects based on their names.
        /// </summary>
        private Dictionary<string, StepperMotor?>? _motorToPosTextMap;

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

        #region Initialization Methods

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

            _am = MissionControl.GetActuationManger();

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

        
        // TODO: Move to Motor service
        #region Motor Movement Methods

        private async Task<int> MoveMotorAbs(StepperMotor motor, TextBox textBox)
        {
            if (textBox == null || !double.TryParse(textBox.Text, out var value))
            {
                var msg = $"invalid input in {motor.GetMotorName} text box: {textBox.Text}";
                MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
                return 0;
            }
            else
            {
                var dist = double.Parse(textBox.Text);
                await _am.AddCommand(GetControllerTypeHelper(motor.GetMotorName()), motor.GetAxis(), CommandType.AbsoluteMove, dist);
                return 1;
            }
        }

        private async Task<int> MoveMotorRel(StepperMotor motor, TextBox textBox, bool moveUp)
        {
            if (textBox == null || !double.TryParse(textBox.Text, out var value))
            {
                var msg = $"invalid input in {motor.GetMotorName} text box: {textBox.Text}";
                MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
                return 0;
            }
            else
            {
                // Convert distance to an absolute number to avoid confusing user
                var dist = Math.Abs(double.Parse(textBox.Text));

                // Update the text box with corrected distance
                textBox.Text = dist.ToString();

                // Check the direction we're moving
                dist = moveUp ? dist : -dist;

                // Move motor
                await _am.AddCommand(GetControllerTypeHelper(motor.GetMotorName()), motor.GetAxis(), CommandType.RelativeMove, dist);

                // NOTE: when called, you must await the return to get the integer value
                // Otherwise returns some weird string
                return 1;
            }
        }

        private async Task<int> HomeMotor(StepperMotor motor)
        {
            await _am.AddCommand(GetControllerTypeHelper(motor.GetMotorName()), motor.GetAxis(), CommandType.AbsoluteMove, motor.GetHomePos());
            UpdateMotorPositionTextBox(motor);
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
                    //var motorDetails = GetMotorDetailsHelper(motor);
                    var pos = await motor.GetPosAsync(); // TODO: figure out why AddCommand returns 0...
                    if (textBox != null) // Full error checking in UITextHelper
                    {
                        UpdateUITextHelper.UpdateUIText(textBox, pos.ToString());
                    }
                    SelectMotorHelper(motor);
                }
            }
            else
            {
                MagnetoLogger.Log($"{motor.GetPortName()} is null, cannot get position.", LogFactoryLogLevel.LogLevel.ERROR);
            }
        }

        private void HandleAbsMove(StepperMotor motor, TextBox textBox)
        {
            var msg = $"{motor.GetMotorName()} abs move button clicked.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.SUCCESS);
            if (motor != null)
            {
                var moveIsAbs = true;
                var moveUp = true; // Does not matter what we put here; unused in absolute move
                MoveMotorAndUpdateUI(motor, textBox, moveIsAbs, moveUp);
            }
            else
            {
                msg = "Cannot move build motor. Motor is null.";
                MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
            }
        }

        private void HandleRelMove(StepperMotor motor, TextBox textBox, bool moveUp)
        {
            var moveIsAbs = false;
            if (motor != null)
            {
                MoveMotorAndUpdateUI(motor, textBox, moveIsAbs, moveUp);
            }
        }

        private void HandleHomeMotor(StepperMotor motor, TextBox positionTextBox)
        {
            MagnetoLogger.Log("Homing Motor.", LogFactoryLogLevel.LogLevel.VERBOSE);

            if (motor != null)
            {
                _ = HomeMotor(motor);
                SelectMotorHelper(motor);
            }
            else
            {
                MagnetoLogger.Log($"Cannot home {motor.GetMotorName()} motor: motor value is null.", LogFactoryLogLevel.LogLevel.ERROR);
            }
        }

        #endregion

        #region Select Motor Helper Methods

        /// <summary>
        /// Selects the given StepperMotor as the current test motor, updates the UI to reflect this selection,
        /// and toggles the selection status. Clears the position text box and updates the background color of motor selection buttons.
        /// </summary>
        /// <param name="motor">The StepperMotor to be selected as the current test motor.</param>
        /// <param name="positionTextBox">The TextBox associated with the motor, to be cleared upon selection.</param>
        /// <param name="thisMotorSelected">A reference to a boolean flag indicating the selection status of this motor.</param>
        private void SelectMotorUIHelper(StepperMotor motor, TextBox positionTextBox, ref bool thisMotorSelected)
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
                SelectMotorUIHelper(_buildMotor, BuildPositionTextBox, ref _buildMotorSelected);
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
                SelectMotorUIHelper(_powderMotor, PowderPositionTextBox, ref _powderMotorSelected);
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
                SelectMotorUIHelper(_sweepMotor, SweepPositionTextBox, ref _sweepMotorSelected);
            }
            else
            {
                var msg = "Sweep Motor is null.";
                MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
            }
        }

        private void SelectMotorHelper(StepperMotor motor)
        {
            switch (motor.GetMotorName())
            {
                case "build":
                    SelectBuildMotor();
                    break;
                case "powder":
                    SelectPowderMotor();
                    break;
                case "sweep":
                    SelectSweepMotor();
                    break;
                default:
                    break;
            }
        }

        #endregion


        #region Move and Update UI Method

        /// <summary>
        /// Retrieves the corresponding TextBox control for a given motor name.
        /// </summary>
        /// <param name="motorName">The name of the motor for which the corresponding TextBox is needed.</param>
        /// <returns>The corresponding TextBox if found, otherwise null.</returns>
        private TextBox? GetMotorPositonTextBox(StepperMotor motor)
        {
            return motor.GetMotorName() switch
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
        private async void UpdateMotorPositionTextBox(StepperMotor motor)
        {
            MagnetoLogger.Log("Updating motor position text box.", LogFactoryLogLevel.LogLevel.SUCCESS);
            // Call position add command first so we can update motor position in UI
            // TODO: WARNING -- this may cause issues when you decouple
            try
            {
                // Call AddCommand with CommandType.PositionQuery to get the motor's position
                var position = await _am.AddCommand(GetControllerTypeHelper(motor.GetMotorName()), motor.GetAxis(), CommandType.PositionQuery, 0);

                MagnetoLogger.Log($"Position of motor on axis {_buildMotor.GetAxis()} is {position}", LogFactoryLogLevel.LogLevel.SUCCESS);
            }
            catch (Exception ex)
            {
                MagnetoLogger.Log($"Failed to get motor position: {ex.Message}", LogFactoryLogLevel.LogLevel.ERROR);
            }
            var textBox = GetMotorPositonTextBox(motor);
            if (textBox != null)
                textBox.Text = motor.GetCurrentPos().ToString();
        }

        private async void MoveMotorAndUpdateUI(StepperMotor motor, TextBox textBox, bool moveIsAbs, bool increment)
        {
            var res = 0;
            if (motor != null)
            {
                // Select build motor button
                SelectMotorHelper(motor);

                if (moveIsAbs)
                {
                    res = await MoveMotorAbs(motor, textBox);
                }
                else
                {
                    res = await MoveMotorRel(motor, textBox, increment);
                }

                // If operation is successful, update text box
                if (res == 1)
                {
                    UpdateMotorPositionTextBox(motor);
                }
                else
                {
                    _ = PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", "Invalid input in moveUp text box.");
                }
            }
            else
            {
                _ = PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", "Cannot select motor. Motor is null.");
            }
        }

        #endregion


        #region Motor Movement Buttons

        private void IncrBuild_Click(object sender, RoutedEventArgs e)
        {
            HandleRelMove(_buildMotor, IncrBuildPositionTextBox, true);
        }

        private void DecrBuild_Click(object sender, RoutedEventArgs e)
        {
            HandleRelMove(_buildMotor, IncrBuildPositionTextBox, false);
        }

        private void IncrPowder_Click(object sender, RoutedEventArgs e)
        {
            HandleRelMove(_powderMotor, IncrPowderPositionTextBox, true);
        }

        private void DecrPowder_Click(object sender, RoutedEventArgs e)
        {
            HandleRelMove(_powderMotor, IncrPowderPositionTextBox, false);
        }

        private void IncrSweep_Click(object sender, RoutedEventArgs e)
        {
            HandleRelMove(_sweepMotor, IncrSweepPositionTextBox, true);
        }

        private void DecrSweep_Click(object sender, RoutedEventArgs e)
        {
            HandleRelMove(_sweepMotor, IncrSweepPositionTextBox, false);
        }

        private void AbsMoveBuildButton_Click(object sender, RoutedEventArgs e)
        {
            HandleAbsMove(_buildMotor, BuildAbsMoveTextBox);
        }

        private void AbsMovePowderButton_Click(object sender, RoutedEventArgs e)
        {
            HandleAbsMove(_powderMotor, PowderAbsMoveTextBox);
        }

        private void AbsMoveSweepButton_Click(object sender, RoutedEventArgs e)
        {
            HandleAbsMove(_sweepMotor, SweepAbsMoveTextBox);
        }

        private void HomeBuildMotorButton_Click(object sender, RoutedEventArgs e)
        {
            HandleHomeMotor(_buildMotor, BuildPositionTextBox);
        }

        private void HomePowderMotorButton_Click(object sender, RoutedEventArgs e)
        {
            HandleHomeMotor(_powderMotor, PowderPositionTextBox);
        }

        private void HomeSweepMotorButton_Click(object sender, RoutedEventArgs e)
        {
            HandleHomeMotor(_sweepMotor, SweepPositionTextBox);
        }

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
                HandleHomeMotor(_buildMotor, BuildPositionTextBox);
            }
            else
            {
                MagnetoLogger.Log("Build Motor is null, cannot home motor.", LogFactoryLogLevel.LogLevel.ERROR);
            }

            if (_powderMotor != null)
            {
                HandleHomeMotor(_powderMotor, PowderPositionTextBox);
            }
            else
            {
                MagnetoLogger.Log("Powder Motor is null, cannot home motor.", LogFactoryLogLevel.LogLevel.ERROR);
            }

            if (_sweepMotor != null)
            {
                HandleHomeMotor(_sweepMotor, SweepPositionTextBox);
            }
            else
            {
                MagnetoLogger.Log("Sweep Motor is null, cannot home motor.", LogFactoryLogLevel.LogLevel.ERROR);
            }
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
            HandleGetPosition(_buildMotor, BuildPositionTextBox);
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
            HandleGetPosition(_powderMotor, PowderPositionTextBox);
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
            HandleGetPosition(_sweepMotor, SweepPositionTextBox);
        }

        #endregion

    }
}
