using System;
using System.Collections.Generic;
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

namespace Magneto.Desktop.WindowsApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //private static SerialConsole _serialConsole = new SerialConsole();

        public MainWindow()
        {
            InitializeComponent();
            InitializeMagneto();
        }

        private void InitializeMagneto()
        {
            SerialConsole.SetDefaultSerialPort();
        }

        private void MoveMotorButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show($"Moving Motor!");

            if (SerialConsole.OpenSerialPort())
            {
                // Write hard-coded move command
                SerialConsole.SerialWrite("1MVA20"); // success!
            }
            else
            {
                MessageBox.Show("Cannot complete the mission. Try again later.");
            }
        }

        private void HomeMotorButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show($"Homing Motor.");

            if (SerialConsole.OpenSerialPort())
            {
                // Write hard-coded move command
                SerialConsole.SerialWrite("1MVA1"); // success!
            }
            else
            {
                MessageBox.Show("Cannot complete the mission. Try again later.");
            }
        }
    }
}
