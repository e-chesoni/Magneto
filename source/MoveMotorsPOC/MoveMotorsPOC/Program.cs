using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;

namespace MoveMotorsPOC
{
    internal class Program
    {
        static SerialPort _serialPort;
        static bool _success;

        private static void SetupSerialPort()
        {
            Console.WriteLine("Initializing serial port...");

            // Allow the user to set the appropriate properties.
            _serialPort.PortName = "COM4";
            _serialPort.BaudRate = 38400;
            _serialPort.Parity = Parity.None;
            _serialPort.DataBits = 8;
            _serialPort.StopBits = StopBits.One;
            _serialPort.Handshake = Handshake.None;
        }

        private static bool OpenSerialPort()
        {
            Console.WriteLine("Opening serial port...");

            // Try opening the serial port
            try { _serialPort.Open(); }
            catch 
            { 
                Console.WriteLine("Cannot open serial port; COM4 is not valid.");
                _success = false;
            }

            return _success;
        }

        private static void SerialWrite(string msg)
        {
            Console.WriteLine("Sending move command...");

            if (_serialPort.IsOpen)
            {
                if (_serialPort.BytesToRead <= 0) 
                {
                    System.Text.ASCIIEncoding encoding = new System.Text.ASCIIEncoding();
                    byte[] data = encoding.GetBytes(msg);

                    try 
                    { 
                        _serialPort.Write(data, 0, data.Length);
                        _serialPort.Write("\n\r");
                    }
                    catch 
                    { 
                        Console.WriteLine("Cannot write to serial port.");
                    }
                }
            }
            else
            {
                Console.WriteLine("Serial Port is closed.");
            }
        }

        static void Main(string[] args)
        {
            // Create a new SerialPort object with default settings.
            _serialPort = new SerialPort();
            _success = true;

            SetupSerialPort();
            if (OpenSerialPort())
            {
                // Write hard-coded move command
                SerialWrite("1MVR5"); // success!
            }
            else
            {
                Console.WriteLine("Cannot complete the mission. Try again later.");
            }
            
            Console.ReadLine();
        }
    }
}
