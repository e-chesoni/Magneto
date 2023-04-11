using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using Microsoft.Extensions.Logging.Console.Internal;

namespace MagnetoLibrary
{
    public static class SerialConsole
    {
        private static SerialPort _serialPort = new SerialPort();
        static bool _success;
        private static string _defaultPortName = "COM4";
        private static int _defaultBaudRate = 38400;
        private static Parity _defaultParity = Parity.None;
        private static int _defaultDataBits = 8;
        private static string _defaultStopBits = "One";
        private static string _defaultHandshake = "None";

        public static void GetAvailablePorts()
        {
            Console.WriteLine("Available Ports:");
            foreach (string s in SerialPort.GetPortNames())
            {
                Console.WriteLine("   {0}", s);
            }
        }

        private static string SetPortName(string portName)
        {
            if (portName == "" || !(portName.ToLower()).StartsWith("com"))
            {
                portName = _defaultPortName;
            }
            Console.WriteLine("Setting portname to {0}", portName);
            return portName;
        }

        private static int SetBaudRate(int baudRate)
        {
            if (baudRate == 0)
            {
                baudRate = _defaultBaudRate;
            }

            return baudRate;
        }

        private static Parity SetParity(string parity) 
        {
            if (parity == "")
            {
                parity = _defaultParity.ToString();
            }
            return (Parity)Enum.Parse(typeof(Parity), parity, true);
        }

        private static int SetDataBits(int dataBits)
        {
            return dataBits;
        }

        private static StopBits SetStopBits(string stopBits)
        {
            return (StopBits)Enum.Parse(typeof(StopBits), stopBits, true);
        }

        private static Handshake SetHandshake(string handshake)
        {
            return (Handshake)Enum.Parse(typeof(Handshake), handshake, true);
        }

        public static void SetSerialPort(string port, int baud, string parity, int dataBits, string stopBits, string handshake)
        {
            Console.WriteLine("Initializing serial port...");

            // Allow the user to set the appropriate properties.
            _serialPort.PortName = SetPortName(port);
            _serialPort.BaudRate = SetBaudRate(baud);
            _serialPort.Parity = SetParity(parity);
            _serialPort.DataBits = SetDataBits(dataBits);
            _serialPort.StopBits = SetStopBits(stopBits);
            _serialPort.Handshake = SetHandshake(handshake);
        }

        public static void SetDefaultSerialPort()
        {
            _serialPort.PortName = SetPortName("COM4");
            _serialPort.BaudRate = SetBaudRate(38400);
            _serialPort.Parity = SetParity("None");
            _serialPort.DataBits = SetDataBits(8);
            _serialPort.StopBits = SetStopBits("One");
            _serialPort.Handshake = SetHandshake("None");
        }

        public static bool OpenSerialPort()
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

        public static void SerialWrite(string msg)
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
        }
    }
}
