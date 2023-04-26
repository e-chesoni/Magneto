using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

// Import Magneto Lib Classes
using MagnetoLibrary;
using MagnetoLibrary.Interfaces;
using MagnetoLibrary.Motor;

namespace Magneto.Desktop.WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //private static SerialConsole _serialConsole = new SerialConsole();
        MagnetoLogger magnetoLogger = new MagnetoLogger();

        // TODO: Init motors

        // TODO: Init stepper motor controller + pass to motors to controller

        // TODO: Init mission control + pass stepper motor controller to MC

        public MainWindow()
        {
            InitializeComponent();
            InitializeMagneto();
        }

        private void InitializeMagneto()
        {
            MagnetoSerialConsole.SetDefaultSerialPort();
        }

        private void MoveMotorButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: MOVE THIS TO MISSION CONTROL
            magnetoLogger.LogToConsole(MagnetoLogger.logLevels.DEBUG, "Moving Motor!");

            if (MagnetoSerialConsole.OpenSerialPort())
            {
                // Write hard-coded move command
                MagnetoSerialConsole.SerialWrite("1MVA20"); // success!
            }
            else
            {
                //MagnetoSerialConsole.SerialWrite("Serial port not open; cannot complete the mission. Try again later.");
                magnetoLogger.LogToConsole(MagnetoLogger.logLevels.ERROR, "Serial port not open; cannot complete the mission. Try again later.");
            }
        }

        private void HomeMotorButton_Click(object sender, RoutedEventArgs e)
        {
            // TODO: MISSION CONTROL HANDLES THIS
            magnetoLogger.LogToConsole(MagnetoLogger.logLevels.DEBUG, "Homing Motor.");

            if (MagnetoSerialConsole.OpenSerialPort())
            {
                // Write hard-coded move command
                MagnetoSerialConsole.SerialWrite("1MVA1"); // success!
            }
            else
            {
                magnetoLogger.LogToConsole(MagnetoLogger.logLevels.ERROR, "Serial port not open; cannot complete the mission. Try again later.");
            }
        }

        private void SetMotorToAxis1Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void SetMotorToAxis2Button_Click(object sender, RoutedEventArgs e)
        {

        }

        private void SetMotorToAxis3Button_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
