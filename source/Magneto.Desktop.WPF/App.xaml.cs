using MagnetoLibrary;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

// Magneto Library

namespace Magneto.Desktop.WPF
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);

            // Your code here
            // This will be executed during the app's teardown
            Console.WriteLine("Doing teardown stuff...");
            SerialConsole.CloseSerialPort();
        }
    }
}
