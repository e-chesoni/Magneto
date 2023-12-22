using System.IO.Ports;
using System.Xml.Linq;
using Magneto.Desktop.WinUI.Core;
using Magneto.Desktop.WinUI.Core.Contracts.Services;
using Magneto.Desktop.WinUI.Core.Models;
using Magneto.Desktop.WinUI.Core.Models.Image;
using Magneto.Desktop.WinUI.Core.Models.Motor;
using Magneto.Desktop.WinUI.Core.Services;
using Magneto.Desktop.WinUI.ViewModels;

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using static Magneto.Desktop.WinUI.Core.Models.Motor.StepperMotor;

namespace Magneto.Desktop.WinUI.Views;

public sealed partial class PrintPage : Page
{
    #region Public Variables

    /// <summary>
    /// Store "global" mission control on this page
    /// </summary>
    public MissionControl MissionControl { get; set; }

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
        MissionControl = (MissionControl)e.Parameter; // get parameter
        MagnetoLogger.Log(MissionControl.FriendlyMessage, LogFactoryLogLevel.LogLevel.DEBUG);
    }

    private void FindPrint_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        string path_to_image = "c:/path/to/image";

        // Add dummy string to text box
        // SelectedPrint is the name of the TextBox in PrintPage.xaml
        SelectedPrint.Text = path_to_image; // This is fine...not sure why there are red lines sometimes

        // Put a new image on the build manager
        MissionControl.CreateImageModel(path_to_image);

        // TODO: Initialize Print
        if (string.IsNullOrEmpty(LayerThickness_TextBox.Text))
        {
            // TODO: Toast Message: Using default thickness of 5mm
            MagnetoLogger.Log("Setting every print layer's thickness to default thickness from MagnetoConfig", LogFactoryLogLevel.LogLevel.DEBUG);
            MissionControl.SetImageThickness(MissionControl.GetDefaultPrintLayerThickness());
        }
        else
        {
            // Check that text box entry is a number
            var textBoxValue = LayerThickness_TextBox.Text;
            double value;
            if (double.TryParse(textBoxValue, out value))
            {
                // Conversion succeeded, do something with 'value'
                MissionControl.SetImageThickness(value);
            }
            else
            {
                // Conversion failed, handle the error
                MagnetoLogger.Log("Conversion failed. Are you sure you entered a number?", 
                    LogFactoryLogLevel.LogLevel.ERROR);
            }
        }
        // Slice image
        MissionControl.SliceImage();
    }

    #endregion

    #region Button Methods

    private void ConvertLevelText(string textBoxVal)
    {
        // Convert text box value
        var val = Convert.ToDouble(textBoxVal);

        if (val < 0)
        {
            val = (-1) * val;
        }

        MissionControl.SetBedLevelStep(val);
    }

    private void UpdateBedLevel(TextBox textbox)
    {
        if (string.IsNullOrEmpty(textbox.Text) || string.IsNullOrWhiteSpace(textbox.Text))
        {
            textbox.Text = MissionControl.GedBedLevelStep().ToString();
        }
        else
        {
            ConvertLevelText(textbox.Text);
        }
    }

    private void HandleLevelMotor(int axis, MotorDirection dir, TextBox textBox)
    {
        UpdateBedLevel(textBox);

        string? msg;
        switch (dir)
        {
            case MotorDirection.Up:
                msg = $"Incrementing Bed Level By: {MissionControl.GedBedLevelStep()}";
                break;
            case MotorDirection.Down:
                msg = $"Decrementing Bed Level By: {MissionControl.GedBedLevelStep()}";
                break;
            default:
                msg = "Invalid direction.";
                break;
        }

        MagnetoLogger.Log(msg, LogFactoryLogLevel.LogLevel.VERBOSE);
        MissionControl.LevelMotor(axis, dir);
    }

    private void LevelBed_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        MissionControl.HomeMotors();
    }

    private void MoveMotor1Up_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {

        HandleLevelMotor(1, MotorDirection.Up, Motor1StepTextBox);
    }

    private void MoveMotor1Down_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        HandleLevelMotor(1, MotorDirection.Down, Motor1StepTextBox);
    }

    private void MoveMotor2Up_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        HandleLevelMotor(2, MotorDirection.Up, Motor2StepTextBox);
    }

    private void MoveMotor2Down_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        HandleLevelMotor(2, MotorDirection.Down, Motor2StepTextBox);
    }

    private void StartPrint_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        // Calls build manager in method to handle print
        // Build manager should have an image at this point!
        // TODO: Clear images from build manager after print (in done and cancel states)
        MissionControl.StartPrint();
    }

    private void HomeMotors_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        MissionControl.HomeMotors();
    }

    private void IncrementThickness_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var newThickness = MissionControl.GetImageThickness();
        newThickness += 1;
        MissionControl.SetImageThickness(newThickness);
    }

    private void DecrementThickness_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e)
    {
        var newThickness = MissionControl.GetImageThickness();
        newThickness -= 1;
        MissionControl.SetImageThickness(newThickness);
    }

    #endregion
}
