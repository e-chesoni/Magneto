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
    public interface IController
    {
        // Attach motor to controller (orientation: facing front of machine)
        int AttachMotor();

        // Detach motor to controller (orientation: facing front of machine)
        int DetachMotor();

        // Perform sequenced motor movement
        // Syntax to move both motors to absolute position: 0MVA5.5
        int MoveMotors();

        // Stop all motors attached to this controller
        int StopMotors();

    }
}
