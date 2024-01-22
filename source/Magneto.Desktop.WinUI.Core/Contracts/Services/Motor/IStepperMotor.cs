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

    /// <summary>
    /// Move motor to position zero
    /// </summary>
    Task<int> HomeMotor();

    /// <summary>
    /// Move motor to an absolute position
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns> Returns Task complete when done
    Task MoveMotorAbsAsync(double pos);

    /// <summary>
    /// Move motor relative to current position
    /// </summary>
    /// <param name="steps"></param>
    /// <returns></returns> Returns Task complete when done
    Task MoveMotorRelAsync(double steps);

    /// <summary>
    /// EMERGENCY STOP: Stop motor
    /// </summary>
    /// <returns></returns> Returns -1 if stop command fails, 0 if move command is successful
    Task StopMotor();

    #endregion

    #region Status Methods

    /// <summary>
    /// Get current motor position
    /// </summary>
    /// <returns></returns> Returns -1 if request for position fails, otherwise returns motor position
    Task<double> GetPosAsync();

    /// <summary>
    /// Send error message about motor
    /// </summary>
    /// <param name="message"></param>
    /// <returns></returns> Returns error associated with implementation error coding
    int SendError(string message);

    #endregion
}
