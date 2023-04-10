﻿using System;
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
        // Move motor one step
        int StepMotor(int dir); // TODO: Make private in concrete implementation

        // Move motor motor to an absolute position
        int MoveMotorAbs(int steps);

        // Move motor relative to current position
        int MoveMotorRel(int steps);

        // Stop motor
        int StopMotor();

        // Get motor status
        int GetStatus();

        // Get current motor position
        int GetPos();

        // Move motor to position0
        void Home();

        // Send error message about motor
        int SendError(string message);
    }
}
