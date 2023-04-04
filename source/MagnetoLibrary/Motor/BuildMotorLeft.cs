using MagnetoLibrary.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MagnetoLibrary.Motor
{
    public class BuildMotorLeft : IMotor
    {
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
