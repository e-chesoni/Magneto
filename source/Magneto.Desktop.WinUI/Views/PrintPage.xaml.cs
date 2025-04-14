using System.IO.Ports;
using System.Runtime.ConstrainedExecution;
using System.Xml.Linq;
using Magneto.Desktop.WinUI.Core;
using Magneto.Desktop.WinUI.Core.Contracts.Services;
using Magneto.Desktop.WinUI.Core.Models;
using Magneto.Desktop.WinUI.Core.Models.Artifact;
using Magneto.Desktop.WinUI.Core.Models.Motors;
using Magneto.Desktop.WinUI.Popups;
using Magneto.Desktop.WinUI.ViewModels;

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using static Magneto.Desktop.WinUI.Core.Models.Motors.StepperMotor;
using static Magneto.Desktop.WinUI.Core.Models.Print.CommandQueueManager;

namespace Magneto.Desktop.WinUI.Views;

public sealed partial class PrintPage : Page
{
    #region Public Variables

    /// <summary>
    /// Store "global" mission control on this page
    /// </summary>
    public MissionControl? _missionControl { get; set; }

    /// <summary>
    /// Page view model
    /// </summary>
    public PrintViewModel ViewModel
    {
        get;
    }

    #endregion

    #region Constructor

    /// <summary>
    /// Print Page constructor
    /// </summary>
    public PrintPage()
    {
        ViewModel = App.GetService<PrintViewModel>();
        _missionControl = App.GetService<MissionControl>();
        InitializeComponent(); // This is fine...not sure why there are red lines sometimes

        var msg = "";

        msg = "Landed on Print Page";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.DEBUG);
        MagnetoSerialConsole.LogAvailablePorts();

        var buildPort = MagnetoConfig.GetMotorByName("build").COMPort;
        var sweepPort = MagnetoConfig.GetMotorByName("sweep").COMPort;

        // Register event handlers on page
        foreach (SerialPort port in MagnetoSerialConsole.GetAvailablePorts())
        {
            // Get default motor (build motor) to get port
            if (port.PortName.Equals(buildPort, StringComparison.OrdinalIgnoreCase))
            {
                MagnetoSerialConsole.AddEventHandler(port);
                msg = $"Requesting addition of event hander or port {port.PortName}";
                MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);
            }
            else if (port.PortName.Equals(sweepPort, StringComparison.OrdinalIgnoreCase))
            {
                MagnetoSerialConsole.AddEventHandler(port);
                msg = $"Requesting addition of event hander or port {port.PortName}";
                MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);
            }
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
        base.OnNavigatedTo(e);
        //MissionControl = (MissionControl)e.Parameter; // get parameter
        //MagnetoLogger.Log(MissionControl.FriendlyMessage, LogFactoryLogLevel.LogLevel.DEBUG);
    }

    private void FindPrint_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        string path_to_image = "c:/path/to/test_print.sjf";
        var msg = "";

        // TODO: Check if path is valid
        var _validPath = true;

        if (_validPath)
        {
            // Add dummy string to text box
            // SelectedPrint is the name of the TextBox in PrintPage.xaml
            SelectedPrint.Text = path_to_image;

            // Put a new image on the build manager
            _missionControl.CreateArtifactModel(path_to_image);

            // TODO: Toast Message: Using default thickness of {} get from config
            msg = "Setting every print layer's thickness to default thickness from MagnetoConfig";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.DEBUG);
            _missionControl.SetArtifactThickness(_missionControl.GetDefaultArtifactThickness());

            // Slice image
            _missionControl.SliceArtifact(); // TODO: IMAGE HANDLER references Magneto Config to control slice number: SliceArtifact calls SliceArtifact in build controller which calls ImageHandler
            StartPrintButton.IsEnabled = true;

            // Enable go to start button
            GoToStartingPositionButton.IsEnabled = true;

            // TODO: MOVE ME -- Populate after successful calibration
            PrintHeightTextBlock.Text = _missionControl.GetCurrentPrintHeight().ToString();
        }
        else
        {
            msg = "Cannot find print: Invalid file path.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
            return;
        }
    }

    #endregion

    #region Button Methods

    private void StartPrint_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        // Calls build manager in method to handle print
        // Build manager should have an image at this point!
        // TODO: Clear images from build manager after print (in done and cancel states)
        _missionControl.StartPrint();
    }

    private void HomeMotors_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        _missionControl.HomeMotors();
    }

    private void IncrementThickness_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var newThickness = _missionControl.GetDefaultArtifactThickness();
        newThickness += 1;
        _missionControl.SetArtifactThickness(newThickness);
    }

    private void DecrementThickness_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var newThickness = _missionControl.GetDefaultArtifactThickness();
        newThickness -= 1;
        _missionControl.SetArtifactThickness(newThickness);
    }

    private void GoToStartingPositionButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var msg = $"Not yet implemented.";
        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.WARN);
        _ = PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Warning", msg);

        // Enable calibrate button
        CalibrateMotorsButton.IsEnabled = true;
    }

    private void CalibrateMotorsButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        _ = PopupInteractiveHelper.ShowContentDialog(this.Content.XamlRoot, _missionControl, "Calibrate Motors", "Calibrate Motors Description");
    }

    #endregion

    private void HomeBuildMotorButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (_missionControl != null)
        {
            var bm = _missionControl.GetActuationManger();
            var build_axis = bm.buildController.GetBuildMotor().GetAxis();
            bm.AddCommand(Core.Models.Print.CommandQueueManager.ControllerType.BUILD, build_axis, Core.Models.Print.CommandQueueManager.CommandType.AbsoluteMove, 0);
        }
        else
        {
            var msg = $"Mission control is null.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
            msg = $"Unable to communicate with Mission Control. Try reloading the page.";
            _ = PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", msg);
        }
    }

    private void HomePowderMotorButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        if (_missionControl != null)
        {
            var bm = _missionControl.GetActuationManger();
            var powder_axis = bm.buildController.GetPowderMotor().GetAxis();
            bm.AddCommand(Core.Models.Print.CommandQueueManager.ControllerType.BUILD, powder_axis, Core.Models.Print.CommandQueueManager.CommandType.AbsoluteMove, 0);
        }
        else
        {
            var msg = $"Mission control is null.";
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
            msg = $"Unable to communicate with Mission Control. Try reloading the page.";
            _ = PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", msg);
        }
    }

    private void CancelPrintButton_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var msg = "";
        if (_missionControl != null)
        {
            var bm = _missionControl.GetActuationManger();
            msg = $"Stopping print.";
            _ = PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Print Canceled", msg);
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
            bm.build_flag = BuildFlag.CANCEL;
        }
        else
        {
            msg = $"Mission control is null. Unable to cancel print.";
            _ = PopupInfo.ShowContentDialog(this.Content.XamlRoot, "Error", msg);
            MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.ERROR);
        }
        
    }
}
