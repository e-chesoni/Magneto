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

        public int GetPos()
        {
            throw new NotImplementedException();
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
        public int MoveMotorAbs(int pos)
        {
            // TODO: Error checking
            string s = string.Format("{0}MVA{1}", motorName, pos);
            SerialConsole.SerialWrite(s); 
            return 1; // return 1 for success
        }

        // Motor CMDs look like nMVRx
        public int MoveMotorRel(int steps)
        {
            string s = string.Format("{0}MVR{1}", motorName, steps);
            SerialConsole.SerialWrite(s);
            return 1; // return 1 for success
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
