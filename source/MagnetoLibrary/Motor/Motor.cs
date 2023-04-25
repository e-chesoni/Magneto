using MagnetoLibrary.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MagnetoLibrary.Motor
{
    public class Motor : IMotor
    {
        public int motorName;

        public Motor(int motorName)
        {
            
        }

        public double GetPos()
        {
            string s = string.Format("{0}POS?", motorName);
            MagnetoSerialConsole.SerialWrite(s);

            //TODO: Read serial console to get position value
            // TOOD: Figure out how to actually read serial console (safely)
            // see: Micronix note https://www.dropbox.com/scl/fo/2ls4fr6ffx0nswuno2n4x/h/System.IO.Ports%20Example%20Program%20and%20Guide?dl=0&preview=System.IO.Ports+C%23+Guide.pdf&subfolder_nav_tracking=1
            // see: Microsoft SerialPort.ReadLine Method https://learn.microsoft.com/en-us/dotnet/api/system.io.ports.serialport.readline?source=recommendations&view=dotnet-plat-ext-7.0

            // string s =  SerialConsole.SerialRead(); 

            return 0;
        }

        public int GetStatus()
        {
            throw new NotImplementedException();
        }

        public void Home()
        {
            throw new NotImplementedException();
        }

        public int MoveMotor(int steps)
        {
            throw new NotImplementedException();
        }

        // Motor CMDs look like nMVAx
        public int MoveMotorAbs(double pos)
        {
            // Invalid position
            if (pos < 0 || pos > 35)
            {
                // TODO: Log error
                return 1;
            }

            string s = string.Format("{0}MVA{1}", motorName, pos);
            MagnetoSerialConsole.SerialWrite(s);

            return 0; // return 0 for success
        }

        // Motor CMDs look like nMVRx
        public int MoveMotorRel(double steps)
        {
            // get the current position
            double currPos = GetPos();
            double pos = currPos + steps;

            // if the current position + steps is greater than 35, fail
            if (pos < 0 || pos > 35)
            {
                // TODO: Log error
                return 1;
            }

            string s = string.Format("{0}MVR{1}", motorName, steps);
            MagnetoSerialConsole.SerialWrite(s);

            return 0; // return 0 for success
        }

            public int SendError(string message)
        {
            throw new NotImplementedException();
        }

        public int StepMotor(int dir)
        {
            throw new NotImplementedException();
        }

        public int StopMotor()
        {
            throw new NotImplementedException();
        }
    }
}
