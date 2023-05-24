using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Magneto.Desktop.WinUI.Core.Contracts.Services.Motor;
/// <summary>
/// Interface for Magneto Motors
/// </summary>
public interface IStepperMotor
{
    #region Movement Methods

    // Move motor to position0
    void Home();

    // Move motor one step
    int StepMotor(int dir); // TODO: Make private in concrete implementation

    // Move motor to an absolute position
    int MoveMotorAbs(double pos);

    // Move motor relative to current position
    int MoveMotorRel(double steps);

    // Stop motor
    int StopMotor();

    #endregion

    #region Status Methods

    // Get current motor position
    double GetPos();

    // Send error message about motor
    int SendError(string message);

    #endregion
}
