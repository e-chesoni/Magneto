using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;

namespace SerialCommunicationPOC;

internal class Program
{
    static SerialPort _serialPort;
    static bool _success;

    private static void ListKnownPorts()
    {
        Console.WriteLine("Available ports:");
        foreach (var s in SerialPort.GetPortNames()) 
        {
            Console.WriteLine("  {0}", s);
        }
    }

    private static void SetupSerialPort()
    {
    
    }

    static void Main(string[] args)
    {
        Console.WriteLine("Starting Serial Communication POC...");
        
        ListKnownPorts();

        //TODO: Do other stuffs

        Console.WriteLine("Closing Serial Communication POC.");
    }
}
