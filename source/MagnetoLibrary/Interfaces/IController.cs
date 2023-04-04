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
        // Attach left motor to controller (orientation: facing front of machine)
        int AttachLeftMotor();

        // Attach right motor to controller (orientation: facing front of machine)
        int AttachRightMotor();

        // Detach left motor to controller (orientation: facing front of machine)
        int DetachLeftMotor();

        // Detach right motor to controller (orientation: facing front of machine)
        int DetachRightMotor();

        // Perform sequenced motor movement
        int MoveMotors();

        // Stop all motors attached to this controller
        int StopMotors();

    }
}
