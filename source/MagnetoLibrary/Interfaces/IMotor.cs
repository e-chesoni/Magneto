using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MagnetoLibrary.Interfaces
{
    /// <summary>
    /// 
    /// </summary>
    public interface IMotor
    {

        // Get current motor position
        double GetPos();

        // Move motor one step
        int StepMotor(int dir); // TODO: Make private in concrete implementation

        // Move motor motor to an absolute position
        int MoveMotorAbs(double pos);

        // Move motor relative to current position
        int MoveMotorRel(double steps);

        // Stop motor
        int StopMotor();

        // Get motor status
        int GetStatus();

        // Move motor to position0
        void Home();

        // Send error message about motor
        int SendError(string message);
    }
}
