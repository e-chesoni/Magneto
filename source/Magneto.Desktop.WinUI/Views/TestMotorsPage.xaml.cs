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
using Magneto.Desktop.WinUI.Models.UIControl;

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

        private MotorPageService _motorPageService;

        private StepperMotor? _powderMotor;

        private StepperMotor? _buildMotor;

        private StepperMotor? _sweepMotor;

        private StepperMotor? _currTestMotor;

        private ActuationManager? _actuationManager;

        private bool _powderMotorSelected = false;

        private bool _buildMotorSelected = false;

        private bool _sweepMotorSelected = false;

        private bool _movingMotorToTarget = false;

        #endregion

        /// <summary>
        /// Store "global" mission control on this page
        /// </summary>
        public MissionControl? MissionControl { get; set; }

        /// <summary>
        /// Page view model
        /// </summary>
        public TestMotorsViewModel ViewModel { get; }

        private MotorUIControlGroup _calibrateMotorUIControlGroup { get; set; }

        public TestMotorsPage()
        {
            ViewModel = App.GetService<TestMotorsViewModel>();
            this.InitializeComponent();

            var msg = "Landed on Test Print Page";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.DEBUG);
            MagnetoSerialConsole.LogAvailablePorts();
        }

        #region Initialization Methods

        private void InitMotorPageService()
        {
            _calibrateMotorUIControlGroup = new MotorUIControlGroup(SelectBuildMotorButton, SelectPowderMotorButton, SelectSweepMotorButton,
                                                                                              BuildPositionTextBox, PowderPositionTextBox, SweepPositionTextBox,
                                                                                              IncrBuildPositionTextBox, IncrPowderPositionTextBox, IncrSweepPositionTextBox,
                                                                                              BuildAbsMoveTextBox, PowderAbsMoveTextBox, SweepAbsMoveTextBox);
            _motorPageService = new MotorPageService(MissionControl.GetActuationManger(), _calibrateMotorUIControlGroup);
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

            // Initialize motor page service
            InitMotorPageService(); // Must go after MissionControl initialization

            var msg = string.Format("TestPrintPage::OnNavigatedTo -- {0}", MissionControl.FriendlyMessage);
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.DEBUG);
        }

        #endregion


        #region Motor Movement Buttons

        private void IncrBuild_Click(object sender, RoutedEventArgs e)
        {
            _motorPageService.HandleRelMove(_motorPageService.buildMotor, _motorPageService.GetBuildStepTextBox(), true, this.Content.XamlRoot);
        }

        private void DecrBuild_Click(object sender, RoutedEventArgs e)
        {
            _motorPageService.HandleRelMove(_motorPageService.buildMotor, _motorPageService.GetBuildStepTextBox(), false, this.Content.XamlRoot);
        }

        private void IncrPowder_Click(object sender, RoutedEventArgs e)
        {
            _motorPageService.HandleRelMove(_motorPageService.powderMotor, _motorPageService.GetPowderStepTextBox(), true, this.Content.XamlRoot);
        }

        private void DecrPowder_Click(object sender, RoutedEventArgs e)
        {
            _motorPageService.HandleRelMove(_motorPageService.powderMotor, _motorPageService.GetPowderStepTextBox(), false, this.Content.XamlRoot);
        }

        private void IncrSweep_Click(object sender, RoutedEventArgs e)
        {
            _motorPageService.HandleRelMove(_motorPageService.sweepMotor, _motorPageService.GetSweepStepTextBox(), true, this.Content.XamlRoot);
        }

        private void DecrSweep_Click(object sender, RoutedEventArgs e)
        {
            _motorPageService.HandleRelMove(_motorPageService.sweepMotor, _motorPageService.GetSweepStepTextBox(), false, this.Content.XamlRoot);
        }

        private void AbsMoveBuildButton_Click(object sender, RoutedEventArgs e)
        {
            _motorPageService.HandleAbsMove(_motorPageService.buildMotor, _motorPageService.GetBuildAbsMoveTextBox(), this.Content.XamlRoot);
        }

        private void AbsMovePowderButton_Click(object sender, RoutedEventArgs e)
        {
            _motorPageService.HandleAbsMove(_motorPageService.powderMotor, _motorPageService.GetPowderAbsMoveTextBox(), this.Content.XamlRoot);
        }

        private void AbsMoveSweepButton_Click(object sender, RoutedEventArgs e)
        {
            _motorPageService.HandleAbsMove(_motorPageService.sweepMotor, _motorPageService.GetSweepAbsMoveTextBox(), this.Content.XamlRoot);
        }

        private void HomeBuildMotorButton_Click(object sender, RoutedEventArgs e)
        {
            _motorPageService.HandleHomeMotorAndUpdateTextBox(_motorPageService.buildMotor, _motorPageService.GetBuildPositionTextBox());
        }

        private void HomePowderMotorButton_Click(object sender, RoutedEventArgs e)
        {
            _motorPageService.HandleHomeMotorAndUpdateTextBox(_motorPageService.powderMotor, _motorPageService.GetPowderPositionTextBox());
        }

        private void HomeSweepMotorButton_Click(object sender, RoutedEventArgs e)
        {
            _motorPageService.HandleHomeMotorAndUpdateTextBox(_motorPageService.sweepMotor, _motorPageService.GetSweepPositionTextBox());
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

            if (_motorPageService.buildMotor != null)
            {
                _motorPageService.HandleHomeMotorAndUpdateTextBox(_motorPageService.buildMotor, _motorPageService.GetBuildPositionTextBox());
            }
            else
            {
                MagnetoLogger.Log("Build Motor is null, cannot home motor.", LogFactoryLogLevel.LogLevel.ERROR);
            }

            if (_motorPageService.powderMotor != null)
            {
                _motorPageService.HandleHomeMotorAndUpdateTextBox(_motorPageService.powderMotor,_motorPageService.GetPowderPositionTextBox());
            }
            else
            {
                MagnetoLogger.Log("Powder Motor is null, cannot home motor.", LogFactoryLogLevel.LogLevel.ERROR);
            }

            if (_motorPageService.sweepMotor != null)
            {
                _motorPageService.HandleHomeMotorAndUpdateTextBox(_motorPageService.sweepMotor, _motorPageService.GetSweepPositionTextBox());
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
            _motorPageService.printUiControlGroupHelper.SelectMotor(_motorPageService.buildMotor);
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
            _motorPageService.printUiControlGroupHelper.SelectMotor(_motorPageService.powderMotor);
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
            _motorPageService.printUiControlGroupHelper.SelectMotor(_motorPageService.sweepMotor);
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
            //HandleGetPosition(_buildMotor, BuildPositionTextBox);
            _motorPageService.HandleGetPosition(_motorPageService.buildMotor, _motorPageService.GetBuildPositionTextBox());
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
            _motorPageService.HandleGetPosition(_motorPageService.powderMotor, _motorPageService.GetPowderPositionTextBox());
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
            //HandleGetPosition(_sweepMotor, SweepPositionTextBox);
            _motorPageService.HandleGetPosition(_motorPageService.sweepMotor, _motorPageService.GetSweepPositionTextBox());
        }

        #endregion

    }
}
